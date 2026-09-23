#nullable enable
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.Versioning;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Media.MediaFoundation;

using static Windows.Win32.PInvoke;

namespace VL.Video.MF
{
    // Good source: https://stackoverflow.com/questions/40913196/how-to-properly-use-a-hardware-accelerated-media-foundation-source-reader-to-dec
    [SupportedOSPlatform("windows6.1")]
    internal sealed class MFVideoCaptureImpl : VideoCaptureImpl
    {
        public unsafe static MFVideoCaptureImpl? Create(VideoCaptureConfig config, IntPtr device)
        {
            using var _ = MediaFoundation.Use();

            // Create the media source based on the selected symbolic link
            var mediaSource = CreateMediaSource(config.DeviceSymbolicLink);
            if (mediaSource is null)
                return null;

            try
            {
                // Find best capture format for device
                using var formats = Utils.EnumerateCaptureFormats(mediaSource);
                var bestCaptureFormat = formats.Values
                    .FirstOrDefault(f => f.size == config.PreferredSize && f.fr == config.PreferredFps);
                if (bestCaptureFormat != null)
                    Utils.SetCaptureFormat(mediaSource, bestCaptureFormat.mediaType);

                var sourceReader = SourceReader.CreateFromMediaSource(mediaSource, (ID3D11Device*)device, readAsync: true);

                return new MFVideoCaptureImpl(sourceReader, mediaSource, config);
            }
            finally
            {
                mediaSource->Release();
            }

            static IMFMediaSource* CreateMediaSource(string? deviceSymbolicLink)
            {
                if (!string.IsNullOrEmpty(deviceSymbolicLink))
                {
                    // Use symbolic link (https://docs.microsoft.com/en-us/windows/win32/medfound/audio-video-capture-in-media-foundation)
                    IMFAttributes* mediaSourceAttributes;
                    MFCreateAttributes(&mediaSourceAttributes, 1).ThrowOnFailure();
                    mediaSourceAttributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
                    mediaSourceAttributes->SetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, deviceSymbolicLink);
                    IMFMediaSource* mediaSource;
                    MFCreateDeviceSource(mediaSourceAttributes, &mediaSource);
                    mediaSourceAttributes->Release();
                    return mediaSource;
                }
                else
                {
                    // Auto select
                    using var devices = Utils.EnumerateVideoDevices();
                    if (devices.Count == 0)
                        return null;
                    return (IMFMediaSource*)devices[0]->ActivateObject(in IMFMediaSource.IID_Guid);
                }
            }
        }

        const uint firstVideoStream = unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM);

        private readonly SourceReader sourceReader;
        private readonly CompositeDisposable controlSubscription;
        private readonly string supportedFormats;

        private unsafe MFVideoCaptureImpl(SourceReader sourceReader, IMFMediaSource* mediaSource, VideoCaptureConfig config)
        {
            this.sourceReader = sourceReader;

            // Connect camera and video controls
            controlSubscription = new CompositeDisposable(
                Utils.Subscribe(mediaSource, config.CameraControls),
                Utils.Subscribe(mediaSource, config.VideoControls));

            supportedFormats = Utils.GetSupportedFormats(mediaSource);
        }

        public override float ActualFPS => Utils.ToFrameRate(sourceReader.FrameRate.n, sourceReader.FrameRate.d);

        public override string SupportedFormats => supportedFormats;

        protected override unsafe IResourceProvider<VideoFrame>? DoGrabVideoFrame()
        {
            return sourceReader.GrabVideoFrame();
        }

        public override unsafe void Dispose()
        {
            controlSubscription.Dispose();
            sourceReader.Dispose();
            base.Dispose();
        }
    }
}
