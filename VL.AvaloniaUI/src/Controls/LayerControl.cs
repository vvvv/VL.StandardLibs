using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Skia;
using VL.Skia;

namespace VL.AvaloniaUI.Controls
{
    sealed class LayerControl : Control
    {
        static LayerControl()
        {
            AffectsRender<LayerControl>(LayerProperty);
        }

        public static readonly StyledProperty<ILayer> LayerProperty = AvaloniaProperty.Register<LayerControl, ILayer>(nameof(Layer));
        public ILayer Layer { get; set; }

        public override void Render(DrawingContext context)
        {
            if (context.PlatformImpl is ISkiaDrawingContextImpl skiaDrawingContext)
                Layer?.Render(CallerInfo.InRenderer((int)Width, (int)Height, skiaDrawingContext.SkCanvas, skiaDrawingContext.GrContext));

            base.Render(context);
        }

        // TODO: Translate events
    }
}
