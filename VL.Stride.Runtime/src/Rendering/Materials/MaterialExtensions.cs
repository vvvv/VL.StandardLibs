using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride.Rendering
{
    /// <summary>
    /// Extension methods for <see cref="Material"/>.
    /// </summary>
    public static class MaterialExtensions
    {
        /// <summary>
        /// Clone the <see cref="Material"/>.
        /// </summary>
        /// <param name="material">The material to clone.</param>
        /// <returns>The cloned material.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="material"/> is <see langword="null"/>.</exception>
        public static Material Clone(this Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            var clone = new Material();

            CopyProperties(material, clone);

            return clone;
        }

        internal static void CopyProperties(Material material, Material clone)
        {
            foreach (var pass in material.Passes)
            {
                clone.Passes.Add(new MaterialPass()
                {
                    HasTransparency = pass.HasTransparency,
                    BlendState = pass.BlendState,
                    CullMode = pass.CullMode,
                    IsLightDependent = pass.IsLightDependent,
                    TessellationMethod = pass.TessellationMethod,
                    PassIndex = pass.PassIndex,
                    Parameters = new ParameterCollection(pass.Parameters)
                });
            }
        }

        /// <summary>
        /// Same as Material.New but also loading referenced content in parameter collection (like EnvironmentLightingDFG_LUT)
        /// as well as setting the <see cref="ShaderGraph.GraphSubscriptions"/> on the used <see cref="MaterialGeneratorContext"/>.
        /// </summary>
        internal static Material New(GraphicsDevice device, MaterialDescriptor descriptor, ContentManager content, CompositeDisposable subscriptions)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            if (subscriptions == null) throw new ArgumentNullException(nameof(subscriptions));

            // The descriptor is not assigned to the material because
            // 1) we don't know whether it will mutate and be used to generate another material
            // 2) we don't wanna hold on to memory we actually don't need
            var context = new MaterialGeneratorContext(new Material(), device)
            {
                GraphicsProfile = device.Features.RequestedProfile,
            };

            // Allows nodes in the graph to tie the lifetime of services to the graph itself
            context.Tags.Set(ShaderGraph.GraphSubscriptions, subscriptions);

            var result = MaterialGenerator.Generate(descriptor, context, string.Format("{0}:RuntimeMaterial", descriptor.MaterialId));

            if (result.HasErrors)
            {
                throw new InvalidOperationException(string.Format("Error when creating the material [{0}]", result.ToText()));
            }

            var m = result.Material;

            // Attach the descriptor (not sure why Stride is not doing that on its own) as its needed for material layers
            m.Descriptor = descriptor;

            foreach (var pass in m.Passes)
            {
                //var t = pass.Parameters.Get(MaterialSpecularMicrofacetEnvironmentGGXLUTKeys.EnvironmentLightingDFG_LUT);
                //if (t != null)
                //{
                //    var reference = AttachedReferenceManager.GetAttachedReference(t);
                //    var realT = content.Load<Texture>(reference.Url, ContentManagerLoaderSettings.StreamingDisabled);
                //    pass.Parameters.Set(MaterialSpecularMicrofacetEnvironmentGGXLUTKeys.EnvironmentLightingDFG_LUT, realT);
                //}

                foreach (var p in pass.Parameters.ParameterKeyInfos)
                {
                    var key = p.Key;
                    if (key.Type != ParameterKeyType.Object)
                        continue;
                    var value = pass.Parameters.GetObject(key);
                    if (value is null)
                        continue;
                    var reference = AttachedReferenceManager.GetAttachedReference(value);
                    if (reference is null)
                        continue;

                    if (content.Exists(reference.Url))
                    {
                        var c = content.Load(key.PropertyType, reference.Url, ContentManagerLoaderSettings.StreamingDisabled);
                        if (c is null)
                            continue;
                        pass.Parameters.SetObject(key, c); 
                    }
                }
            }
            return m;
        }
    }
}
