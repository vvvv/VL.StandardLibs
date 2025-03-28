#nullable enable
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Stride.Engine;
using VL.Stride.Games;

namespace VL.Stride.Rendering.PostFX;

/// <summary>
/// Applies an image effect on the given input texture. Some effects also need the depth buffer on the second input.
/// Use the optional output texture pin to render to a pre-existing target.
/// </summary>
/// <remarks>
/// The following filters expect the depth buffer on the second pin (press Ctrl-+ to add more pins): 
/// <see cref="AmbientOcclusion"/>, <see cref="Outline"/>, <see cref="TemporalAntiAliasEffect"/>, <see cref="LocalReflections"/>, <see cref="DepthOfField"/>, <see cref="Fog"/>
/// In addition <see cref="LocalReflections"/> also expects the normals and specular roughness buffers on the third and forth pins.
/// </remarks>
[ProcessNode]
public class ApplyImageEffect : IDisposable
{
    private readonly IResourceHandle<Game> _gameHandle;
    private readonly SchedulerSystem _schedulerSystem;
    private Texture? _outputTexture;

    public ApplyImageEffect()
    {
        _gameHandle = AppHost.Current.Services.GetGameHandle();
        _schedulerSystem = ((VLGame)_gameHandle.Resource).SchedulerSystem;
    }

    [return: Pin(Name = "Output")]
    public Texture? Update(
        [Pin(PinGroupKind = Model.PinGroupKind.Collection, PinGroupDefaultCount = 1)] Spread<Texture> input,
        [Pin(Visibility = Model.PinVisibility.Optional)] Texture? textureOutput,
        ImageEffect? effect)
    {
        if (effect is null)
            return null;

        var colorBuffer = input.ElementAtOrDefault(0);
        if (colorBuffer is null)
            return null;

        for (int i = 0; i < input.Count; i++)
            effect.SetInput(i, input[i]);

        var output = GetOutputTexture(colorBuffer, textureOutput);
        effect.SetOutput(output);

        // Fog and outline work differently than the rest. Let's fix it.
        _schedulerSystem.Schedule(GetFixedEffect(effect));

        return output;

        IGraphicsRendererBase GetFixedEffect(ImageEffect effect)
        {
            if (effect is Outline outline)
                return new OutlineFix(outline);
            if (effect is Fog fog)
                return new FogFix(fog);
            return effect;
        }
    }

    private Texture GetOutputTexture(Texture input, Texture? output)
    {
        if (output is null)
        {
            if (_outputTexture is null || _outputTexture.Description != input.Description)
            {
                _outputTexture?.Dispose();
                _outputTexture = Texture.New2D(_gameHandle.Resource.GraphicsDevice, input.Width, input.Height, input.Format, 
                    textureFlags: TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            }

            return _outputTexture;
        }
        else
        {
            if (_outputTexture is not null)
            {
                _outputTexture.Dispose();
                _outputTexture = null;
            }

            return output;
        }
    }

    public void Dispose()
    {
        _outputTexture?.Dispose();
        _gameHandle.Dispose();
    }

    private sealed class OutlineFix(Outline effect) : IGraphicsRendererBase
    {
        public void Draw(RenderDrawContext context)
        {
            if (effect.InputCount < 2)
                return;

            var color = effect.GetInput(0);
            var depth = effect.GetInput(1);
            effect.SetColorDepthInput(color, depth, context.RenderContext.RenderView.NearClipPlane, context.RenderContext.RenderView.FarClipPlane);
            effect.Draw(context);
        }
    }

    private sealed class FogFix(Fog effect) : IGraphicsRendererBase
    {
        public void Draw(RenderDrawContext context)
        {
            if (effect.InputCount < 2)
                return;

            var color = effect.GetInput(0);
            var depth = effect.GetInput(1);
            effect.SetColorDepthInput(color, depth, context.RenderContext.RenderView.NearClipPlane, context.RenderContext.RenderView.FarClipPlane);
            effect.Draw(context);
        }
    }
}
