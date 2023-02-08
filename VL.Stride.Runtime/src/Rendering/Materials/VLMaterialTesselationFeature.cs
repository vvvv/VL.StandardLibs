using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;
using VL.Stride.Shaders.ShaderFX;
using VL.Stride.Shaders.ShaderFX.Control;

namespace VL.Stride.Rendering.Materials
{
    public class VLMaterialTessellationFeature : IMaterialTessellationFeature
    {

        public IComputeNode MaterialExtension { get; set; }
        
        public IMaterialTessellationFeature MaterialTessellationFeature { get; set; }

        public string MaterialTessellationStream { get; set; }

        public bool Enabled { get; set; } = true;

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return other is MaterialTessellationBaseFeature;
        }

        public void Visit(MaterialGeneratorContext context)
        {
            MaterialTessellationFeature?.Visit(context);

            if (Enabled && context.Step == MaterialGeneratorStep.GenerateShader)
            {
                AddMaterialExtension(context);
            }
        }

        void AddMaterialExtension(MaterialGeneratorContext context)
        {
            var enableExtension = MaterialExtension != null;

            if (enableExtension)
            {   
                // reset the tessellation stream at the beginning of the stage
                //context.AddStreamInitializer(MaterialShaderStage.Domain, "MaterialTessellationStream");
                context.AddStreamInitializer(MaterialShaderStage.Domain, MaterialTessellationStream);


                var ext = MaterialExtension;
                
                if (ext is IShaderFXNode node) // check for ShaderFX node
                {
                    var compositionPins = node.InputPins;
                    var baseKeys = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
                    
                    for (int i = 0; i < compositionPins.Count; i++)
                    {
                        var cp = compositionPins[i];

                        cp?.GenerateAndSetShaderSource(context, baseKeys);
                    }
                }

                var shaderSource = MaterialExtension.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White));

                if (shaderSource is ShaderMixinSource mixinSource)
                {
                    if (ext is IShaderFXNode node2) // check for ShaderFX node
                    {
                        var compositionPins = node2.InputPins;

                        for (int i = 0; i < compositionPins.Count; i++)
                        {
                            var cp = compositionPins[i];

                            var shader = context.Parameters.Get(cp.Key);

                            if (shader is ShaderSource classCode)
                                mixinSource.AddComposition(cp.Key.Name, classCode);
                        }
                    }
                }

                context.Parameters.Set(MaterialKeys.TessellationShader, shaderSource);
            }

        }


        //// takes care of the composition inputs of the connected node
        //private static bool UpdateCompositions(IReadOnlyList<ShaderFXPin> compositionPins, GraphicsDevice graphicsDevice, ParameterCollection parameters, ShaderMixinSource mixin, CompositeDisposable subscriptions)
        //{
        //    var anyChanged = false;
        //    for (int i = 0; i < compositionPins.Count; i++)
        //    {
        //        anyChanged |= compositionPins[i].ShaderSourceChanged;
        //    }

        //    if (anyChanged)
        //    {
        //        // Disposes all current subscriptions. So for example all data bindings between the sources and our parameter collection
        //        // gets removed.
        //        subscriptions.Clear();

        //        var context = ShaderGraph.NewShaderGeneratorContext(graphicsDevice, parameters, subscriptions);

        //        var updatedMixin = new ShaderMixinSource();
        //        updatedMixin.DeepCloneFrom(mixin);
                
        //        parameters.Set(EffectNodeBaseKeys.EffectNodeBaseShader, updatedMixin);

        //        return true;
        //    }

        //    return false;
        //}
    }
}
