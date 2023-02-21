using SharpDX.MediaFoundation;
using SharpDX.Multimedia;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core;

namespace VL.Video.MediaFoundation
{
    public static class VideoCaptureHelpers
    {
        public class Format
        {
            public Int2 size;
            public float fr;
            public string format;
            public float aspect;
            public MediaType mediaType;
        }

        public static string GetSupportedFormats(VideoCaptureDeviceEnumEntry deviceEntry)
        {
            try
            {
                return String.Join(Environment.NewLine, EnumerateSupportedFormats(deviceEntry)
                .OrderByDescending(x => x.format)
                .ThenByDescending(x => x.size.X)
                .ThenByDescending(x => x.size.Y)
                .ThenByDescending(x => x.fr)
                .Select(x => $"{x.format} {x.size.X}x{x.size.Y} - {x.aspect:F2} @ {x.fr:F2}")
                .Distinct()
                .ToArray());
            }
            catch (Exception)
            {
                return "Error: Your device does not allow listing of supported formats.";
            }
        }

        static IEnumerable<Format> EnumerateSupportedFormats(VideoCaptureDeviceEnumEntry deviceEntry)
        {
            var devices = EnumerateVideoDevices();
            if (devices.Length == 0)
                yield break;

            // Zero is the made up default entry - skip it
            var index = deviceEntry.Definition.Entries.IndexOf(x => x == deviceEntry.Value) - 1;
            if (index < 0)
                yield break;

            var device = devices[index];
            var name = device.Get(CaptureDeviceAttributeKeys.FriendlyName);
            var mediaSource = device.ActivateObject<MediaSource>();
            mediaSource.CreatePresentationDescriptor(out PresentationDescriptor descriptor);
            var streamDescriptor = descriptor.GetStreamDescriptorByIndex(0, out SharpDX.Mathematics.Interop.RawBool _);
            var handler = streamDescriptor.MediaTypeHandler;
            for (int i = 0; i < handler.MediaTypeCount; i++)
            {
                var mediaType = handler.GetMediaTypeByIndex(i);
                if (mediaType.MajorType == MediaTypeGuids.Video)
                    yield return ToFormat(mediaType);
            }
        }

        static Format ToFormat(MediaType mediaType)
        {
            var frameRate = ParseFrameRate(mediaType.Get(MediaTypeAttributeKeys.FrameRate));
            ParseSize(mediaType.Get(MediaTypeAttributeKeys.FrameSize), out int width, out int height);
            ParseSize(mediaType.Get(MediaTypeAttributeKeys.PixelAspectRatio), out int nominator, out int denominator);

            var format = mediaType.ToVideoFormatString();
            return new Format() { size = new Int2(width, height), fr = frameRate, format = format, aspect = nominator / denominator, mediaType = mediaType };
        }

        public static Activate[] EnumerateVideoDevices()
        {
            var attributes = new MediaAttributes();
            attributes.Set(CaptureDeviceAttributeKeys.SourceType, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);
            return MediaFactory.EnumDeviceSources(attributes);
        }

        public static void ParseSize(long value, out int width, out int height)
        {
            width = (int)(value >> 32);
            height = (int)(value & 0x00000000FFFFFFFF);
        }

        public static long MakeSize(int width, int height)
        {
            return height + ((long)(width) << 32);
        }

        public static float ParseFrameRate(long value)
        {
            var numerator = (int)(value >> 32);
            var denominator = (int)(value & 0x00000000FFFFFFFF);
            return (float)(numerator * 100 / denominator) / 100f;
        }
        public static long MakeFrameRate(float value)
        {
            var r = 1 / value;
            var numerator = 100000 * value;
            var denominator = (int)(r * numerator);
            return denominator + ((long)(numerator) << 32);
        }

        private static string ToVideoFormatString(this MediaType mediaType)
        {
            // https://docs.microsoft.com/en-us/windows/desktop/medfound/video-subtype-guids
            var subTypeId = mediaType.Get(MediaTypeAttributeKeys.Subtype);
            var fourccEncoded = BitConverter.ToInt32(subTypeId.ToByteArray(), 0);
            var fourcc = new FourCC(fourccEncoded);
            return fourcc.ToString();
        }

        // See https://docs.microsoft.com/en-us/windows/win32/medfound/how-to-set-the-video-capture-format
        internal static IEnumerable<Format> EnumerateCaptureFormats(this MediaSource mediaSource)
        {
            mediaSource.CreatePresentationDescriptor(out var pd);
            using (pd)
            {
                using var sd = pd.GetStreamDescriptorByIndex(0, out _);
                using var handler = sd.MediaTypeHandler;
                for (int i = 0; i < handler.MediaTypeCount; i++)
                {
                    var mediaType = handler.GetMediaTypeByIndex(i);
                    if (mediaType.MajorType == MediaTypeGuids.Video)
                        yield return ToFormat(mediaType);
                }
            }
        }

        // See https://docs.microsoft.com/en-us/windows/win32/medfound/how-to-set-the-video-capture-format
        internal static void SetCaptureFormat(this MediaSource mediaSource, MediaType mediaType)
        {
            mediaSource.CreatePresentationDescriptor(out var pd);
            using (pd)
            {
                using var sd = pd.GetStreamDescriptorByIndex(0, out var _);
                using var handler = sd.MediaTypeHandler;
                handler.CurrentMediaType = mediaType;
            }
        }

        internal static IDisposable Subscribe(this MediaSource mediaSource, IObservable<CameraControls> cameraControls)
        {
            return cameraControls.Subscribe(p => mediaSource.Subscribe(p.Name, p.Subject));
        }

        internal static IDisposable Subscribe(this MediaSource mediaSource, IObservable<VideoControls> videoControls)
        {
            return videoControls.Subscribe(p => mediaSource.Subscribe(p.Name, p.Subject));
        }

        static IDisposable Subscribe<T>(this IObservable<IControls<T>> videoControls, Func<Property<T>, IDisposable> subscribeToProperty)
        {
            var subscriptions = new SerialDisposable();
            return new CompositeDisposable(
                subscriptions,
                videoControls.Subscribe(c =>
                {
                    subscriptions.Disposable = new CompositeDisposable(
                        c.GetProperties().Select(subscribeToProperty));
                }));
        }

        static IDisposable Subscribe(this MediaSource mediaSource, CameraControlPropertyName propertyName, IObservable<Optional<float>> observableValue)
        {
            var flags = CameraControlFlags.None;

            // Fetch defaults
            int min = default, max = default, step = default, @default = default;
            {
                var cameraControl = Marshal.GetObjectForIUnknown(mediaSource.NativePointer) as IAMCameraControl;
                if (cameraControl != null)
                {
                    cameraControl.GetRange(propertyName, out min, out max, out step, out @default, out flags);

                    Marshal.ReleaseComObject(cameraControl);
                }
            }

            if (flags == CameraControlFlags.None)
                return Disposable.Empty;

            return observableValue.Subscribe(value =>
            {
                // Need to fetch the interface again because of thread affinity
                var cameraControl = Marshal.GetObjectForIUnknown(mediaSource.NativePointer) as IAMCameraControl;
                if (cameraControl != null)
                {
                    if (value.HasValue)
                        cameraControl.Set(propertyName, (int)VLMath.Map(value.Value, 0f, 1f, min, max, MapMode.Clamp), CameraControlFlags.Manual);
                    else
                        cameraControl.Set(propertyName, @default, CameraControlFlags.Auto);

                    Marshal.ReleaseComObject(cameraControl);
                }
            });
        }

        static IDisposable Subscribe(this MediaSource mediaSource, VideoProcAmpProperty propertyName, IObservable<Optional<float>> observableValue)
        {
            var flags = VideoProcAmpFlags.None;

            // Fetch defaults
            int min = default, max = default, step = default, @default = default;
            {
                var videoControl = Marshal.GetObjectForIUnknown(mediaSource.NativePointer) as IAMVideoProcAmp;
                if (videoControl != null)
                {
                    videoControl.GetRange(propertyName, out min, out max, out step, out @default, out flags);

                    Marshal.ReleaseComObject(videoControl);
                }
            }

            if (flags == VideoProcAmpFlags.None)
                return Disposable.Empty;

            return observableValue.Subscribe(value =>
            {
                // Need to fetch the interface again because of thread affinity
                var videoControl = Marshal.GetObjectForIUnknown(mediaSource.NativePointer) as IAMVideoProcAmp;
                if (videoControl != null)
                {
                    if (value.HasValue)
                        videoControl.Set(propertyName, (int)VLMath.Map(value.Value, 0f, 1f, min, max, MapMode.Clamp), VideoProcAmpFlags.Manual);
                    else
                        videoControl.Set(propertyName, @default, VideoProcAmpFlags.Auto);

                    Marshal.ReleaseComObject(videoControl);
                }
            });
        }
    }
}
