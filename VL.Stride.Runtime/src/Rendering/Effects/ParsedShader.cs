using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Shaders.Core;
using Stride.Shaders.Parsing;
using Stride.Shaders.Parsing.SDSL.AST;
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
        public readonly ShaderFile Shader;
        public readonly ShaderClass ShaderClass;

        // base shaders
        public IReadOnlyList<ParsedShader> BaseShaders => baseShaders;
        private readonly List<ParsedShader> baseShaders = new List<ParsedShader>();

        // compositions
        public IReadOnlyDictionary<string, CompositionInput> Compositions => compositions;
        private readonly Dictionary<string, CompositionInput> compositions;

        public IReadOnlyDictionary<string, CompositionInput> CompositionsWithBaseShaders => compositionsWithBaseShaders.Value;

        Lazy<IReadOnlyDictionary<string, CompositionInput>> compositionsWithBaseShaders;

        public readonly IReadOnlyList<ShaderMember> Variables;
        public readonly IReadOnlyDictionary<string, ShaderMember> VariablesByName;

        public string FilePath { get; }

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

        public ParsedShader(ShaderFile shader, string filePath)
        {
            Shader = shader;
            FilePath = filePath;
            ShaderClass = Shader.RootDeclarations.FirstOrDefault() as ShaderClass ?? Shader.Namespaces.FirstOrDefault()?.Declarations.FirstOrDefault() as ShaderClass; ;
            Variables = ShaderClass?.Elements.OfType<ShaderMember>().Where(v => v.StreamKind == StreamKind.None).ToList() ?? new List<ShaderMember>(); //should include parent shaders?
            VariablesByName = Variables.ToDictionary(v => v.Name.ToString());
            compositions = Variables
                .Select((v, i) => (v, i))
                .Where(v => v.v.IsCompose)
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
                    case ScalarType s when s.Type == Scalar.Float:
                        yield return ParameterKeys.NewValue((float)v.Value.GetNumberDefault<double>(), keyName);
                        break;
                    case ScalarType s when s.Type == Scalar.Int:
                        yield return ParameterKeys.NewValue((int)v.Value.GetNumberDefault<long>(), keyName);
                        break;
                    case ScalarType s when s.Type == Scalar.UInt:
                        yield return ParameterKeys.NewValue((uint)v.Value.GetNumberDefault<long>(), keyName);
                        break;
                    case ScalarType s when s.Type == Scalar.Boolean:
                        yield return ParameterKeys.NewValue(v.Value.GetBoolDefault(), keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Float && vt.Size == 2:
                        yield return ParameterKeys.NewValue(v.Value.GetVector2(), keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Float && vt.Size == 3:
                        yield return ParameterKeys.NewValue(v.Value.GetVector3(), keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Float && vt.Size == 4:
                        yield return ParameterKeys.NewValue(v.Value.GetVector4(), keyName);
                        break;
                    case MatrixType m when m.BaseType.Type == Scalar.Float && m.Rows == 4 && m.Columns == 4:
                        yield return ParameterKeys.NewValue(Matrix.Identity, keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Int && vt.Size == 2:
                        yield return ParameterKeys.NewValue(v.Value.GetInt2(), keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Int && vt.Size == 3:
                        yield return ParameterKeys.NewValue(v.Value.GetInt3(), keyName);
                        break;
                    case VectorType vt when vt.BaseType.Type == Scalar.Int && vt.Size == 4:
                        yield return ParameterKeys.NewValue(v.Value.GetInt4(), keyName);
                        break;
                    case TextureType t:
                        yield return new ObjectParameterKey<Texture>(keyName);
                        break;
                    case SamplerType:
                        yield return new ObjectParameterKey<SamplerState>(keyName);
                        break;
                    case BufferType:
                        yield return new ObjectParameterKey<Buffer>(keyName);
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

        public readonly ShaderMember Variable;

        public CompositionInput(ShaderMember v, int localIndex)
        {
            Name = v.Name.ToString();

            // parse attributes
            foreach (var attr in v.Attributes?.OfType<AnyShaderAttribute>() ?? Enumerable.Empty<AnyShaderAttribute>())
            {
                switch (attr.Name)
                {
                    case ShaderMetadata.OptionalName:
                        IsOptional = true;
                        break;
                    case ShaderMetadata.SummaryName:
                        Summary = (attr.Parameters.ElementAtOrDefault(0) as StringLiteral)?.Value;
                        break;
                    case ShaderMetadata.RemarksName:
                        Remarks = (attr.Parameters.ElementAtOrDefault(0) as StringLiteral)?.Value;
                        break;
                    default:
                        break;
                }
            }

            TypeName = v.TypeName;

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
