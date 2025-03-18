using SkiaSharp;
using System;
using VL.Lib.Basics.Resources;

namespace VL.Skia
{
    public sealed class PictureRecorder : IDisposable
    {
        private const float LargeNumber = 1e9f;
        private readonly SKPictureRecorder pictureRecorder;
        private readonly Producing<SKPicture> output = new Producing<SKPicture>();

        public PictureRecorder()
        {
            pictureRecorder = new SKPictureRecorder();
        }

        public SKPicture Update(ILayer layer)
        {
            // Render
            using var canvas = pictureRecorder.BeginRecording(new SKRect(left: -LargeNumber, top: -LargeNumber, right: LargeNumber, bottom: LargeNumber));
            layer?.Render(CallerInfo.InRenderer(2 * LargeNumber, 2 * LargeNumber, canvas, null));
            var picture = output.Resource = pictureRecorder.EndRecording();

            return picture;
        }

        public void Dispose()
        {
            output.Dispose();
            pictureRecorder.Dispose();
        }
    }
}
