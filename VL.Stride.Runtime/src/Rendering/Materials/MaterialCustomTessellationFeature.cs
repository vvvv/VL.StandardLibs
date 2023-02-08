using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Shaders;
using Stride.Rendering;
using Stride.Rendering.Materials;
using VL.Lib.Collections;

namespace VL.Stride.Rendering.Materials
{
    /// <summary>
    /// Material for flat (dicing) tessellation.    
    /// </summary>
    [DataContract("MaterialCustomTessellation")]
    [Display("CustomTessellation")]
    public class MaterialCustomTessellationFeature : MaterialTessellationBaseFeature
    {
        /// <summary>
        /// Gets or sets MaterialTessellationStream.
        /// </summary>
        /// <userdoc>
        /// This is the name of the stream that will be arive in HSMain, HSConstantMain and DSMain.
        /// </userdoc>
        [DataMember(30)]
        [Display("TessellationStreams")]
        public string TessellationStream { get; set; }

        /// <summary>
        /// Gets or sets MaterialTessellationStream.
        /// </summary>
        /// <userdoc>
        /// This is the name of the stream that will be arive in HSMain, HSConstantMain and DSMain.
        [DataMember(40)]
        [Display("TessellationShadere")]
        public IComputeNode TessellationShader { get; set; }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            base.GenerateShader(context);

            if (hasAlreadyTessellationFeature)
                return;

            // reset the tessellation stream at the beginning of the stage
            context.AddStreamInitializer(MaterialShaderStage.Domain, TessellationStream);


            // set the tessellation method used enumeration
            context.MaterialPass.TessellationMethod |= StrideTessellationMethod.None;

            // create and affect the shader source
            // var tessellationShader = new ShaderMixinSource();
            // tessellationShader.Mixins.Add(new ShaderClassSource("TessellationFlat"));
            var enableExtension = TessellationShader != null;

            if (enableExtension)
            {


                var ext = TessellationShader;

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
                

                var shaderSource = TessellationShader.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White));

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
    }
}

