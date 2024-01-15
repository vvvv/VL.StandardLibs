using Stride.Core.Mathematics;
using Stride.Core.Shaders.Ast;
using Stride.Core.Shaders.Ast.Hlsl;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Stride.Shaders.ShaderFX;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Rendering
{
    public class ParsedShader
    {
        public readonly Shader Shader;
        public readonly ClassType ShaderClass;

        // base shaders
        public IReadOnlyList<ParsedShader> BaseShaders => baseShaders;
        private readonly List<ParsedShader> baseShaders = new List<ParsedShader>();

        // compositions
        public IReadOnlyDictionary<string, CompositionInput> Compositions => compositions;
        private readonly Dictionary<string, CompositionInput> compositions;

        public IReadOnlyDictionary<string, CompositionInput> CompositionsWithBaseShaders => compositionsWithBaseShaders.Value;

        Lazy<IReadOnlyDictionary<string, CompositionInput>> compositionsWithBaseShaders;

        public readonly IReadOnlyList<Variable> Variables;
        public readonly IReadOnlyDictionary<string, Variable> VariablesByName;

        private IEnumerable<CompositionInput> GetCompositionsWithBaseShaders()
        {
            foreach (var comp in Compositions)
            {
                yield return comp.Value;
            }

            foreach (var baseClass in BaseShaders)
            {
                foreach (var baseComp in baseClass.Compositions)
                {
                    yield return baseComp.Value;
                }
            }
        }

        public ParsedShader(Shader shader)
        {
            Shader = shader;
            ShaderClass = Shader.GetFirstClassDecl();
            Variables = ShaderClass?.Members.OfType<Variable>().Where(v => !v.Qualifiers.Contains(StrideStorageQualifier.Stream)).ToList() ?? new List<Variable>(); //should include parent shaders?
            VariablesByName = Variables.ToDictionary(v => v.Name.Text);
            compositions = Variables
                .Select((v, i) => (v, i))
                .Where(v => v.v.Qualifiers.Contains(StrideStorageQualifier.Compose))
                .Select(v => new CompositionInput(v.v, v.i))
                .ToDictionary(v => v.Name);

            compositionsWithBaseShaders = new Lazy<IReadOnlyDictionary<string, CompositionInput>>(() => GetCompositionsWithBaseShaders().ToDictionary(c => c.Name));
        }

        public void AddBaseShader(ParsedShader baseShader)
        {
            if (!baseShaders.Contains(baseShader))
                baseShaders.Add(baseShader);

        }

        public IEnumerable<ParameterKey> GetUniformInputs()
        {
            foreach (var v in Variables)
            {
                var type = v.Type;
                var keyName = ShaderClass.Name + "." + v.Name;

                switch (type)
                {
                    case ScalarType s when s.Name.Text == "float":
                        yield return ParameterKeys.NewValue(v.GetDefault<float>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "int":
                        yield return ParameterKeys.NewValue(v.GetDefault<int>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "uint":
                        yield return ParameterKeys.NewValue(v.GetDefault<uint>(), keyName);
                        break;
                    case ScalarType s when s.Name.Text == "bool":
                        yield return ParameterKeys.NewValue(v.GetDefault<bool>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float2":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector2>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float3":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector3>(), keyName);
                        break;
                    case TypeName n when n.Name.Text == "float4":
                        yield return ParameterKeys.NewValue(v.GetDefault<Vector4>(), keyName);
                        break;
                    case TypeName m when m.Name.Text == "float4x4":
                        yield return ParameterKeys.NewValue(Matrix.Identity, keyName);
                        break;
                    case TypeName s when s.Name.Text == "int2":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int2>(), keyName);
                        break;
                    case TypeName s when s.Name.Text == "int3":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int3>(), keyName);
                        break;
                    case TypeName s when s.Name.Text == "int4":
                        yield return ParameterKeys.NewValue(v.GetDefault<Int4>(), keyName);
                        break;
                    case TextureType t:
                        yield return new ObjectParameterKey<Texture>(keyName);
                        break;
                    case ObjectType o when o.Name.Text == "SamplerState":
                        yield return new ObjectParameterKey<SamplerState>(keyName);
                        break;
                    case GenericType b when b.Name.Text.Contains("Buffer"):
                        yield return new ObjectParameterKey<Buffer>(keyName);
                        break;
                    case GenericType t when t.Name.Text.Contains("Texture"):
                        yield return new ObjectParameterKey<Texture>(keyName);
                        break;
                    default:
                        break;
                }
            }
        }

        public override string ToString()
        {
            return ShaderClass?.ToString() ?? base.ToString();
        }
    }

    public class ParsedShaderRef
    {
        public ParsedShader ParsedShader;
        public Stack<ParsedShader> ParentShaders = new Stack<ParsedShader>();
    }

    public class UniformInput
    {
        public string Name;
        public Type Type;

    }

    public class CompositionInput
    {
        public readonly string Name;
        public readonly string TypeName;
        public readonly string Summary;
        public readonly string Remarks;
        public readonly bool IsOptional;
        public readonly PermutationParameterKey<ShaderSource> Key;

        /// <summary>
        /// The local index of this variable in the shader file.
        /// </summary>
        public readonly int LocalIndex;

        public readonly Variable Variable;

        public CompositionInput(Variable v, int localIndex)
        {
            Name = v.Name.Text;

            // parse attributes
            foreach (var attr in v.Attributes.OfType<AttributeDeclaration>())
            {
                switch (attr.Name)
                {
                    case ShaderMetadata.OptionalName:
                        IsOptional = true;
                        break;
                    case ShaderMetadata.SummaryName:
                        Summary = attr.ParseString();
                        break;
                    case ShaderMetadata.RemarksName:
                        Remarks = attr.ParseString();
                        break;
                    default:
                        break;
                }
            }

            TypeName = v.Type.Name.Text;

            Key = new PermutationParameterKey<ShaderSource>(Name);
            LocalIndex = localIndex;
            Variable = v;
        }

        // cache
        ShaderSource defaultShaderSource;
        IComputeNode defaultComputeNode;
        IComputeNode defaultGetter;

        public IComputeNode GetDefaultComputeNode(bool forPatch = false)
        {
            if (defaultComputeNode != null)
                return forPatch ? defaultComputeNode : defaultGetter ?? defaultComputeNode;

            try
            {
                if (knownShaderFXTypeInputs.TryGetValue(TypeName, out var compDefault))
                {
                    var def = compDefault.Factory(CompilationDefaultValue);
                    defaultComputeNode = def.func;
                    defaultGetter = def.getter;
                    return forPatch ? defaultComputeNode : defaultGetter ?? defaultComputeNode;
                }

                defaultComputeNode = new ShaderSourceComputeNode(new ShaderClassSource(TypeName));
                return defaultComputeNode;
            }
            catch
            {
                return null;
            }
        }

        public object CompilationDefaultValue
        {
            get
            {
                if (cachedCompilationDefaultValue != null)
                    return cachedCompilationDefaultValue;

                if (!knownShaderFXTypeInputs.TryGetValue(TypeName, out var typeDefault))
                    return null;

                if (!Variable.TryGetAttribute(ShaderMetadata.DefaultName, out var attribute))
                    return cachedCompilationDefaultValue = typeDefault.BoxedDefault;

                return cachedCompilationDefaultValue = attribute.ParseBoxed(typeDefault.ValueType);
            }
        }
        object cachedCompilationDefaultValue;

        public ShaderSource GetDefaultShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            if (defaultShaderSource != null)
                return defaultShaderSource;

            var defaultNode = GetDefaultComputeNode();

            if (defaultNode != null)
            {
                defaultShaderSource = defaultNode.GenerateShaderSource(context, baseKeys);
                return defaultShaderSource;
            }
            else
            {
                defaultShaderSource = new ShaderClassSource(TypeName);
                return defaultShaderSource;
            }
        }

        static Dictionary<string, CompDefault> knownShaderFXTypeInputs = new Dictionary<string, CompDefault>()
        {
            { "ComputeVoid", new CompDefaultVoid() },
            { "ComputeFloat", new CompDefaultValue<float>() },
            { "ComputeFloat2", new CompDefaultValue<Vector2>() },
            { "ComputeFloat3", new CompDefaultValue<Vector3>() },
            { "ComputeFloat4", new CompDefaultValue<Vector4>() },
            { "ComputeColor", new CompDefaultValue<Color4>() },
            { "ComputeMatrix", new CompDefaultValue<Matrix>() },
            { "ComputeBool", new CompDefaultValue<bool>() },
            { "ComputeInt", new CompDefaultValue<int>() },
            { "ComputeInt2", new CompDefaultValue<Int2>() },
            { "ComputeInt3", new CompDefaultValue<Int3>() },
            { "ComputeInt4", new CompDefaultValue<Int4>() },
            { "ComputeUInt", new CompDefaultValue<uint>() },
        };

        abstract class CompDefault
        {
            public readonly object BoxedDefault;
            public readonly Func<object, (IComputeNode func, IComputeNode getter)> Factory;
            public readonly Type ValueType;

            public CompDefault(object defaultValue, Func<object, (IComputeNode func, IComputeNode getter)> factory, Type valueType)
            {
                BoxedDefault = defaultValue;
                Factory = factory;
                ValueType = valueType;
            }
        }

        class CompDefaultVoid : CompDefault
        {
            public CompDefaultVoid()
                : base(null, _ => (new ComputeOrder(), null), null)
            {
            }
        }

        class CompDefaultValue<T> : CompDefault where T : struct
        {
            public CompDefaultValue(T defaultValue = default)
                : base(defaultValue, BuildInput, typeof(T))
            {
            }

            static (IComputeNode, IComputeNode) BuildInput(object boxedDefaultValue)
            {
                var input = new InputValue<T>();
                 if (boxedDefaultValue is T defaultValue)
                    input.Input = defaultValue;
                return (ShaderFXUtils.DeclAndSetVar("Default", input), input);
            }
        }

        class ShaderSourceComputeNode : IComputeNode
        {
            readonly ShaderSource shaderSource;

            public ShaderSourceComputeNode(ShaderSource shader)
                => shaderSource = shader;

            public ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
            {
                return shaderSource;
            }

            public IEnumerable<IComputeNode> GetChildren(object context = null)
            {
                return Enumerable.Empty<IComputeNode>();
            }
        }
    }
}
