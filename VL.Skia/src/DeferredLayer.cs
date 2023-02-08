using SkiaSharp;
using Stride.Core.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    public sealed class DeferredLayer : ILayer, IDisposable
    {
        private const float LargeNumber = 1e9f;
        private static readonly SKRect largeRect = new SKRect(left: -LargeNumber, top: -LargeNumber, right: LargeNumber, bottom: LargeNumber);
        private readonly SKPictureRecorder recorder = new SKPictureRecorder();
        private readonly ConcurrentQueue<INotification> notifications = new ConcurrentQueue<INotification>();

        private CallerInfo lastCallerInfo;
        private SKPicture latestRecording;

        private ILayer input;

        public RectangleF? Bounds => input?.Bounds;

        public void Update(ILayer input)
        {
            this.input = input;

            if (lastCallerInfo is null)
                return;

            using var canvas = recorder.BeginRecording(largeRect);

            var callerInfo = lastCallerInfo.WithGRContext(null).WithCanvas(canvas);
            while (notifications.TryDequeue(out var n))
                input?.Notify(n, callerInfo);

            // Transform of CallerInfo must match the one used by the canvas
            callerInfo = callerInfo.WithTransformation(SKMatrix.Identity);
            input?.Render(callerInfo);
            var recording = recorder.EndRecording();

            lock (recorder)
            {
                latestRecording?.Dispose();
                latestRecording = recording;
            }
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            notifications.Enqueue(notification);
            return false; // ???
        }

        public void Render(CallerInfo caller)
        {
            lastCallerInfo = caller;

            lock (recorder)
            {
                if (latestRecording != null)
                    caller.Canvas.DrawPicture(latestRecording);
            }
        }

        public void Dispose()
        {
            latestRecording?.Dispose();
            latestRecording = null;
            recorder.Dispose();
        }
    }
}
