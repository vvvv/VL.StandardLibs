using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VL.ImGui.Generator
{
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        private const string GenerateNodeAttribute = "VL.ImGui.GenerateNodeAttribute";
        private const string PinAttribute = "VL.ImGui.PinAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var classes = context.SyntaxProvider.CreateSyntaxProvider(
               predicate: (node, token) => node is ClassDeclarationSyntax @class && @class.AttributeLists.Count > 0,
               transform: (ctx, token) => GetSemanticTargetForGeneration(ctx, token))
               .Where(c => c != null);

            var compilationAndClasses = context.CompilationProvider.Combine(classes.Collect());

            context.RegisterSourceOutput(compilationAndClasses, (spc, source) => Execute(source.Left, source.Right, spc));
        }

        private static ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken token)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            foreach (var attributeListSyntax in classSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, token).Symbol as IMethodSymbol;
                    if (attributeSymbol is null)
                        continue;

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == GenerateNodeAttribute)
                        return classSyntax;
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            var attributeSymbol = compilation.GetTypeByMetadataName(GenerateNodeAttribute)
                ?? throw new InvalidOperationException("Symbol not found: " + GenerateNodeAttribute);

            var pinAttributeSymbol = compilation.GetTypeByMetadataName(PinAttribute)
                ?? throw new InvalidOperationException("Symbol not found: " + PinAttribute);

            //Debugger.Launch();

            foreach (var syntax in classes.Distinct())
            {
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                var classSymbol = semanticModel.GetDeclaredSymbol(syntax);
                var data = GetAttributeData(attributeSymbol, classSymbol);
                build(Mode.RetainedMode);
                build(Mode.ImmediateMode);

                void build(Mode mode)
                {
                    var source = CreateSource(context, syntax, semanticModel, classSymbol, data, pinAttributeSymbol, mode);
                    context.AddSource($"{syntax.Identifier}.{mode}.g.cs", source);
                }
            }
        }

        private static ImmutableDictionary<string, TypedConstant> GetAttributeData(INamedTypeSymbol attributeSymbol, ISymbol symbol)
        {
            var attributeData = symbol?.GetAttributes().FirstOrDefault(d => d.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
            
            if (attributeData == null)
                return ImmutableDictionary<string, TypedConstant>.Empty;

            return attributeData.NamedArguments.ToImmutableDictionary(kv => kv.Key, kv => kv.Value);
        }

        internal enum Mode 
        {
            DownStreamParentFlag = 1<<0,
            RegionParentFlag = 1<<1,
            ParentsComeAsBeginEndCouplesFlag = 1<<2,    

            RetainedMode = 1 << 5 | DownStreamParentFlag,
            ImmediateMode = 1 << 6 | RegionParentFlag,
        }

        private static string CreateSource(SourceProductionContext context, ClassDeclarationSyntax declarationSyntax, SemanticModel semanticModel, 
            INamedTypeSymbol typeSymbol, ImmutableDictionary<string, TypedConstant> nodeAttrData, INamedTypeSymbol pinAttributeSymbol, Mode mode = Mode.RetainedMode)
        {
            var category = nodeAttrData.GetValueOrDefault("Category").Value as string ?? "ImGui";
            string nodeDecl = default;
            switch (mode)
            {
                case Mode.RetainedMode:
                    if (category != "ImGui.Styling")
                    {
                        category = category.Replace("ImGui", "ReGui");
                        category += ".Internal";
                    }
                    nodeDecl = "return c.Node(inputs, outputs);";
                    break;
                case Mode.ImmediateMode:
                    nodeDecl = "return c.Node(inputs, outputs, () => { s.Update(ctx); });";
                    break;
                default:
                    break;
            }
            var name = nodeAttrData.GetValueOrDefault("Name").Value as string ?? typeSymbol.Name;
            var tags = nodeAttrData.GetValueOrDefault("Tags").Value as string;
            var fragmented = "true";
            if (mode == Mode.ImmediateMode)
                fragmented = "false";   
            if (nodeAttrData.GetValueOrDefault("Fragmented").Value is bool f)
                fragmented = f ? "true" : "false";
            var button = false;
            if (nodeAttrData.GetValueOrDefault("Button").Value is bool b)
                button = b;

            var root = declarationSyntax.SyntaxTree.GetCompilationUnitRoot();
            var declaredUsings = root.Usings;
            foreach (var ns in root.Members.OfType<NamespaceDeclarationSyntax>())
                declaredUsings = declaredUsings.AddRange(ns.Usings);

            var summary = GetDocEntry(typeSymbol);

            var indent = "                    ";
            var indent2 = "                        ";
            var inputDescriptions = new List<string>();
            var outputDescriptions = new List<string>();
            var inputs = new List<string>();
            var outputs = new List<string>();

            List<ITypeSymbol> types = new List<ITypeSymbol>();
            ITypeSymbol ct = typeSymbol;
            while(ct != null)
            {
                types.Add(ct);
                ct = ct.BaseType;
            }
            var properties_ = ((IEnumerable<ITypeSymbol>)types).Reverse().SelectMany(t => t.GetMembers().OfType<IPropertySymbol>());

            SortedList<int, IPropertySymbol> properties = new SortedList<int, IPropertySymbol>();
            int i = 0;
            var showStyleInput = (bool)(nodeAttrData.GetValueOrDefault("IsStylable").Value ?? true);

            foreach (var property in properties_)
            {
                if (property.Name == "Style" && !showStyleInput)
                    continue;

                var pinAttrData = GetAttributeData(pinAttributeSymbol, property);
                if (pinAttrData.TryGetValue("Priority", out var prio))
                    properties.Add(((int)prio.Value) * 1000 + i, property);
                else
                    properties.Add(i, property);
                i++;
            }

            var ctx = mode == Mode.ImmediateMode ? "var ctx = default(Context);" : string.Empty;
            foreach (var property in properties.Values)
            {
                string propertySummary = GetDocEntry(property);

                bool doInput = property.SetMethod != null && property.SetMethod.DeclaredAccessibility == Accessibility.Public;
                bool doOutput = false;

                //if (button)
                //{
                //    if (property.Name == "Channel")
                //        doInput = false;
                //}

                if (doInput)
                {
                    inputDescriptions.Add($"_c.Input(\"{ToUserName(property.Name)}\", _w.{property.Name}, summary: @\"{propertySummary}\"),");
                    inputs.Add($"c.Input(v => s.{property.Name} = v, s.{property.Name}),");
                }
                else
                    doOutput = property.GetMethod != null && property.GetMethod.DeclaredAccessibility == Accessibility.Public; ;

                //if (property.Name == "Channel")
                //    doOutput = true;
                if (button)
                {
                    if (property.Name == "Value")
                        doOutput = false;
                }

                if (doOutput)
                {
                    outputDescriptions.Add($"_c.Output(\"{ToUserName(property.Name)}\", _w.{property.Name}),");
                    outputs.Add($"c.Output(() => s.{property.Name}),");
                }
            }

            if (mode == Mode.ImmediateMode)
            {
                inputDescriptions.Insert(0, "_c.Input(\"Context\", default(Context)),");
                inputs.Insert(0, "c.Input(v => ctx = v, ctx),");

                outputDescriptions.Insert(0, "_c.Output<Context>(\"Context\"),");
                outputs.Insert(0, "c.Output(() => ctx),");
            }
            else
            {
                var isStyle = typeSymbol.AllInterfaces.Any(t => t.Name == "IStyle");
                var type = isStyle ? "IStyle" : typeSymbol.DeclaredAccessibility == Accessibility.Public ? typeSymbol.Name : "Widget";
                outputDescriptions.Insert(0, $"_c.Output<{type}>(\"Output\"),");
                outputs.Insert(0, "c.Output(() => s),");
            }

            return $@"
using VL.Core;

namespace {typeSymbol.ContainingNamespace}
{{
    partial class {typeSymbol.Name}
    {{
        internal static IVLNodeDescription GetNodeDescription_{mode}(IVLNodeDescriptionFactory factory)
        {{
            return factory.NewNodeDescription(""{name}"", ""{category}"", fragmented: {fragmented}, invalidated: default, init: _c =>
            {{
                var _w = new {typeSymbol.Name}();
                var _inputs = new IVLPinDescription[]
                {{
                    { string.Join($"{Environment.NewLine}{indent}", inputDescriptions)}
                }};
                var _outputs = new[]
                {{                    
                    {string.Join($"{Environment.NewLine}{indent}", outputDescriptions)}
                }};
                return _c.Node(_inputs, _outputs, c =>
                {{
                    var s = new {typeSymbol.Name}();
                    {ctx}
                    var inputs = new IVLPin[]
                    {{
                        {string.Join($"{Environment.NewLine}{indent2}", inputs)}
                    }};
                    var outputs = new IVLPin[]
                    {{
                        {string.Join($"{Environment.NewLine}{indent2}", outputs)}
                    }};
                    {nodeDecl}
                }}, summary: @""{summary}"");
            }}, tags: ""{tags}"");
        }}
    }}
}}
";
        }

        private static string GetDocEntry(ISymbol symbol, string tag = "summary", string name = null)
        {
            var rawComment = new StringBuilder();
            foreach (var s in symbol.DeclaringSyntaxReferences)
            {
                foreach (var t in s.GetSyntax().GetLeadingTrivia())
                {
                    if (t.IsKind(SyntaxKind.SingleLineCommentTrivia))
                    {
                        rawComment.AppendLine(t.ToString().TrimStart('/', ' '));
                    }
                }
            }

            if (rawComment.Length == 0)
                return null;

            try
            {
                var x = XElement.Parse($"<X>{rawComment}</X>");
                if (name != null)
                    return x.Elements(tag).FirstOrDefault(e => e.Attribute("name")?.Value == name)?.ToString();
                else
                    return x.Element(tag)?.Value.ToString().Trim('\n').Replace("\"", "\"\"");
            }
            catch (Exception)
            {
                return null;
            }
        }

        static readonly Regex FCamelCasePattern = new Regex("[a-z][A-Z0-9]", RegexOptions.Compiled);

        private static string ToUserName(string name)
        {
            name = name.Trim('_');
            var userName = FCamelCasePattern.Replace(name, match => $"{match.Value[0]} {match.Value[1]}");
            if (userName.Length > 0)
                return char.ToUpper(userName[0]) + userName.Substring(1);
            return name;
        }

        private class Progress : IProgress<Diagnostic>
        {
            private readonly SourceProductionContext context;

            public Progress(SourceProductionContext context)
            {
                this.context = context;
            }

            public void Report(Diagnostic value)
            {
                context.ReportDiagnostic(value);
            }
        }
    }
}
