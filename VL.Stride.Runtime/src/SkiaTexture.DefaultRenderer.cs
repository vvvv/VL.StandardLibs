#nullable enable

using Silk.NET.SDL;
using Stride.Graphics;
using System.Runtime.Versioning;
using VL.Core;
using VL.Skia;
using VL.Skia.Egl;
using VL.Stride.Games;
using Texture = Stride.Graphics.Texture;

namespace VL.Stride;

partial class SkiaTexture
{
    abstract class Renderer : IDisposable
    {
        protected readonly List<SkiaTexture> renderList = new();

        public Renderer(RenderContextProvider renderContextProvider)
        {
            RenderContextProvider = renderContextProvider;
        }

        public RenderContextProvider RenderContextProvider { get; }

        public void Schedule(SkiaTexture obj) => renderList.Add(obj);
        public abstract EglSurface CreateEglSurface(Texture texture);
        public abstract void Dispose();
    }

    /// <summary>
    /// Per-app-host service that renders Skia textures in batch during Stride's draw call.
    /// </summary>
    [SupportedOSPlatform("Windows6.1")]
    sealed unsafe class DefaultRenderer : Renderer
    {
        private readonly AppHost appHost;
        private readonly VLGame game;
        private readonly RenderContextProvider renderContextProvider;

        private DefaultRenderer(AppHost appHost, VLGame game, RenderContextProvider renderContextProvider) 
            : base(renderContextProvider)
        {
            this.appHost = appHost;
            this.game = game;
            this.renderContextProvider = renderContextProvider;

            if (OperatingSystem.IsWindowsVersionAtLeast(8))
                game.DrawStarted += OnDrawStarted;
        }

        public static DefaultRenderer GetOrCreate(AppHost appHost, VLGame game)
        {
            return appHost.Services.GetOrAddService(_ =>
            {
                var renderContextProvider = appHost.GetRenderContextProvider(dedicated: false);
                return new DefaultRenderer(appHost, game, renderContextProvider);
            }, allowToAskParent: false);
        }

        [SupportedOSPlatform("Windows8.0")]
        private void OnDrawStarted(object? sender, EventArgs e)
        {
            try
            {
                var renderContext = renderContextProvider.GetRenderContext();
                var eglContext = renderContext.EglContext;

                // Switch device context state once
                using (eglContext.SwitchDeviceContextState())
                {
                    // Now draw all of them
                    foreach (var texture in renderList)
                    {
                        texture.Render(renderContext);
                    }
                    // Unbind surface
                    eglContext.MakeCurrent(null);
                }

            }
            catch (Exception ex)
            {
                appHost.DefaultLogger.LogError(ex, "Error during SkiaTexture rendering");
            }
            finally
            {
                renderList.Clear();
            }
        }

        public override void Dispose()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(8))
                game.DrawStarted -= OnDrawStarted;
        }

        public override EglSurface CreateEglSurface(Texture texture)
        {
            var renderContext = renderContextProvider.GetRenderContext();
            var nativeTexture = (SharpDX.Direct3D11.Texture2D)SharpDXInterop.GetNativeResource(texture);
            return renderContext.EglContext.CreateSurfaceFromClientBuffer(nativeTexture.NativePointer);
        }
    }
}