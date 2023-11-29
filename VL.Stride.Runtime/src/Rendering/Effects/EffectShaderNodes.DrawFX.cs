using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Diagnostics;
using VL.Model;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Rendering
{
    public static partial class EffectShaderNodes
    {
        static IVLNodeDescription NewDrawEffectShaderNode(this IVLNodeDescriptionFactory factory, 
            NameAndVersion name, string shaderName, ShaderMetadata shaderMetadata, IObservable<object> changes, 
            IServiceRegistry serviceRegistry, GraphicsDevice graphicsDevice)
        {
            return factory.NewNodeDescription(
                name: name,
                category: "Stride.Rendering.DrawShaders",
                tags: shaderMetadata.Tags,
                fragmented: true,
                invalidated: changes,
                init: buildContext => DrawEffectImpl(shaderName, shaderMetadata, serviceRegistry, graphicsDevice, buildContext, effectBytecode: null)
                );
        }

        // used by VL.Scalable library
        public static IVLNodeDescription NewDrawEffectShaderNode(this IVLNodeDescriptionFactory factory,
            NameAndVersion name, string shaderName, string category, EffectBytecode effectBytecode, GraphicsDevice graphicsDevice)
        {
            return factory.NewNodeDescription(
                name: name,
                category: category,
                fragmented: true,
                init: buildContext => DrawEffectImpl(shaderName, shaderMetadata: null, serviceRegistry: null, graphicsDevice, buildContext, effectBytecode)
                );
        }

        static NodeBuilding.NodeImplementation DrawEffectImpl(string shaderName, ShaderMetadata shaderMetadata,
            IServiceRegistry serviceRegistry, GraphicsDevice graphicsDevice, NodeBuilding.NodeDescriptionBuildContext buildContext,
            EffectBytecode effectBytecode)
        {
            var (effectInstance, messages, shaderMixinSource) = 
                CreateEffectInstance("DrawFXEffect", shaderName, shaderMetadata, serviceRegistry, graphicsDevice, effectBytecode: effectBytecode);

            var _inputs = new List<IVLPinDescription>();
            var _outputs = new List<IVLPinDescription>() { buildContext.Pin("Output", typeof(IEffect)) };

            var _parameterSetterInput = new PinDescription<Action<ParameterCollection, RenderView, RenderDrawContext>>("Parameter Setter");
            var _worldIn = default(PinDescription<Matrix>);

            var usedNames = new HashSet<string>() { _parameterSetterInput.Name };
            var needsWorld = false;
            foreach (var parameter in GetParameters(effectInstance))
            {
                var key = parameter.Key;
                var name = key.Name;

                if (WellKnownParameters.PerDrawMap.ContainsKey(name))
                {
                    // Expose World only - all other world dependent parameters we can compute on our own
                    needsWorld = true;
                    continue;
                }

                if (shaderMetadata != null)
                {
                    _inputs.Add(CreatePinDescription(in parameter, usedNames, shaderMetadata));
                }
                else
                { //binary shader
                    _inputs.Add(new ParameterPinDescription(usedNames, key, parameter.Count));
                }
            }

            if (needsWorld)
                _inputs.Add(_worldIn = new PinDescription<Matrix>(TransformationKeys.World.GetPinName(usedNames), Matrix.Identity));

            _inputs.Add(_parameterSetterInput);

            return buildContext.Node(
                inputs: _inputs,
                outputs: _outputs,
                messages: messages,
                summary: shaderMetadata?.Summary,
                remarks: shaderMetadata?.Remarks,
                filePath: shaderMetadata?.FilePath,
                newNode: nodeBuildContext =>
                {
                    var gameHandle = AppHost.Current.Services.GetGameHandle();
                    var game = gameHandle.Resource;

                    // create new effect instance with "better device"
                    graphicsDevice = game.GraphicsDevice;

                    (effectInstance, _, _) = 
                        CreateEffectInstance("DrawFXEffect", shaderName, shaderMetadata, serviceRegistry, graphicsDevice, effectBytecode: effectBytecode);
                    var effect = new CustomDrawEffect(effectInstance, graphicsDevice);
                    var context = new ShaderGeneratorContext(graphicsDevice) { Parameters = effect.Parameters };

                    var inputs = new List<IVLPin>();
                    foreach (var _input in _inputs)
                    {
                        // Handle the predefined pins first
                        if (_input == _parameterSetterInput)
                            inputs.Add(nodeBuildContext.Input<Action<ParameterCollection, RenderView, RenderDrawContext>>(v => effect.ParameterSetter = v));
                        else if (_input is ParameterPinDescription parameterPinDescription)
                            inputs.Add(parameterPinDescription.CreatePin(context));
                        else if (_input == _worldIn)
                            inputs.Add(effect.WorldIn = (IVLPin<Matrix>)_worldIn.CreatePin(context));
                    }

                    var compositionPins = inputs.OfType<ShaderFXPin>().ToList();


                    var effectOutput = nodeBuildContext.Output<IEffect>(() =>
                    {
                        if (shaderMixinSource != null)
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
                openEditorAction: () => OpenEditor(shaderMetadata)
            );
        }

        static bool OpenEditor(ShaderMetadata shaderMetadata)
        {
            var path = shaderMetadata?.FilePath;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
