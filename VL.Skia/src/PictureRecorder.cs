using SkiaSharp;
using System;
using Stride.Core.Mathematics;
using VL.Lib.Basics.Resources;

namespace VL.Skia
{
    public sealed class PictureRecorder : IDisposable
    {
        private const float LargeNumber = 1e9f;
        private readonly SKPictureRecorder pictureRecorder;
        private readonly RenderContext renderContext;
        private readonly Producing<SKPicture> output = new Producing<SKPicture>();

        public PictureRecorder()
        {
            renderContext = RenderContext.ForCurrentThread();
            pictureRecorder = new SKPictureRecorder();
        }

        public SKPicture Update(ILayer layer)
        {
            // Render
            using var canvas = pictureRecorder.BeginRecording(new SKRect(left: -LargeNumber, top: -LargeNumber, right: LargeNumber, bottom: LargeNumber));
            layer?.Render(CallerInfo.InRenderer(2 * LargeNumber, 2 * LargeNumber, canvas, null));
            var picture = output.Resource = pictureRecorder.EndRecording();

            // Further make the picture dependent on our render context - Skia doesn't track that dependency.
            renderContext.AddRef();
            picture.AfterDispose(() => renderContext.Release());

            return picture;
        }

        public void Dispose()
        {
            output.Dispose();
            pictureRecorder.Dispose();
            renderContext.Release();
        }
    }
}
