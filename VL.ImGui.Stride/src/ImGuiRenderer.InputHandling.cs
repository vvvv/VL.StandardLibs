using SkiaSharp;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using VL.Skia;
using VL.Stride;
using CommonSpace = VL.Skia.CommonSpace;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        IDisposable SubscribeToInputSource(IInputSource inputSource, RenderDrawContext context)
        {
            if (inputSource is null)
                return Disposable.Empty;

            var inputManager = context.RenderContext.Services.GetService<InputManager>();
            if (inputManager is null)
                return Disposable.Empty;

            // Mimic the behavior of the Skia render path
            var viewport = context.RenderContext.ViewportState.Viewport0;
            var viewportLayer = new InViewportUpstream();
            var withinCommonSpaceLayer = new SetSpaceUpstream2();

            var self = new SetNotify();
            self.Update(null, (n, c) =>
            {
                using (_context.MakeCurrent())
                {
                    _context.IO.HandleNotification(n);
                }
                return n.Handled;
            }, out _);

            withinCommonSpaceLayer.Update(self, out var spaceLayer, CommonSpace.DIPTopLeft);
            viewportLayer.Update(spaceLayer, SKRect.Create(viewport.X, viewport.Y, viewport.Width, viewport.Height), CommonSpace.PixelTopLeft, out _);

            var callerInfo = CallerInfo.InRenderer(viewport.Width, viewport.Height, canvas: null, context: null);
            return inputManager.GetNotifications(inputSource, sender: this)
                .Subscribe(n =>
                {
                    viewportLayer.Notify(n, callerInfo);
            });
        }
    }
}
