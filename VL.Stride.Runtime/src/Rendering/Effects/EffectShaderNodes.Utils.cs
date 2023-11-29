using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Stride.Shaders.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Diagnostics;
using VL.Stride.Effects;
using VL.Stride.Shaders;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride.Rendering
{
    static partial class EffectShaderNodes
    {
        static bool IsShaderFile(string file) => string.Equals(Path.GetExtension(file), ".sdsl", StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetExtension(file), ".sdfx", StringComparison.OrdinalIgnoreCase);

        static NameAndVersion GetNodeName(string effectName, string suffix)
        {
            // Levels_ClampBoth_TextureFX
            var name = effectName.Substring(0, effectName.Length - suffix.Length);
            // Levels_ClampBoth
            var nameParts = name.Split('_');
            if (nameParts.Length > 0)
            {
                name = nameParts[0];
                return new NameAndVersion(name, string.Join(" ", nameParts.Skip(1)));
            }
            return new NameAndVersion(name);
        }

        static bool IsNewOrDeletedShaderFile(FileSystemEventArgs e)
        {
            // Check for shader files only. Editor (like VS) create lot's of other temporary files.
            if (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Deleted)
                return IsShaderFile(e.Name);
            // Also the old name must be a shader file. We're not interested in weired renamings by VS.
            if (e.ChangeType == WatcherChangeTypes.Renamed && e is RenamedEventArgs r)
                return IsShaderFile(e.Name) && IsShaderFile(r.OldName);
            return false;
        }

        static (EffectInstance effect, ImmutableArray<Message> messages, ShaderMixinSource shaderMixinSource) CreateEffectInstance(string effectName, string shaderName,
            ShaderMetadata shaderMetadata, IServiceRegistry serviceRegistry, GraphicsDevice graphicsDevice, ParameterCollection parameterCollection = null, EffectBytecode effectBytecode = null)
        {
            var messages = ImmutableArray<Message>.Empty;
            EffectInstance effect = default;
            ShaderMixinSource shaderMixinSource = default;
            try
            {
                if (effectBytecode != null)
                    effect = new EffectInstance(new Effect(graphicsDevice, effectBytecode) { Name = effectName });
                else
                {
                    var context = BuildBaseMixin(shaderName, shaderMetadata, graphicsDevice, out shaderMixinSource, parameterCollection);
                    effect = new DynamicEffectInstance(effectName, context.Parameters);
                    (effect as DynamicEffectInstance).Initialize(serviceRegistry);
                }

                effect.UpdateEffect(graphicsDevice);
            }
            catch (InvalidOperationException e)
            {
                messages = messages.Add(new Message(MessageType.Error, e.Message));
            }

            return (effect, messages, shaderMixinSource);
        }

        static IEnumerable<ParameterKeyInfo> GetParameters(EffectInstance effectInstance)
        {
            var byteCode = effectInstance.Effect?.Bytecode;
            if (byteCode is null)
                yield break;

            var layoutNames = byteCode.Reflection.ResourceBindings.Select(x => x.ResourceGroup ?? "Globals").Distinct().ToList();
            var parameters = effectInstance.Parameters;
            var compositionParameters = parameters.ParameterKeyInfos.Where(pki => pki.Key.PropertyType == typeof(ShaderSource) && pki.Key.Name != "EffectNodeBase.EffectNodeBaseShader");
            foreach (var parameter in parameters.Layout.LayoutParameterKeyInfos.Concat(compositionParameters))
            {
                var key = parameter.Key;
                var name = key.Name;

                // Skip constant buffers
                if (layoutNames.Contains(name))
                    continue;

                // Skip compiler injected paddings
                if (name.Contains("_padding_"))
                    continue;

                // Skip well known parameters
                if (WellKnownParameters.PerFrameMap.ContainsKey(name)
                    || WellKnownParameters.PerViewMap.ContainsKey(name)
                    || WellKnownParameters.TexturingMap.ContainsKey(name))
                    continue;

                // Skip inputs from ShaderFX graph
                if (name.StartsWith("ShaderFX.Input"))
                    continue;

                yield return parameter;
            }
        }

        private static ShaderGeneratorContext BuildBaseMixin(string shaderName, ShaderMetadata shaderMetadata, GraphicsDevice graphicsDevice, out ShaderMixinSource effectInstanceMixin, ParameterCollection parameters = null)
        {
            effectInstanceMixin = new ShaderMixinSource();
            effectInstanceMixin.Mixins.Add(new ShaderClassSource(shaderName));

            var mixinParams = parameters ?? new ParameterCollection();
            mixinParams.Set(EffectNodeBaseKeys.EffectNodeBaseShader, effectInstanceMixin);

            var context = new ShaderGeneratorContext(graphicsDevice)
            {
                Parameters = mixinParams,
            };

            //add composition parameters to parameters
            if (shaderMetadata.ParsedShader != null)
            {
                foreach (var compKey in shaderMetadata.ParsedShader.CompositionsWithBaseShaders)
                {
                    var comp = compKey.Value;
                    var shaderSource = comp.GetDefaultShaderSource(context, baseKeys);
                    effectInstanceMixin.AddComposition(comp.Name, shaderSource);
                    mixinParams.Set(comp.Key, shaderSource);
                } 
            }

            return context;
        }

        //used for shader source generation
        static MaterialComputeColorKeys baseKeys = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);

        // check composition pins
        private static bool UpdateCompositions(IReadOnlyList<ShaderFXPin> compositionPins, GraphicsDevice graphicsDevice, ParameterCollection parameters, ShaderMixinSource mixin, CompositeDisposable subscriptions)
        {
            var anyChanged = false;
            for (int i = 0; i < compositionPins.Count; i++)
            {
                anyChanged |= compositionPins[i].ShaderSourceChanged;
            }

            if (anyChanged)
            {
                // Disposes all current subscriptions. So for example all data bindings between the sources and our parameter collection
                // gets removed.
                subscriptions.Clear();

                var context = ShaderGraph.NewShaderGeneratorContext(graphicsDevice, parameters, subscriptions);

                var updatedMixin = new ShaderMixinSource();
                updatedMixin.DeepCloneFrom(mixin);
                for (int i = 0; i < compositionPins.Count; i++)
                {
                    var cp = compositionPins[i];
                    cp.GenerateAndSetShaderSource(context, baseKeys, updatedMixin);
                }
                parameters.Set(EffectNodeBaseKeys.EffectNodeBaseShader, updatedMixin);

                return true;
            }

            return false;
        }

        static IVLPin ToOutput<T>(NodeBuilding.NodeInstanceBuildContext c, T value, Action getter)
        {
            return c.Output(() =>
            {
                getter();
                return value;
            });
        }

        static ShaderSource GetShaderSource(string effectName)
        {
            var isMixin = ShaderMixinManager.Contains(effectName);
            if (isMixin)
                return new ShaderMixinGeneratorSource(effectName);
            return new ShaderClassSource(effectName);
        }

        // Not used yet
        static Dictionary<string, Dictionary<string, ParameterKey>> GetCompilerParameters(string filePath, string sdfxEffectName)
        {
            // In .sdfx, shader has been renamed to effect, in order to avoid ambiguities with HLSL and .sdsl
            var macros = new[]
            {
                    new global::Stride.Core.Shaders.Parser.ShaderMacro("shader", "effect")
                };

            // Parse and collect
            var shader = StrideShaderParser.PreProcessAndParse(filePath, macros);
            var builder = new RuntimeShaderMixinBuilder(shader);
            return builder.CollectParameters();
        }

    }
}
