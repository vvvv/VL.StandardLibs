#nullable enable
using Stride.Core.Mathematics;
using System;
using System.Reactive.Subjects;
using System.Threading;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Video.CaptureControl;

namespace VL.Video
{
    public sealed partial class VideoCapture : IVideoSource2
    {
        private string? deviceLink;
        private Int2 preferredSize;
        private float preferredFps;
        private int changeTicket;
        private VideoCaptureImpl? currentCapture;

        public VideoCaptureDeviceEnumEntry? Device
        {
            set
            {
                var deviceLink = value?.Tag as string;
                if (deviceLink != this.deviceLink)
                {
                    this.deviceLink = deviceLink;
                    changeTicket++;
                }
            }
        }

        public Int2 PreferredSize 
        { 
            set
            {
                if (value != preferredSize)
                {
                    preferredSize = value;
                    changeTicket++;
                }
            }
        }

        public float PreferredFps 
        { 
            set
            {
                if (value != preferredFps)
                {
                    preferredFps = value;
                    changeTicket++;
                }
            }
        }

        public CameraControls CameraControls
        {
            set
            {
                var v = value ?? CameraControls.Default;
                if (v != cameraControls.Value)
                    cameraControls.OnNext(v);
            }
        }
        readonly BehaviorSubject<CameraControls> cameraControls = new BehaviorSubject<CameraControls>(CameraControls.Default);

        public VideoControls VideoControls
        {
            set
            {
                var v = value ?? VideoControls.Default;
                if (v != videoControls.Value)
                    videoControls.OnNext(v);
            }
        }
        readonly BehaviorSubject<VideoControls> videoControls = new BehaviorSubject<VideoControls>(VideoControls.Default);

        public bool Enabled { get; set; }

        public float ActualFPS => currentCapture?.ActualFPS ?? default;

        public string SupportedFormats => currentCapture?.SupportedFormats ?? string.Empty;

        int IVideoSource2.ChangedTicket => changeTicket;

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            var config = new VideoCaptureConfig(deviceLink, preferredSize, preferredFps, cameraControls, videoControls);
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
            {
                var device = ctx.GraphicsDeviceType == GraphicsDeviceType.Direct3D11 ? ctx.GraphicsDevice : default;
                var capture = MF.MFVideoCaptureImpl.Create(config, device);
                if (capture is null)
                    return null;

                var previousCapture = Interlocked.Exchange(ref currentCapture, capture);
                capture.VideoCapture = this;
                capture.DisposeAction = () =>
                {
                    Interlocked.CompareExchange(ref currentCapture, previousCapture, capture);
                };
                return capture;
            }

            throw new PlatformNotSupportedException();
        }
    }
}
