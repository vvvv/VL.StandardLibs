using SharpFont.Cache;
using Stride.Input;
using Stride.Rendering;
using System;
using System.Reactive.Disposables;
using VL.Stride;

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

            return inputManager.GetNotifications(inputSource, sender: this)
                .Subscribe(notification =>
                {
                    using (_context.MakeCurrent())
                    {
                        System.Numerics.Vector2 position = new System.Numerics.Vector2(Viewport.HasValue ? Viewport.Value.X : 0, Viewport.HasValue ? Viewport.Value.Y : 0);
                        _io.HandleNotification(notification, position);
                    }
                });
        }
    }
}
