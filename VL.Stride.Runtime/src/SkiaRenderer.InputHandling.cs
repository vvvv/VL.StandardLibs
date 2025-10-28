using SkiaSharp;
using Stride.Input;
using Stride.Rendering;
using System;
using System.Reactive.Disposables;
using VL.Skia;

namespace VL.Stride
{
    partial class SkiaRenderer
    {
        IDisposable SubscribeToInputSource(IInputSource inputSource, RenderDrawContext context, SKCanvas canvas, GRContext graphicsContext, float scaling)
        {
            if (inputSource is null)
                return Disposable.Empty;

            var inputManager = context.RenderContext.Services.GetService<InputManager>();
            if (inputManager is null)
                return Disposable.Empty;

            var renderTarget = context.CommandList.RenderTarget;
            var callerInfo = CallerInfo.InRenderer(renderTarget.Width, renderTarget.Height, canvas, graphicsContext, scaling);
            return inputManager.GetNotifications(inputSource, sender: this)
                .Subscribe(n =>
                {
                    viewportLayer.Notify(n, callerInfo);
                });
        }
    }
}
