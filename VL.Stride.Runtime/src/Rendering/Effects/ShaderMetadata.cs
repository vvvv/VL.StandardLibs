using System;
using Stride.Graphics;
using Stride.Core.Shaders.Ast;
using System.Linq;
using Stride.Core.Shaders.Ast.Hlsl;
using System.Collections.Generic;
using Stride.Core.IO;
using Stride.Rendering;
using Stride.Core.Shaders.Ast.Stride;
using Stride.Shaders;
using VL.Stride.Shaders.ShaderFX;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using System.ComponentModel;
using Stride.Shaders.Parser.Mixins;

namespace VL.Stride.Rendering
{
    public class ShaderMetadata
    {
        public PixelFormat OutputFormat { get; private set; } = PixelFormat.None;

        public Int2 OutputSize { get; private set; }

        public PixelFormat RenderFormat { get; private set; } = PixelFormat.None;

        public string Category { get; private set; }

        public string Summary { get; private set; }

        public string Remarks { get; private set; }

        public string Tags { get; private set; }

        public ParsedShader ParsedShader { get; private set; }

        public bool IsTextureSource { get; private set; }
        
        public List<string> WantsMips { get; private set; }

        public List<string> DontConvertToLinearOnRead{ get; private set; }

        public bool DontConvertToSRgbOnOnWrite { get; private set; }

        public string FilePath { get; init; }

        public void GetPixelFormats(out PixelFormat outputFormat, out PixelFormat renderFormat)
        {

            if (IsTextureSource)
            {
                if (OutputFormat == PixelFormat.None)
                    outputFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                else
                    outputFormat = OutputFormat;

                if (DontConvertToSRgbOnOnWrite && RenderFormat == PixelFormat.None)
                    renderFormat = outputFormat.ToNonSRgb();
                else
                    renderFormat = RenderFormat;
            }
            else
            {
                outputFormat = OutputFormat;
                renderFormat = RenderFormat;
            }
        }

        public void GetOutputSize(out Int2 outputSize, out bool outputSizeVisible)
        {
            // default
            outputSize = IsTextureSource ? new Int2(512, 512) : Int2.Zero;

            // overwritten in shader?
            var hasOutputSize = OutputSize != Int2.Zero;
            if (hasOutputSize)
                outputSize = OutputSize;

            // visible if set in shader or is source
            outputSizeVisible = IsTextureSource || hasOutputSize;
        }

        public string GetCategory(string prefix)
        {
            var result = prefix;

            if (string.IsNullOrWhiteSpace(Category))
                return result;

            if (!Category.StartsWith(prefix))
                return prefix + "." + Category;

            return Category;
        }

        Dictionary<string, EnumMetadata> pinEnumTypes = new Dictionary<string, EnumMetadata>();
        HashSet<string> optionalPins = new HashSet<string>();

        private void AddEnumTypePinAttribute(string name, string enumTypeName, Expression initialValue)
        {
            var type = Type.GetType(enumTypeName);
            if (type != null && type.IsEnum)
            {
                object initalVal = Activator.CreateInstance(type);
                if (initialValue is LiteralExpression literal)
                {
                    var defaultText = literal.Text;
                    var converter = TypeDescriptor.GetConverter(Enum.GetUnderlyingType(type));

                    if (converter != null && converter.IsValid(defaultText))
                    {
                        var underVal = converter.ConvertFromString(defaultText);
                        initalVal = Enum.ToObject(type, underVal);
                    }
                }

                pinEnumTypes[name] = new EnumMetadata(type, initalVal);
            }
        }

        private void AddOptionalPinAttribute(string name)
        {
            optionalPins.Add(name);
        }

        Dictionary<string, string> pinSummaries = new Dictionary<string, string>();
        Dictionary<string, string> pinRemarks = new Dictionary<string, string>();
        Dictionary<string, string> pinAssets = new Dictionary<string, string>();

        private void AddPinSummary(string pinKeyName, string summary)
        {
            pinSummaries[pinKeyName] = summary;
        }

        private void AddPinRemarks(string pinKeyName, string remarks)
        {
            pinRemarks[pinKeyName] = remarks;
        }

        private void AddPinAsset(string pinKeyName, string assetURL)
        {
            pinAssets[pinKeyName] = assetURL;
        }

        public void GetPinDocuAndVisibility(ParameterKey key, out string summary, out string remarks, out bool isOptional)
        {
            var name = key.Name;
            summary = "";
            remarks = "";
            isOptional = false;

            if (key.PropertyType == typeof(ShaderSource) && ParsedShader != null)
            {
                if (ParsedShader.CompositionsWithBaseShaders.TryGetValue(key.GetVariableName(), out var composition))
                {
                    if (!string.IsNullOrWhiteSpace(composition.Summary))
                        summary = composition.Summary;

                    if (!string.IsNullOrWhiteSpace(composition.Remarks))
                        remarks = composition.Remarks;

                    isOptional = composition.IsOptional;
                }
            }
            else
            {
                if (pinSummaries.TryGetValue(name, out var sum))
                    summary = sum;

                if (pinRemarks.TryGetValue(name, out var rem))
                    remarks = rem;

                isOptional = optionalPins.Contains(name);
            }

            // add type in shader to pin summary, if not float, int or bool type
            var varName = key.GetVariableName();
            if (ParsedShader != null && ParsedShader.VariablesByName.TryGetValue(varName, out var variable))
            {
                var varType = variable.Type.ToString();
                if (!(varType.StartsWith("float", StringComparison.OrdinalIgnoreCase) 
                    || varType.StartsWith("int", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("bool", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("uint", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("Sampler", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("ComputeFloat", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("ComputeInt", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("ComputeBool", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("ComputeUInt", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("ComputeMatrix", StringComparison.OrdinalIgnoreCase)
                    || varType.StartsWith("matrix", StringComparison.OrdinalIgnoreCase)
                    ))
                {
                    summary += (string.IsNullOrWhiteSpace(summary) ? "" : Environment.NewLine) + varType;
                }
            }

        }

        public IEnumerable<(string textureName, bool wantsMips, bool dontUnapplySRgb)> GetTexturePinsToManage(IEnumerable<string> allTextureInputNames)
            => GetTexturePinsToManageInternal(allTextureInputNames).ToList();

        IEnumerable<(string textureName, bool wantsMips, bool dontUnapplySRgb)> GetTexturePinsToManageInternal(IEnumerable<string> allTextureInputNames)
        {
            var wantsMips = WantsMips?.Count > 0;
            var wantsSRgb = DontConvertToLinearOnRead != null;

            var mipPins = wantsMips ? WantsMips : Enumerable.Empty<string>();

            var srgbPins = Enumerable.Empty<string>();
            
            if (wantsSRgb)
            {
                if (DontConvertToLinearOnRead.Count > 0)
                    srgbPins = DontConvertToLinearOnRead;
                else
                    srgbPins = allTextureInputNames;
            }

            foreach (var textureName in allTextureInputNames)
            {
                var m = mipPins.Contains(textureName);
                var s = srgbPins.Contains(textureName);
                
                if (m || s)
                    yield return (textureName, m, s);
            }
        }

        /// <summary>
        /// Gets the type of the pin, if overwritten by an attribute, e.g. int -> enum.
        /// </summary>
        public Type GetPinType(ParameterKey key, out object runtimeDefaultValue, out object compilationDefaultValue)
        {
            runtimeDefaultValue = null;
            compilationDefaultValue = null;

            if (pinEnumTypes.TryGetValue(key.Name, out var enumTypeName))
            {
                runtimeDefaultValue = compilationDefaultValue = enumTypeName.defaultValue;
                return enumTypeName.typeName;
            }

            if (ParsedShader is null)
                return null;

            if (key.PropertyType == typeof(ShaderSource))
            {
                if (ParsedShader.CompositionsWithBaseShaders.TryGetValue(key.GetVariableName(), out var composition))
                {
                    compilationDefaultValue = composition.CompilationDefaultValue;
                    runtimeDefaultValue = composition.GetDefaultComputeNode(forPatch: true);
                    if (knownShaderFXTypes.TryGetValue(composition.TypeName, out var type))
                    {
                        return type;
                    }
                }

                return typeof(IComputeNode);
            }

            return null;
        }

        public Type GetShaderFXOutputType(out Type innerType)
        {
            innerType = typeof(VoidOrUnknown);
            foreach (var baseShader in ParsedShader?.BaseShaders ?? Enumerable.Empty<ParsedShader>())
            {
                var baseName = baseShader?.ShaderClass?.Name;
                if (!string.IsNullOrWhiteSpace(baseName))
                {
                    if (knownShaderFXTypes.TryGetValue(baseName, out var type))
                    {
                        if (type.IsGenericType)
                            innerType = type.GetGenericArguments()[0];

                        return type;
                    }
                }
            }

            return typeof(IComputeNode);
        }


        static Dictionary<string, Type> knownShaderFXTypes = new Dictionary<string, Type>()
        {
            { "ComputeVoid", typeof(ComputeVoid) },
            { "ComputeFloat", typeof(SetVar<float>) },
            { "ComputeFloat2", typeof(SetVar<Vector2>) },
            { "ComputeFloat3", typeof(SetVar<Vector3>) },
            { "ComputeFloat4", typeof(SetVar<Vector4>) },
            { "ComputeColor", typeof(SetVar<Color4>) },
            { "ComputeMatrix", typeof(SetVar<Matrix>) },
            { "ComputeBool", typeof(SetVar<bool>) },
            { "ComputeInt", typeof(SetVar<int>) },
            { "ComputeInt2", typeof(SetVar<Int2>) },
            { "ComputeInt3", typeof(SetVar<Int3>) },
            { "ComputeInt4", typeof(SetVar<Int4>) },
            { "ComputeUInt", typeof(SetVar<uint>) },
        };

        /// <summary>
        /// Determines whether the specified pin with the given key is optional.
        /// </summary>
        public bool IsOptional(ParameterKey key)
        {
            return optionalPins.Contains(key.Name);
        }

        //shader
        public const string CategoryName = "Category";
        public const string SummaryName = "Summary";
        public const string RemarksName = "Remarks";
        public const string TagsName = "Tags";
        public const string OutputFormatName = "OutputFormat";
        public const string OutputSizeName = "OutputSize";
        public const string RenderFormatName = "RenderFormat";
        public const string TextureSourceName = "TextureSource";
        public const string WantsMipsName = "WantsMips";
        public const string DontConvertToLinearOnReadName = "DontConvertToLinearOnRead";
        public const string DontConvertToSRgbOnName = "DontConvertToSRgbOnWrite";
        public const string ColorAttributeName = "Color";

        //pin
        public const string EnumTypeName = "EnumType";
        public const string OptionalName = "Optional";
        public const string DefaultName = "Default";
        public const string AssetName = "Asset";

        /// <summary>
        /// Registers the additional stride variable attributes. Avoids writing them to the final shader, which would create an error in the native platform compiler.
        /// </summary>
        public static void RegisterAdditionalShaderAttributes()
        {
            // only pin attributes need to be registered
            StrideAttributes.AvailableAttributes.Add(EnumTypeName);
            StrideAttributes.AvailableAttributes.Add(OptionalName);
            StrideAttributes.AvailableAttributes.Add(DefaultName);
            StrideAttributes.AvailableAttributes.Add(SummaryName);
            StrideAttributes.AvailableAttributes.Add(RemarksName);
            StrideAttributes.AvailableAttributes.Add(AssetName);
        }

        public static ShaderMetadata CreateMetadata(string effectName, IVirtualFileProvider fileProvider, ShaderSourceManager shaderSourceManager)
        {
            //create metadata with default values
            var shaderMetadata = new ShaderMetadata()
            {
                FilePath = EffectUtils.GetPathOfSdslShader(effectName, fileProvider)
            };

            //try to populate metdata with information form the shader
            if (fileProvider.TryParseEffect(effectName, shaderSourceManager, out var result))
            {
                shaderMetadata.ParsedShader = result;
                var shaderDecl = result.ShaderClass;

                if (shaderDecl != null)
                {
                    //shader 
                    foreach (var attr in shaderDecl.Attributes.OfType<AttributeDeclaration>())
                    {
                        switch (attr.Name)
                        {
                            case CategoryName:
                                shaderMetadata.Category = attr.ParseString();
                                break;
                            case SummaryName:
                                shaderMetadata.Summary = attr.ParseString();
                                break;
                            case RemarksName:
                                shaderMetadata.Remarks = attr.ParseString();
                                break;
                            case TagsName:
                                shaderMetadata.Tags = attr.ParseString();
                                break;
                            case OutputFormatName:
                                if (Enum.TryParse<PixelFormat>(attr.ParseString(), true, out var outputFormat))
                                    shaderMetadata.OutputFormat = outputFormat;
                                break;
                            case RenderFormatName:
                                if (Enum.TryParse<PixelFormat>(attr.ParseString(), true, out var renderFormat))
                                    shaderMetadata.RenderFormat = renderFormat;
                                break;
                            case OutputSizeName:
                                shaderMetadata.OutputSize = attr.ParseInt2();
                                break;
                            case TextureSourceName:
                                shaderMetadata.IsTextureSource = true;
                                break;
                            case WantsMipsName:
                                shaderMetadata.WantsMips = attr.ParseStringAsCommaSeparatedList();
                                break;
                            case DontConvertToLinearOnReadName:
                                shaderMetadata.DontConvertToLinearOnRead = attr.ParseStringAsCommaSeparatedList();
                                break;
                            case DontConvertToSRgbOnName:
                                shaderMetadata.DontConvertToSRgbOnOnWrite = true;
                                break;
                            default:
                                break;
                        }
                    }

                    //pins
                    var pinDecls = shaderDecl.Members.OfType<Variable>().Where(v => !v.Qualifiers.Contains(StrideStorageQualifier.Compose) && !v.Qualifiers.Contains(StrideStorageQualifier.Stream));
                    foreach (var pinDecl in pinDecls)
                    {
                        foreach (var attr in pinDecl.Attributes.OfType<AttributeDeclaration>())
                        {
                            switch (attr.Name)
                            {
                                case EnumTypeName:
                                    shaderMetadata.AddEnumTypePinAttribute(pinDecl.GetKeyName(shaderDecl), attr.ParseString(), pinDecl.InitialValue);
                                    break;
                                case OptionalName:
                                    shaderMetadata.AddOptionalPinAttribute(pinDecl.GetKeyName(shaderDecl));
                                    break;
                                case SummaryName:
                                    shaderMetadata.AddPinSummary(pinDecl.GetKeyName(shaderDecl), attr.ParseString());
                                    break;
                                case RemarksName:
                                    shaderMetadata.AddPinRemarks(pinDecl.GetKeyName(shaderDecl), attr.ParseString());
                                    break;
                                case DefaultName:
                                    // handled in composition parsing in ParseShader.cs
                                    break;
                                case AssetName:
                                    shaderMetadata.AddPinAsset(pinDecl.GetKeyName(shaderDecl), attr.ParseString());
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            return shaderMetadata;
        }

        class EnumMetadata
        {
            public readonly Type typeName;
            public readonly object defaultValue;

            public EnumMetadata(Type enumType, object boxedDefaultValue)
            {
                typeName = enumType;
                defaultValue = boxedDefaultValue;
            }
        }

    }
}
