using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Rendering;
using System.Reactive.Disposables;
using VL.Stride;

namespace VL.ImGui
{
    partial class ImGuiRenderer
    {
        IDisposable SubscribeToInputSource(IInputSource inputSource, RenderDrawContext context, Int2 RendertargetSize)
        {
            var inputManager = context.RenderContext.Services.GetService<InputManager>();
            _context.WithInputSource(inputManager, inputSource, RendertargetSize);

            if (inputSource is null)
                return Disposable.Empty;

            
            if (inputManager is null)
                return Disposable.Empty;

            return inputManager.GetNotifications(inputSource, sender: this)
                .Subscribe(notification =>
                {
                    using (_context.MakeCurrent())
                    {
                        _io.HandleNotification(notification);
                    }
                });
        }
    }
}
