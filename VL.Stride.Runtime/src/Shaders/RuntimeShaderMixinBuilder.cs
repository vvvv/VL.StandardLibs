using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Core.Shaders.Visitor;
using Stride.Rendering;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace VL.Stride.Shaders
{
    /// <summary>
    /// Builds a shader mixin at runtime based on the shader AST.
    /// Can also be used to collect all the compiler parameters of a shader.
    /// </summary>
    class RuntimeShaderMixinBuilder : ShaderWalker, IShaderMixinBuilder
    {
        private const string BlockContextTag = "BlockContextTag";

        private readonly Dictionary<string, Dictionary<string, ParameterKey>> UsedParameters = new Dictionary<string, Dictionary<string, ParameterKey>>();
        private readonly HashSet<string> usings = new HashSet<string>();
        private readonly Shader shader;
        private ShaderMixinSource mixinTree;
        private ShaderMixinContext context;
        private EffectBlock currentBlock;

        public RuntimeShaderMixinBuilder(Shader shader) 
            : base(buildScopeDeclaration: false, useNodeStack: false)
        {
            this.shader = shader;
        }

        public void Generate(ShaderMixinSource mixinTree, ShaderMixinContext context)
        {
            this.mixinTree = mixinTree;
            this.context = context;

            CollectParameters();

            VisitDynamic(shader);
        }

        public Dictionary<string, Dictionary<string, ParameterKey>> CollectParameters()
        {
            var blockVisitor = new ShaderBlockVisitor(this);
            blockVisitor.Run(shader);
            return UsedParameters;
        }

        Type GetType(Identifier typeExpr)
        {
            // Look through all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var ns in usings)
                {
                    var name = $"{ns}.{typeExpr}";
                    var type = assembly.GetType(name);
                    if (type != null)
                        return type;
                }
            }
            return null;
        }

        ParameterKey GetParameterKey(Identifier typeExpr, Identifier memberExpr)
        {
            if (UsedParameters.TryGetValue(typeExpr.Text, out var parameters))
                return parameters[memberExpr.Text];

            var type = GetType(typeExpr);
            var field = type?.GetField(memberExpr.ToString(), BindingFlags.Public | BindingFlags.Static);
            return field?.GetValue(null) as ParameterKey;
        }

        public override void Visit(EffectBlock effectBlock)
        {
            var previousBlock = currentBlock;
            currentBlock = effectBlock;
            try
            {
                base.Visit(effectBlock);
            }
            finally
            {
                currentBlock = previousBlock;
            }
        }

        public override void Visit(UsingStatement usingStatement)
        {
            usings.Add(usingStatement.Name.ToString());
            base.Visit(usingStatement);
        }

        public override void Visit(AssignmentExpression assignmentExpression)
        {
            if (TryParameters(assignmentExpression.Target, out var typeTarget, out var typeMember))
            {
                var key = GetParameterKey(typeTarget, typeMember);
                SetParam(context, key, GetValue(assignmentExpression.Value));
            }
        }

        public override void Visit(KeywordExpression keywordExpression)
        {
            // A discard will be transformed to 'return'
            if (keywordExpression.Name.Text == "discard")
            {
                context.Discard();
            }
        }

        public override void Visit(MixinStatement mixinStatement)
        {
            Expression mixinName;
            AssignmentExpression assignExpression;
            var genericParameters = new List<object>();

            switch (mixinStatement.Type)
            {
                case MixinStatementType.Default:
                    ExtractGenericParameters(mixinStatement.Value, out mixinName, genericParameters);

                    this.context.Mixin(mixinTree, (string)GetStringOrValue(mixinName), genericParameters.ToArray());
                    break;

                case MixinStatementType.Child:

                    // mixin child can come in 2 flavour:
                    // 1) mixin child MyEffect => equivalent to mixin child MyEffect = MyEffect
                    // 2) mixin child MyGenericEffectName = MyEffect
                    var targetExpression = mixinStatement.Value;
                    assignExpression = mixinStatement.Value as AssignmentExpression;
                    if (assignExpression != null)
                    {
                        targetExpression = assignExpression.Value;
                    }

                    ExtractGenericParameters(targetExpression, out mixinName, genericParameters);
                    var childName = (string)GetStringOrValue(assignExpression != null ? assignExpression.Target : mixinName);
                    if (this.context.ChildEffectName == childName)
                    {
                        this.context.Mixin(mixinTree, childName, genericParameters);
                        this.context.Discard();
                    }
                    break;

                case MixinStatementType.Remove:
                    ExtractGenericParameters(mixinStatement.Value, out mixinName, genericParameters);

                    this.context.RemoveMixin(mixinTree, (string)GetStringOrValue(mixinName));

                    if (genericParameters.Count > 0)
                    {
                        throw new Exception("Removing with generic parameters is not supported "+ mixinStatement.Span);
                    }
                    break;

                case MixinStatementType.Macro:
                    var shaderBlockContext = (ShaderBlockContext)currentBlock.GetTag(BlockContextTag);
                    assignExpression = mixinStatement.Value as AssignmentExpression;
                    Expression macroName;
                    Expression macroValue;

                    if (assignExpression != null)
                    {
                        macroName = assignExpression.Target;
                        if (macroName is VariableReferenceExpression)
                        {
                            macroName = new LiteralExpression(macroName.ToString());
                        }
                        macroValue = assignExpression.Value;
                    }
                    else
                    {
                        var variableReference = mixinStatement.Value as MemberReferenceExpression;
                        if (variableReference == null || !(variableReference.Target is VariableReferenceExpression) || !shaderBlockContext.DeclaredParameters.Contains((((VariableReferenceExpression)variableReference.Target).Name.Text)))
                        {
                            throw new Exception("Invalid syntax. Expecting: mixin macro Parameters.NameOfProperty or mixin macro nameOfProperty = value " + mixinStatement.Span);
                        }
                        else
                        {
                            macroName = new LiteralExpression(variableReference.Member.Text);
                            macroValue = mixinStatement.Value;
                        }
                    }

                    mixinTree.AddMacro((string)GetStringOrValue(macroName), GetValue(macroValue));
                    break;

                case MixinStatementType.Compose:
                    assignExpression = mixinStatement.Value as AssignmentExpression;
                    if (assignExpression == null)
                    {
                        throw new Exception("Expecting assign expression for composition " + mixinStatement.Value.Span);
                    }

                    ExtractGenericParameters(assignExpression.Value, out mixinName, genericParameters);

                    var mixinToCompose = GetStringOrValue(mixinName);
                    var subMixin = new ShaderMixinSource();

                    // If it's a +=, let's create or complete a ShaderArraySource
                    if (assignExpression.Operator == AssignmentOperator.Addition)
                        context.PushCompositionArray(mixinTree, (string)GetStringOrValue(assignExpression.Target), subMixin);
                    else
                        context.PushComposition(mixinTree, (string)GetStringOrValue(assignExpression.Target), subMixin);

                    if (mixinToCompose is string s)
                        context.Mixin(subMixin, s);
                    else if (mixinToCompose is ShaderSource shaderSource)
                        context.Mixin(subMixin, shaderSource);

                    context.PopComposition();
                    break;
            }
        }


        private bool TryParameters(Expression expression, out Identifier type, out Identifier member)
        {
            type = null;
            member = null;
            var memberReferenceExpression = expression as MemberReferenceExpression;
            if (memberReferenceExpression == null)
                return false;

            var name = memberReferenceExpression.Target as VariableReferenceExpression;

            bool foundDeclaredParameters = false;
            if (currentBlock != null)
            {
                var context = (ShaderBlockContext)currentBlock.GetTag(BlockContextTag);
                HashSet<string> usings = context.DeclaredParameters;

                if (name != null && usings.Contains(name.Name))
                {
                    type = name.Name;
                    member = memberReferenceExpression.Member;
                    foundDeclaredParameters = true;
                }
            }

            return foundDeclaredParameters;
        }

        private void ExtractGenericParameters(Expression expression, out Expression mixinName, List<object> genericParametersOut)
        {
            if (genericParametersOut == null)
            {
                throw new ArgumentNullException("genericParametersOut");
            }

            mixinName = expression;
            genericParametersOut.Clear();

            if (expression is VariableReferenceExpression varExp)
            {
                if (varExp.Name is IdentifierGeneric identifierGeneric)
                {
                    mixinName = new VariableReferenceExpression(identifierGeneric.Text);

                    foreach (var subIdentifier in identifierGeneric.Identifiers)
                    {
                        var identifierDot = subIdentifier as IdentifierDot;
                        if (identifierDot != null)
                        {
                            if (identifierDot.Identifiers.Count == 2)
                            {
                                genericParametersOut.Add(GetValue(new MemberReferenceExpression(new VariableReferenceExpression(identifierDot.Identifiers[0]), identifierDot.Identifiers[1])));
                            }
                            else
                            {
                                throw new Exception("Unsupported identifier in generic used for mixin " + identifierDot.Span);
                            }
                        }
                        else if (subIdentifier is LiteralIdentifier literalIdentifier)
                        {
                            genericParametersOut.Add(GetValue(new LiteralExpression(literalIdentifier.Value)));
                        }
                        else if (subIdentifier.GetType() == typeof(Identifier))
                        {
                            genericParametersOut.Add(GetValue(new VariableReferenceExpression(subIdentifier)));
                        }
                        else
                        {
                            throw new Exception("Unsupported identifier in generic used for mixin " + subIdentifier.Span);
                        }
                    }
                }
            }
        }

        object GetStringOrValue(Expression expr)
        {
            if (expr is VariableReferenceExpression variableReference)
                return variableReference.Name.Text;
            return GetValue(expr);
        }

        object GetValue(Expression expr)
        {
            if (TryParameters(expr, out var typeTarget, out var typeMember))
                return GetParam(context, GetParameterKey(typeTarget, typeMember));
            else
                return GetObjectValue(expr);
        }

        static object GetParam(ShaderMixinContext context, ParameterKey key)
        {
            if (key is PermutationParameterKey<string> stringKey)
                return context.GetParam(stringKey);
            else if (key is PermutationParameterKey<bool> boolKey)
                return context.GetParam(boolKey);
            else if (key is PermutationParameterKey<double> doubleKey)
                return context.GetParam(doubleKey);
            else if (key is PermutationParameterKey<float> floatKey)
                return context.GetParam(floatKey);
            else if (key is PermutationParameterKey<int> intKey)
                return context.GetParam(intKey);
            else if (key is PermutationParameterKey<uint> uintKey)
                return context.GetParam(uintKey);
            else if (key is PermutationParameterKey<Vector2> vec2Key)
                return context.GetParam(vec2Key);
            else if (key is PermutationParameterKey<Vector3> vec3Key)
                return context.GetParam(vec3Key);
            else if (key is PermutationParameterKey<Vector4> vec4Key)
                return context.GetParam(vec4Key);
            else if (key is PermutationParameterKey<Int2> int2Key)
                return context.GetParam(int2Key);
            else if (key is PermutationParameterKey<Int3> int3Key)
                return context.GetParam(int3Key);
            else if (key is PermutationParameterKey<Int4> int4Key)
                return context.GetParam(int4Key);
            else if (key is PermutationParameterKey<UInt4> uint4Key)
                return context.GetParam(uint4Key);
            throw new NotImplementedException();
        }

        static void SetParam(ShaderMixinContext context, ParameterKey key, object value)
        {
            if (key is PermutationParameterKey<string> stringKey)
                context.SetParam(stringKey, (string)value);
            else if (key is PermutationParameterKey<bool> boolKey)
                context.SetParam(boolKey, (bool)value);
            else if (key is PermutationParameterKey<double> doubleKey)
                context.SetParam(doubleKey, (double)value);
            else if (key is PermutationParameterKey<float> floatKey)
                context.SetParam(floatKey, (float)value);
            else if (key is PermutationParameterKey<int> intKey)
                context.SetParam(intKey, (int)value);
            else if (key is PermutationParameterKey<uint> uintKey)
                context.SetParam(uintKey, (uint)value);
            else if (key is PermutationParameterKey<Vector2> vec2Key)
                context.SetParam(vec2Key, (Vector2)value);
            else if (key is PermutationParameterKey<Vector3> vec3Key)
                context.SetParam(vec3Key, (Vector3)value);
            else if (key is PermutationParameterKey<Vector4> vec4Key)
                context.SetParam(vec4Key, (Vector4)value);
            else if (key is PermutationParameterKey<Int2> int2Key)
                context.SetParam(int2Key, (Int2)value);
            else if (key is PermutationParameterKey<Int3> int3Key)
                context.SetParam(int3Key, (Int3)value);
            else if (key is PermutationParameterKey<Int4> int4Key)
                context.SetParam(int4Key, (Int4)value);
            else if (key is PermutationParameterKey<UInt4> uint4Key)
                context.SetParam(uint4Key, (UInt4)value);
            throw new NotImplementedException();
        }

        static ParameterKey CreateParameter(Variable variable)
        {
            if (variable.Type == TypeBase.String)
                return NewPermutation<string>(variable);
            if (variable.Type == ScalarType.Bool)
                return NewPermutation<bool>(variable);
            if (variable.Type == ScalarType.Double)
                return NewPermutation<double>(variable);
            if (variable.Type == ScalarType.Float)
                return NewPermutation<float>(variable);
            if (variable.Type == ScalarType.Int)
                return NewPermutation<int>(variable);
            if (variable.Type == ScalarType.UInt)
                return NewPermutation<uint>(variable);
            if (variable.Type == VectorType.Float2)
                return NewPermutation<Vector2>(variable);
            if (variable.Type == VectorType.Float3)
                return NewPermutation<Vector3>(variable);
            if (variable.Type == VectorType.Float4)
                return NewPermutation<Vector4>(variable);
            if (variable.Type == VectorType.Int2)
                return NewPermutation<Int2>(variable);
            if (variable.Type == VectorType.Int3)
                return NewPermutation<Int3>(variable);
            if (variable.Type == VectorType.Int4)
                return NewPermutation<Int4>(variable);
            if (variable.Type == VectorType.UInt4)
                return NewPermutation<UInt4>(variable);
            throw new NotImplementedException();
        }

        static PermutationParameterKey<T> NewPermutation<T>(Variable variable)
        {
            return ParameterKeys.NewPermutation(defaultValue: GetValue<T>(variable.InitialValue), name: variable.Name.Text);
        }

        static T GetValue<T>(Expression expr) => GetObjectValue(expr) is T v ? v : default;

        static object GetObjectValue(Expression expr) => expr is LiteralExpression l ? l.Value : default;

        private class ShaderBlockContext
        {
            public readonly HashSet<string> DeclaredParameters = new HashSet<string>();
        }

        /// <summary>
        /// Internal visitor to precalculate all available Parameters in the context
        /// </summary>
        private sealed class ShaderBlockVisitor : ShaderWalker
        {
            private ShaderBlockContext currentContext;
            private RuntimeShaderMixinBuilder parent;

            public ShaderBlockVisitor(RuntimeShaderMixinBuilder parent)
                : base(false, false)
            {
                this.parent = parent;
            }

            public bool HasShaderClassType { get; private set; }

            public void Run(Shader shader)
            {
                VisitDynamic(shader);
            }

            internal bool IsParameterKey(Variable variable)
            {
                // Don't generate a parameter key for variable stored storage qualifier: extern, const, compose, stream
                if (variable.Qualifiers.Contains(global::Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Extern)
                    || variable.Qualifiers.Contains(global::Stride.Core.Shaders.Ast.StorageQualifier.Const)
                    || variable.Qualifiers.Contains(StrideStorageQualifier.Compose)
                    || variable.Qualifiers.Contains(StrideStorageQualifier.PatchStream)
                    || variable.Qualifiers.Contains(global::Stride.Core.Shaders.Ast.Hlsl.StorageQualifier.Groupshared)
                    || variable.Qualifiers.Contains(StrideStorageQualifier.Stream))
                    return false;

                // Don't generate a parameter key for [Link] or [RenameLink]
                if (variable.Attributes.OfType<AttributeDeclaration>().Any(x => x.Name == "RenameLink" || x.Name == "Link"))
                    return false;

                return true;
            }

            public override void Visit(EffectBlock effectBlock)
            {
                // Create a context associated with ShaderBlock
                currentContext = new ShaderBlockContext();
                effectBlock.SetTag(BlockContextTag, currentContext);

                foreach (Statement statement in effectBlock.Body.Statements)
                {
                    VisitDynamic(statement);
                }
                currentContext = null;
            }

            public override void Visit(UsingParametersStatement usingParametersStatement)
            {
                if (currentContext == null)
                {
                    throw new Exception("Unexpected 'using params' outside of shader block declaration " + usingParametersStatement.Span);
                }

                HashSet<string> usings = currentContext.DeclaredParameters;

                // If this is a using params without a body, it is a simple reference of a ParameterBlock
                if (usingParametersStatement.Body == null)
                {
                    var simpleName = usingParametersStatement.Name as VariableReferenceExpression;
                    if (simpleName != null)
                    {
                        string typeName = simpleName.Name.Text;

                        if (usings.Contains(typeName))
                        {
                            throw new Exception("Unexpected declaration of using params. This variable is already declared in this scope " + usingParametersStatement.Span);
                        }

                        usings.Add(typeName);
                    }
                }
                else
                {
                    // using params with a body is to enter the context of the parameters passed to the using statements
                    VisitDynamic(usingParametersStatement.Body);
                }
            }

            //public override void Visit(ShaderClassType shaderClassType)
            //{
            //    var parameters = new Dictionary<string, ParameterKey>();
            //    parent.parameterCollections.Add(shaderClassType.Name, parameters);

            //    foreach (var decl in shaderClassType.Members)
            //    {
            //        if (decl is Variable variable && IsParameterKey(variable))
            //        {
            //            AddNewParameter(parameters, variable);
            //        }
            //        else if (decl is ConstantBuffer constantBuffer)
            //        {
            //            foreach (var member in constantBuffer.Members)
            //            {
            //                if (member is Variable variable1 && IsParameterKey(variable1))
            //                    AddNewParameter(parameters, variable1);
            //            }
            //        }
            //    }
            //}

            public override void Visit(ParametersBlock paramsBlock)
            {
                var parameters = new Dictionary<string, ParameterKey>();
                parent.UsedParameters.Add(paramsBlock.Name, parameters);
                foreach (var parameter in paramsBlock.Body.Statements.OfType<DeclarationStatement>())
                    if (parameter.Content is Variable variable)
                        AddNewParameter(parameters, variable);
            }

            private static void AddNewParameter(Dictionary<string, ParameterKey> parameters, Variable variable)
            {
                var key = CreateParameter(variable);
                if (key != null)
                    parameters.Add(key.Name, key);
            }
        }
    }
}
