using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using VL.Core;

namespace VL.Stride.Rendering
{
    class TexturePinManager : IDisposable
    {
        readonly NodeContext nodeContext;
        readonly IVLPin<Texture> texturePin;
        readonly IVLPin<Texture> shaderTexturePin;
        readonly IVLPin<bool> alwaysGeneratePin;
        readonly bool wantsMips;
        readonly bool dontUnapplySRgb;
        readonly string profilerName;

        Texture lastInputTexture;
        Texture inputTexture;
        Texture nonSRgbView;
        MipMapGenerator generator;
        bool copyToNonSRgb;
        bool renderMips;

        public TexturePinManager(NodeContext nodeContext, IVLPin<Texture> texturePin, IVLPin<Texture> shaderTexturePin, IVLPin<bool> alwaysGeneratePin, bool dontUnapplySRgb, string profilerName = "Pin MipMap Generator")
        {
            this.nodeContext = nodeContext;
            this.texturePin = texturePin;
            this.shaderTexturePin = shaderTexturePin;
            this.alwaysGeneratePin = alwaysGeneratePin;
            this.wantsMips = alwaysGeneratePin != null;
            this.dontUnapplySRgb = dontUnapplySRgb;
            this.profilerName = profilerName;
        }

        public void Update()
        {
            var currentInputTexture = texturePin.Value;
            var inputChanged = currentInputTexture != lastInputTexture;
            lastInputTexture = currentInputTexture;

            if (inputChanged)
            {
                inputTexture = currentInputTexture;

                if (dontUnapplySRgb)
                {
                    // clear non srgb view
                    copyToNonSRgb = false;
                    nonSRgbView?.Dispose();
                    nonSRgbView = null;

                    if (inputTexture != null)
                    {
                        var viewFormat = inputTexture.ViewFormat;
                        if (viewFormat.IsSRgb())
                        {
                            var resourceFormat = inputTexture.Format;

                            if (resourceFormat.IsTypeless()) // Simple case, typeless resource with sRGB view
                            {
                                nonSRgbView = inputTexture.ToTextureView(new TextureViewDescription() { Format = viewFormat.ToNonSRgb() });
                            }
                            else // needs a copy into a non sRGB texture
                            {
                                var desc = inputTexture.Description.ToCloneableDescription();
                                desc.Format = desc.Format.ToNonSRgb();
                                nonSRgbView = Texture.New(inputTexture.GraphicsDevice, desc);
                                copyToNonSRgb = true;
                            }

                            inputTexture = nonSRgbView;
                        } 
                    }
                }
            }

            // Input already has mips
            if (!wantsMips || inputTexture?.MipLevels > 1)
            {
                shaderTexturePin.Value = inputTexture;
                generator?.Dispose();
                generator = null;
                renderMips = false;
                return; //done
            }

            // Mips must be generated 
            generator ??= new MipMapGenerator(nodeContext) { Name = profilerName };

            generator.InputTexture = inputTexture;
            shaderTexturePin.Value = generator.OutputTexture;

            renderMips = inputChanged || alwaysGeneratePin.Value;

        }

        public void Draw(RenderDrawContext context)
        {
            if (copyToNonSRgb)
                context.CommandList.Copy(lastInputTexture, nonSRgbView);

            if (renderMips)
                generator.Draw(context);
        }

        public void Dispose()
        {
            nonSRgbView?.Dispose();
            nonSRgbView = null;
            generator?.Dispose();
            generator = null;
        }
    }

    public class TextureInputPinsManager : IGraphicsRendererBase, IDisposable
    {
        readonly NodeContext nodeContext;

        List<TexturePinManager> pins = new List<TexturePinManager>();

        public TextureInputPinsManager(NodeContext nodeContext)
        {
            this.nodeContext = nodeContext;
        }

        public void AddInput(IVLPin<Texture> texturePin, IVLPin<Texture> shaderTexturePin, IVLPin<bool> alwaysGeneratePin, bool dontUnapplySRgb, string profilerName)
        {
            pins.Add(new TexturePinManager(nodeContext, texturePin, shaderTexturePin, alwaysGeneratePin, dontUnapplySRgb, profilerName));
        }

        public void Update()
        {
            for (int i = 0; i < pins.Count; i++)
            {
                pins[i].Update();
            }
        }

        public void Draw(RenderDrawContext context)
        {
            for (int i = 0; i < pins.Count; i++)
            {
                pins[i].Draw(context);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < pins.Count; i++)
            {
                pins[i].Dispose();
            }

            pins.Clear();
        }
    }
}
