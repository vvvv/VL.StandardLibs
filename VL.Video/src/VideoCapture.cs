#nullable enable
using Stride.Core.Mathematics;
using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Model;
using VL.Video.CaptureControl;

namespace VL.Video
{
    /// <summary>Captures video from a selected video-capture device.</summary>
    [ProcessNode(Name = "VideoIn", Category = "Video", FragmentSelection = FragmentSelection.Explicit, Summary = "Capture video from USB cameras", Remarks = "Supports any camera that comes with a UVC 1.1 driver", Tags = "capture,stream,webcam,camera")]
    public sealed partial class VideoCapture : IVideoSource2
    {
        private readonly object syncRoot = new();

        /// <summary>Creates a video-capture source.</summary>
        [Fragment]
        public VideoCapture()
        {
        }

        private string? deviceLink;
        private Int2 preferredSize;
        private float preferredFps;
        private int changeTicket;
        private VideoCaptureImpl? currentCapture;
        private bool enabled;

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

        public bool Enabled
        {
            get => enabled;
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    changeTicket++;
                }
            }
        }

        public float ActualFPS => currentCapture?.ActualFPS ?? default;

        public string SupportedFormats => currentCapture?.SupportedFormats ?? string.Empty;

        int IVideoSource2.ChangedTicket => changeTicket;

        /// <summary>Configures the capture device and reports active capture details.</summary>
        /// <param name="device">Device to capture from. The Default entry lets the system choose a device.</param>
        /// <param name="preferredSize">Requested capture size. The default is 1920 by 1080.</param>
        /// <param name="preferredFps">Requested capture frame rate. The device may provide a different rate.</param>
        /// <param name="cameraControls">Optional camera-control values to apply; support depends on the device.</param>
        /// <param name="videoControls">Optional video-processing values to apply; support depends on the device.</param>
        /// <param name="enabled">Whether this capture source is enabled.</param>
        /// <param name="actualFps">Frame rate reported by the active capture, or zero when inactive.</param>
        /// <param name="supportedFormats">Formats reported by the active capture, or an empty string when inactive.</param>
        /// <returns>The configured video source.</returns>
        [Fragment]
        [return: Pin(Name = "Output")]
        public IVideoSource Update(
            [Pin(Name = "Device")] VideoCaptureDeviceEnumEntry? device,
            [Pin(Name = "Preferred Size"), DefaultValue(typeof(Int2), "1920, 1080")] Int2 preferredSize,
            [Pin(Name = "Preferred FPS"), DefaultValue(30f)] float preferredFps,
            [Pin(Name = "Camera Controls")] CameraControls cameraControls,
            [Pin(Name = "Video Controls")] VideoControls videoControls,
            [DefaultValue(true)] bool enabled,
            [Pin(Name = "Actual FPS")] out float actualFps,
            [Pin(Name = "Supported Formats")] out string supportedFormats)
        {
            Device = device;
            PreferredSize = preferredSize;
            PreferredFps = preferredFps;
            CameraControls = cameraControls;
            VideoControls = videoControls;
            Enabled = enabled;
            actualFps = ActualFPS;
            supportedFormats = SupportedFormats;
            return this;
        }

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            lock (syncRoot)
            {
                if (currentCapture != null && !enabled)
                    return null;

                var config = new VideoCaptureConfig(deviceLink, preferredSize, preferredFps, cameraControls, videoControls);
                if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    var device = ctx.GraphicsDeviceType == GraphicsDeviceType.Direct3D11 ? ctx.GraphicsDevice : default;
                    var capture = MF.MFVideoCaptureImpl.Create(config, device, useLinearFormat: ctx.UsesLinearColorspace);
                    if (capture is null)
                        return null;

                    capture.VideoCapture = this;
                    capture.DisposeAction = () =>
                    {
                        currentCapture = null;
                        // Let other sinks try to resubscribe
                        changeTicket++;
                    };
                    return currentCapture = capture;
                }

                throw new PlatformNotSupportedException();
            }
        }
    }
}
