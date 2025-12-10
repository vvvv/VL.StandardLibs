using SkiaSharp;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using VL.Skia;
using VL.Stride;
using VL.Stride.Games;
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
                    _context.IO.HandleNotification(n, ClientToScreen);
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

        Vector2 ClientToScreen(Vector2 clientPos)
        {
            if (_window is null)
                return clientPos;
            if (_window.IsMain)
            {
                var windowPos = _window.Position;
                return new Vector2(clientPos.X + windowPos.X, clientPos.Y + windowPos.Y);
            }
            else
            {
                // Above code leads to strange feedback when moving the window - not sure why, probably because events get queued by Stride's InputManager?
                return VLGame.GetCursorPos();
            }
        }
    }
}
