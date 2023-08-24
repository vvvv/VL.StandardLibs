using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Model;
using VL.Stride.Graphics;
using VL.Stride.Engine;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Rendering
{
    static partial class EffectShaderNodes
    {
        const string textureInputName = "Input";
        const string samplerInputName = "Sampler";

        static IVLNodeDescription NewImageEffectShaderNode(this IVLNodeDescriptionFactory factory, NameAndVersion name, string shaderName, ShaderMetadata shaderMetadata, IObservable<object> changes, IServiceRegistry serviceRegistry, GraphicsDevice graphicsDevice)
        {
            return factory.NewNodeDescription(
                name: name,
                category: "Stride.Rendering.ImageShaders.Experimental.Advanced",
                tags: shaderMetadata.Tags,
                fragmented: true,
                invalidated: changes,
                init: buildContext =>
                {
                    var (_effect, _messages, shaderMixinSource) = 
                        CreateEffectInstance("TextureFXEffect", shaderName, shaderMetadata, serviceRegistry, graphicsDevice);

                    var _inputs = new List<IVLPinDescription>();
                    var _outputs = new List<IVLPinDescription>() { buildContext.Pin("Output", typeof(ImageEffectShader)) };

                    // The pins as specified by https://github.com/devvvvs/vvvv/issues/5756
                    var usedNames = new HashSet<string>()
                    {
                            "Output Size",
                            "Output Format",
                            "Output Texture",
                            "Enabled",
                            "Apply"
                    };

                    var _textureCount = 0;
                    var _samplerCount = 0;
                    var parameters = GetParameters(_effect).OrderBy(p => p.Key.Name.StartsWith("Texturing.Texture") ? 0 : 1).ToList();

                    //order sampler pins after their corresponding texture pins
                    var texturingSamplerPins = new Dictionary<ParameterKeyInfo, int>();
                    //find all samplers that have a corresponding texture
                    int insertOffset = 0;
                    foreach (var parameter in parameters)
                    {
                        if (parameter.Key.Name.StartsWith("Texturing.Sampler"))
                        {
                            var texturePinIdx = parameters.IndexOf(p => p.Key.Name == parameter.Key.Name.Replace("Sampler", "Texture"));
                            if (texturePinIdx >= 0)
                            {
                                texturingSamplerPins.Add(parameter, texturePinIdx + insertOffset);
                                insertOffset++;
                            }
                        }
                    }

                    //move the sampler pins after the corresponding texture pins
                    foreach (var samplerPin in texturingSamplerPins)
                    {
                        parameters.Remove(samplerPin.Key);
                        parameters.Insert(samplerPin.Value + 1, samplerPin.Key);
                    }

                    foreach (var parameter in parameters)
                    {
                        var key = parameter.Key;
                        var name = key.Name;

                        // Skip the matrix transform - we're drawing fullscreen
                        if (key == SpriteBaseKeys.MatrixTransform)
                            continue;

                        if (key.PropertyType == typeof(Texture))
                        {
                            var pinName = "";
                            if (shaderMetadata.IsTextureSource && !key.Name.StartsWith("Texturing.Texture"))
                                pinName = key.GetPinName(usedNames);
                            else
                                pinName = ++_textureCount == 1 ? textureInputName : $"{textureInputName} {_textureCount}";
                            usedNames.Add(pinName);
                            _inputs.Add(new ParameterKeyPinDescription<Texture>(pinName, (ParameterKey<Texture>)key));
                        }
                        else
                        {
                            var pinName = default(string); // Using null the name is based on the parameter name
                            var isOptional = false;
                            if (key.PropertyType == typeof(SamplerState) && key.Name.StartsWith("Texturing.Sampler"))
                            {
                                pinName = ++_samplerCount == 1 ? samplerInputName : $"{samplerInputName} {_samplerCount}";
                                usedNames.Add(pinName);
                                isOptional = true;
                            }
                            // also make other samplers from Texturing shader optional
                            else if (key.PropertyType == typeof(SamplerState) && key.Name.StartsWith("Texturing."))
                            {
                                isOptional = true;
                            }

                            _inputs.Add(CreatePinDescription(in parameter, usedNames, shaderMetadata, name: pinName, isOptionalOverride: isOptional));
                        }
                    }

                    IVLPinDescription _outputTextureInput, _enabledInput;

                    _inputs.Add(
                        _outputTextureInput = new PinDescription<Texture>("Output Texture")
                        {
                            Summary = "The texture to render to. If not set, the node creates its own output texture based on the input texture.",
                            Remarks = "The provided texture must be a render target.",
                            IsVisible = false
                        });
                    _inputs.Add(_enabledInput = new PinDescription<bool>("Enabled", defaultValue: true));

                    return buildContext.Node(
                        inputs: _inputs,
                        outputs: _outputs,
                        messages: _messages,
                        summary: shaderMetadata.Summary,
                        remarks: shaderMetadata.Remarks,
                        filePath: shaderMetadata?.FilePath,
                        newNode: nodeBuildContext =>
                        {
                            var gameHandle = AppHost.Current.Services.GetGameHandle();
                            var effect = new TextureFXEffect("TextureFXEffect") { Name = shaderName };

                            var context = BuildBaseMixin(shaderName, shaderMetadata, graphicsDevice, out var textureFXEffectMixin, effect.Parameters);

                            //effect.Parameters.Set
                            var inputs = new List<IVLPin>();
                            var enabledInput = default(IVLPin);
                            var textureCount = 0;
                            foreach (var _input in _inputs)
                            {
                                // Handle the predefined pins first
                                if (_input == _outputTextureInput)
                                {
                                    inputs.Add(nodeBuildContext.Input<Texture>(setter: t =>
                                    {
                                        if (t != null)
                                            effect.SetOutput(t);
                                    }));
                                }
                                else if (_input == _enabledInput)
                                    inputs.Add(enabledInput = nodeBuildContext.Input<bool>(v => effect.Enabled = v, effect.Enabled));
                                else if (_input is ParameterPinDescription parameterPinDescription)
                                    inputs.Add(parameterPinDescription.CreatePin(context));
                                else if (_input is ParameterKeyPinDescription<Texture> textureInput)
                                {
                                    if (textureInput.Key.Name.StartsWith("Texturing.Texture"))
                                    {
                                        var slot = textureCount++;
                                        inputs.Add(nodeBuildContext.Input<Texture>(setter: t =>
                                        {
                                            effect.SetInput(slot, t);
                                        }));
                                    }
                                    else
                                    {
                                        inputs.Add(nodeBuildContext.Input<Texture>(setter: t =>
                                        {
                                            effect.Parameters.SetObject(textureInput.Key, t);
                                        }));
                                    }
                                }
                            }

                            var compositionPins = inputs.OfType<ShaderFXPin>().ToList();

                            var effectOutput = ToOutput(nodeBuildContext, effect, () =>
                            {
                                UpdateCompositions(compositionPins, graphicsDevice, effect.Parameters, textureFXEffectMixin, effect.Subscriptions);
                            });
                            return nodeBuildContext.Node(
                                inputs: inputs,
                                outputs: new[] { effectOutput },
                                update: default,
                                dispose: () =>
                                {
                                    effect.Dispose();
                                    gameHandle.Dispose();
                                });
                        },
                        openEditorAction: () => OpenEditor(shaderMetadata)
                    );
                });
        }

        static IVLNodeDescription NewTextureFXNode(this IVLNodeDescriptionFactory factory, IVLNodeDescription shaderDescription, string name, ShaderMetadata shaderMetadata)
        {
            return factory.NewNodeDescription(
                name: name,
                category: shaderMetadata.GetCategory("Stride.Textures"),
                tags: shaderMetadata.Tags,
                fragmented: true,
                invalidated: shaderDescription.Invalidated,
                init: buildContext =>
                {
                    const string Enabled = "Enabled";

                    var _inputs = shaderDescription.Inputs.ToList();
                    var allTextureInputDescs = _inputs.OfType<ParameterKeyPinDescription<Texture>>().ToList();
                    var allTextureInputNames = allTextureInputDescs.Select(pd => pd.Key.GetVariableName()).ToList();
                    var texturePinsToManage = shaderMetadata.GetTexturePinsToManage(allTextureInputNames);
                    var hasTexturePinsToManage = texturePinsToManage.Count() > 0;

                    var isFilterOrMixer = !shaderMetadata.IsTextureSource;
                    shaderMetadata.GetOutputSize(out var defaultSize, out var outputSizeVisible);
                    shaderMetadata.GetPixelFormats(out var defaultFormat, out var defaultRenderFormat);

                    var _outputSize = new PinDescription<Int2>("Output Size", defaultSize) { IsVisible = outputSizeVisible };
                    var _outputFormat = new PinDescription<PixelFormat>("Output Format", defaultFormat) { IsVisible = false };
                    var _renderFormat = new PinDescription<PixelFormat>("Render Format", defaultRenderFormat) { IsVisible = false, Summary = "Allows to specify a render format that is differet to the output format" };

                        // mip manager pins
                        var wantsMips = shaderMetadata.WantsMips?.Count > 0;
                    if (wantsMips)
                    {
                        foreach (var textureName in shaderMetadata.WantsMips)
                        {
                            var texDesc = allTextureInputDescs.FirstOrDefault(p => p.Key.GetVariableName() == textureName);
                            if (texDesc != null)
                            {
                                var texIndex = _inputs.IndexOf(texDesc);
                                _inputs.Insert(texIndex + 1, new PinDescription<bool>("Always Generate Mips for " + texDesc.Name, true)
                                {
                                    Summary = "If true, mipmaps will be generated in every frame, if false only on change of the texture reference. If the texture has mipmaps, nothing will be done.",
                                    IsVisible = false
                                });
                            }
                        }
                    }

                    if (isFilterOrMixer)
                    {
                        // Filter or Mixer
                        _inputs.Insert(_inputs.Count - 1, _outputSize);
                        _inputs.Insert(_inputs.Count - 1, _outputFormat);
                        _inputs.Insert(_inputs.Count - 1, _renderFormat);

                        // Replace Enabled with Apply
                        var _enabledPinIndex = _inputs.IndexOf(p => p.Name == Enabled);
                        if (_enabledPinIndex >= 0)
                            _inputs[_enabledPinIndex] = new PinDescription<bool>("Apply", defaultValue: true);
                    }
                    else
                    {
                        // Pure source
                        _inputs.Insert(_inputs.Count - 2, _outputSize);
                        _inputs.Insert(_inputs.Count - 2, _outputFormat);
                        _inputs.Insert(_inputs.Count - 2, _renderFormat);
                    }


                    return buildContext.Node(
                        inputs: _inputs,
                        outputs: new[] { buildContext.Pin("Output", typeof(Texture)) },
                        messages: shaderDescription.Messages,
                        summary: shaderMetadata.Summary,
                        remarks: shaderMetadata.Remarks,
                        filePath: shaderDescription.FilePath,
                        newNode: nodeBuildContext =>
                        {
                            var nodeContext = nodeBuildContext.NodeContext;
                            var shaderNode = shaderDescription.CreateInstance(nodeContext);
                            var inputs = shaderNode.Inputs.ToList();
                            var mipmapManager = new TextureInputPinsManager(nodeContext);

                            var shaderNodeInputs = shaderDescription.Inputs.ToList();

                            // install pin managers for mipmaps or inputs that should be read in sRGB space
                            if (hasTexturePinsToManage)
                            {
                                foreach (var textureToManage in texturePinsToManage)
                                {
                                    var texDesc = allTextureInputDescs.FirstOrDefault(p => p.Key.GetVariableName() == textureToManage.textureName);
                                    if (texDesc != null)
                                    {
                                        var texIndex = shaderNodeInputs.IndexOf(texDesc);
                                        var shaderTexturePin = inputs.ElementAtOrDefault(texIndex) as IVLPin<Texture>;
                                        if (shaderTexturePin != null)
                                        {
                                            var newTexturePin = nodeBuildContext.Input<Texture>();

                                            // Replace this texture input with the new one
                                            inputs[texIndex] = newTexturePin;

                                            // Insert generate pin
                                            IVLPin<bool> alwaysGeneratePin = null;

                                            if (textureToManage.wantsMips)
                                            {
                                                alwaysGeneratePin = nodeBuildContext.Input(true);
                                                inputs.Insert(texIndex + 1, alwaysGeneratePin);
                                                shaderNodeInputs.Insert(texIndex + 1, new PinDescription<bool>("Always Generate Mips for " + texDesc.Name, true) { IsVisible = false }); //keep shader pin indices in sync
                                                }

                                                // Setup pin manager
                                                mipmapManager.AddInput(newTexturePin, shaderTexturePin, alwaysGeneratePin, textureToManage.dontUnapplySRgb, profilerName: name + " " + texDesc.Name + " Mipmap Generator");
                                        }
                                    }
                                }
                            }

                            var textureInput = inputs.ElementAtOrDefault(shaderNodeInputs.IndexOf(p => p.Name == textureInputName));
                            var outputTextureInput = inputs.ElementAtOrDefault(shaderNodeInputs.IndexOf(p => p.Name == "Output Texture"));
                            var enabledInput = (IVLPin<bool>)inputs.ElementAt(shaderNodeInputs.IndexOf(p => p.Name == Enabled));

                            var outputSize = nodeBuildContext.Input(defaultSize);
                            var outputFormat = nodeBuildContext.Input(defaultFormat);
                            var renderFormat = nodeBuildContext.Input(defaultRenderFormat);
                            if (isFilterOrMixer)
                            {
                                inputs.Insert(inputs.Count - 1, outputSize);
                                inputs.Insert(inputs.Count - 1, outputFormat);
                                inputs.Insert(inputs.Count - 1, renderFormat);
                            }
                            else
                            {
                                inputs.Insert(inputs.Count - 2, outputSize);
                                inputs.Insert(inputs.Count - 2, outputFormat);
                                inputs.Insert(inputs.Count - 2, renderFormat);
                            }

                            var gameHandle = AppHost.Current.Services.GetGameHandle();
                            var game = gameHandle.Resource;
                            var scheduler = game.Services.GetService<SchedulerSystem>();
                            var graphicsDevice = game.GraphicsDevice;

                            // Remove this once FrameDelay can deal with textures properly
                            var output1 = default(((Int2 size, PixelFormat format, PixelFormat renderFormat) desc, Texture texture, Texture view));
                            var output2 = default(((Int2 size, PixelFormat format, PixelFormat renderFormat) desc, Texture texture, Texture view));
                            var lastViewFormat = PixelFormat.None;
                            var usedRenderFormat = PixelFormat.None;
                            var mainOutput = nodeBuildContext.Output(getter: () =>
                            {
                                var inputTexture = textureInput?.Value as Texture;

                                if (!enabledInput.Value)
                                {
                                    if (isFilterOrMixer)
                                        return inputTexture; // By pass
                                    else
                                        return output1.texture; // Last result
                                }

                                var outputTexture = outputTextureInput.Value as Texture;
                                if (outputTexture is null)
                                {
                                    // No output texture is provided, generate one
                                    const TextureFlags textureFlags = TextureFlags.ShaderResource | TextureFlags.RenderTarget;
                                    var desc = (size: defaultSize, format: defaultFormat, renderFormat: defaultRenderFormat);
                                    if (inputTexture != null)
                                    {
                                        // Base it on the input texture
                                        var viewFormat = inputTexture.ViewFormat;

                                        // Figure out render format
                                        if (!shaderMetadata.IsTextureSource && shaderMetadata.DontConvertToSRgbOnOnWrite)
                                        {
                                            if (viewFormat != lastViewFormat)
                                                usedRenderFormat = viewFormat.ToNonSRgb();

                                            lastViewFormat = viewFormat;
                                        }
                                        else
                                        {
                                            usedRenderFormat = PixelFormat.None; //same as view format
                                        }

                                        desc = (new Int2(inputTexture.ViewWidth, inputTexture.ViewHeight), viewFormat, usedRenderFormat);

                                        // Watch out for feedback loops
                                        if (inputTexture == output1.texture)
                                        {
                                            Utilities.Swap(ref output1, ref output2);
                                        }
                                    }

                                    // Overwrite with user settings
                                    if (outputSize.Value.X > 0)
                                        desc.size.X = outputSize.Value.X;

                                    if (outputSize.Value.Y > 0)
                                        desc.size.Y = outputSize.Value.Y;

                                    if (outputFormat.Value != PixelFormat.None)
                                    {
                                        desc.format = outputFormat.Value;
                                        desc.renderFormat = outputFormat.Value;
                                    }

                                    if (renderFormat.Value != PixelFormat.None)
                                        desc.renderFormat = renderFormat.Value;

                                    // Ensure we have an output of proper size
                                    if (desc != output1.desc)
                                    {
                                        output1.view?.Dispose();
                                        output1.texture?.Dispose();
                                        output1.desc = desc;

                                        if (desc.format != PixelFormat.None && desc.size.X > 0 && desc.size.Y > 0)
                                        {
                                            if (desc.renderFormat != PixelFormat.None
                                            && desc.renderFormat != desc.format
                                            && desc.renderFormat.BlockSize() == desc.format.BlockSize()
                                            && desc.format.TryToTypeless(out var typelessFormat))
                                            {
                                                var td = TextureDescription.New2D(desc.size.X, desc.size.Y, typelessFormat, textureFlags);
                                                var tvd = new TextureViewDescription() { Format = desc.format, Flags = textureFlags };
                                                var rvd = new TextureViewDescription() { Format = desc.renderFormat, Flags = textureFlags };

                                                output1.texture = Texture.New(graphicsDevice, td, tvd);
                                                output1.view = output1.texture.ToTextureView(rvd);
                                            }
                                            else
                                            {
                                                output1.texture = Texture.New2D(graphicsDevice, desc.size.X, desc.size.Y, desc.format, textureFlags);
                                                output1.view = output1.texture;
                                            }
                                        }
                                        else
                                        {
                                            output1.texture = null;
                                            output1.view = null;
                                        }
                                    }
                                }
                                else //output texture set by patch
                                {
                                    output1.texture = outputTexture;
                                    output1.view = outputTexture;
                                }

                                var effect = shaderNode.Outputs[0].Value as TextureFXEffect;
                                if (scheduler != null && effect != null && output1.texture != null)
                                {
                                    effect.SetOutput(output1.view);
                                    if (hasTexturePinsToManage)
                                    {
                                        mipmapManager.Update();
                                        scheduler.Schedule(mipmapManager);
                                    }
                                    scheduler.Schedule(effect);
                                    return output1.texture;
                                }

                                return null;
                            });
                            return nodeBuildContext.Node(
                                inputs: inputs,
                                outputs: new[] { mainOutput },
                                dispose: () =>
                                {
                                    output1.view?.Dispose();
                                    output1.texture?.Dispose();
                                    output2.view?.Dispose();
                                    output2.texture?.Dispose();
                                    mipmapManager?.Dispose();
                                    gameHandle.Dispose();
                                    shaderNode.Dispose();
                                });
                        },
                        openEditorAction: shaderDescription.OpenEditorAction
                    );
                });
        }
    }
}
