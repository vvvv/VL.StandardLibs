using Avalonia;
using Avalonia.Controls.Embedding;
using Avalonia.Skia;
using Stride.Core.Mathematics;
using System;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.AvaloniaUI
{
    sealed class AvaloniaLayer : ILayer, IDisposable
    {
        private readonly RootElementImpl rootImpl;
        private readonly EmbeddableControlRoot controlRoot;

        public AvaloniaLayer()
        {
            Initialization.InitAvalonia();
            rootImpl = new RootElementImpl();
            controlRoot = new EmbeddableControlRoot(rootImpl);
        }

        public object Content
        {
            get => controlRoot.Content;
            set => controlRoot.Content = value;
        }

        public void Dispose()
        {
            controlRoot.Dispose();
        }

        public RectangleF? Bounds => default;

        public bool Notify(INotification notification, CallerInfo caller)
        {
            return rootImpl.Notify(notification, caller);
        }

        public void Render(CallerInfo caller)
        {
            if (!controlRoot.IsInitialized)
            {
                rootImpl.ClientSize = caller.ViewportBounds.ToAvaloniaRect().Size;
                controlRoot.Prepare();
            }

            rootImpl.Render(caller);
        }
    }
}
