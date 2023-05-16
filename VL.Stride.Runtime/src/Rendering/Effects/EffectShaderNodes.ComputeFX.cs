using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Model;
using VL.Stride.Rendering.ComputeEffect;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Rendering
{
    static partial class EffectShaderNodes
    {
        static IVLNodeDescription NewComputeEffectShaderNode(this IVLNodeDescriptionFactory factory, NameAndVersion name, string shaderName, ShaderMetadata shaderMetadata, IObservable<object> changes, Func<string> getFilePath, IServiceRegistry serviceRegistry, GraphicsDevice graphicsDevice)
        {
            return factory.NewNodeDescription(
                name: name,
                category: "Stride.Rendering.ComputeShaders",
                tags: shaderMetadata.Tags,
                fragmented: true,
                invalidated: changes,
                init: buildContext =>
                {
                    var _parameters = new ParameterCollection();
                    _parameters.Set(ComputeShaderBaseKeys.ThreadGroupCountGlobal, Int3.One);
                    _parameters.Set(ComputeEffectShaderKeys.ThreadNumbers, Int3.One);

                    var (_effect, _messages, shaderMixinSource) = 
                        CreateEffectInstance("ComputeFXEffect", shaderName, shaderMetadata, serviceRegistry, graphicsDevice, _parameters);

                    var _dispatcherInput = new PinDescription<IComputeEffectDispatcher>("Dispatcher");
                    var _threadNumbersInput = new PinDescription<Int3>("Thread Group Size", Int3.One);
                    var _inputs = new List<IVLPinDescription>()
                    {
                            _dispatcherInput,
                            _threadNumbersInput
                    };
                    var _outputs = new List<IVLPinDescription>() { buildContext.Pin("Output", typeof(IGraphicsRendererBase)) };

                    var usedNames = new HashSet<string>()
                    {
                            "Enabled"
                    };

                    foreach (var parameter in GetParameters(_effect))
                    {
                        _inputs.Add(CreatePinDescription(in parameter, usedNames, shaderMetadata));
                    }

                    IVLPinDescription _enabledInput;

                    _inputs.Add(_enabledInput = new PinDescription<bool>("Enabled", defaultValue: true));

                    return buildContext.Node(
                        inputs: _inputs,
                        outputs: _outputs,
                        messages: _messages,
                        summary: shaderMetadata.Summary,
                        remarks: shaderMetadata.Remarks,
                        filePath: getFilePath(),
                        newNode: nodeBuildContext =>
                        {
                            var gameHandle = AppHost.Current.Services.GetGameHandle();
                            var renderContext = RenderContext.GetShared(gameHandle.Resource.Services);
                            var mixinParams = BuildBaseMixin(shaderName, shaderMetadata, graphicsDevice, out var shaderMixinSource);
                            var effect = new VLComputeEffectShader(renderContext, shaderName, mixinParams);
                            var inputs = new List<IVLPin>();
                            var enabledInput = default(IVLPin);
                            foreach (var _input in _inputs)
                            {
                                    // Handle the predefined pins first
                                    if (_input == _dispatcherInput)
                                    inputs.Add(nodeBuildContext.Input<IComputeEffectDispatcher>(setter: v => effect.Dispatcher = v));
                                else if (_input == _threadNumbersInput)
                                    inputs.Add(nodeBuildContext.Input<Int3>(setter: v => effect.ThreadGroupSize = v));
                                else if (_input == _enabledInput)
                                    inputs.Add(enabledInput = nodeBuildContext.Input<bool>(v => effect.Enabled = v, effect.Enabled));
                                else if (_input is ParameterPinDescription parameterPinDescription)
                                    inputs.Add(parameterPinDescription.CreatePin(graphicsDevice, effect.Parameters));
                            }

                            var compositionPins = inputs.OfType<ShaderFXPin>().ToList();

                            var effectOutput = nodeBuildContext.Output(() =>
                            {
                                UpdateCompositions(compositionPins, graphicsDevice, effect.Parameters, shaderMixinSource, effect.Subscriptions);

                                return effect;
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
                        openEditorAction: () => OpenEditor(getFilePath)
                    );
                });
        }
    }
}
