using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VL.Core;
using VL.Video.CaptureControl;
using VL.Video.MF;
using Windows.Win32.Foundation;
using Windows.Win32.Media.DirectShow;
using Windows.Win32.Media.MediaFoundation;

using static Windows.Win32.PInvoke;

namespace VL.Video.MF
{
    [SupportedOSPlatform("windows6.1")]
    internal static unsafe class Utils
    {
        internal class Format
        {
            public Int2 size;
            public float fr;
            public string format;
            public float aspect;
            public IMFMediaType* mediaType;
        }

        public static unsafe string GetString(IMFActivate* activate, in Guid guid)
        {
            activate->GetStringLength(in guid, out var length);

            Span<char> buffer = stackalloc char[(int)length + 1];
            fixed (char* c = buffer)
            {
                activate->GetString(in guid, new PWSTR(c), (uint)buffer.Length, &length);
            }

            return buffer.Slice(0, (int)length).ToString();
        }

        public static unsafe T* ActivateObject<T>(IMFActivate* activate)
            where T : unmanaged
        {
            var id = typeof(T).GUID;
            return (T*)activate->ActivateObject(&id);
        }

        public static string GetSupportedFormats(IMFMediaSource* mediaSource)
        {
            try
            {
                using var formatEnum = EnumerateCaptureFormats(mediaSource);
                return string.Join(Environment.NewLine, formatEnum.Values
                    .OrderByDescending(x => x.format)
                    .ThenByDescending(x => x.size.X)
                    .ThenByDescending(x => x.size.Y)
                    .ThenByDescending(x => x.fr)
                    .Select(x => $"{x.format} {x.size.X}x{x.size.Y} - {x.aspect:F2} @ {x.fr:F2}")
                    .Distinct());
            }
            catch (Exception)
            {
                return "Error: Your device does not allow listing of supported formats.";
            }
        }

        static Format ToFormat(IMFMediaType* mediaType)
        {
            mediaType->GetUINT64(MF_MT_FRAME_RATE, out var fr);
            var frameRate = ParseFrameRate(fr);
            mediaType->GetUINT64(MF_MT_FRAME_SIZE, out var fs);
            ParseSize(fs, out int width, out int height);
            mediaType->GetUINT64(MF_MT_PIXEL_ASPECT_RATIO, out var ratio);
            ParseSize(ratio, out int nominator, out int denominator);

            var format = ToVideoFormatString(mediaType);
            return new Format() { size = new Int2(width, height), fr = frameRate, format = format, aspect = nominator / denominator, mediaType = mediaType };
        }

        internal static DeviceEnumeration EnumerateVideoDevices()
        {
            IMFAttributes* attributes;
            MFCreateAttributes(&attributes, 1).ThrowOnFailure();
            attributes->SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);

            MFEnumDeviceSources(attributes, out var devicePtr, out var count).ThrowOnFailure();

            var devices = new IMFActivate*[count];
            for (int i = 0; i < devices.Length; i++)
                devices[i] = devicePtr[i];

            Marshal.FreeCoTaskMem(new IntPtr(devicePtr));

            return new(devices);
        }

        internal readonly struct DeviceEnumeration : IDisposable
        {
            public DeviceEnumeration(IMFActivate*[] devices)
            {
                Devices = devices;
            }

            public IMFActivate*[] Devices { get; }

            public int Count => Devices.Length;

            public IMFActivate* this[int index] => Devices[index];

            public void Dispose()
            {
                foreach (var device in Devices)
                    device->Release();
            }
        }

        internal readonly struct FormatEnumeration : IDisposable
        {
            public FormatEnumeration(IReadOnlyList<Format> values)
            {
                Values = values;
            }

            public IReadOnlyList<Format> Values { get; }

            public int Count => Values.Count;

            public Format this[int index] => Values[index];

            public void Dispose()
            {
                foreach (var format in Values)
                    format.mediaType->Release();
            }
        }

        public static void ParseSize(ulong value, out int width, out int height)
        {
            width = (int)(value >> 32);
            height = (int)(value & 0x00000000FFFFFFFF);
        }

        public static long MakeSize(int width, int height)
        {
            return height + ((long)width << 32);
        }

        public static float ParseFrameRate(ulong value)
        {
            ParseFrameRate(value, out var numerator, out var denominator);
            return ToFrameRate(numerator, denominator);
        }

        public static float ToFrameRate(int numerator, int denominator)
        {
            return numerator * 100 / denominator / 100f;
        }

        public static void ParseFrameRate(ulong value, out int numerator, out int denominator)
        {
            numerator = (int)(value >> 32);
            denominator = (int)(value & 0x00000000FFFFFFFF);
        }

        public static long MakeFrameRate(float value)
        {
            var r = 1 / value;
            var numerator = 100000 * value;
            var denominator = (int)(r * numerator);
            return denominator + ((long)numerator << 32);
        }

        public static void GetFrameRate(IMFMediaType* pType, out int numerator, out int denominator)
        {
            pType->GetUINT64(MF_MT_FRAME_RATE, out var frameRate);
            ParseFrameRate(frameRate, out numerator, out denominator);
        }

        private static string ToVideoFormatString(IMFMediaType* mediaType)
        {
            // https://docs.microsoft.com/en-us/windows/desktop/medfound/video-subtype-guids
            mediaType->GetGUID(MF_MT_SUBTYPE, out var subTypeId);
            var fourccEncoded = BitConverter.ToUInt32(subTypeId.ToByteArray(), 0);
            return FourCCToString(fourccEncoded);
        }

        private static string FourCCToString(uint value)
        {
            return string.Format("{0}", new string(new[]
            {
                (char) (value & 0xFF),
                (char) (value >> 8 & 0xFF),
                (char) (value >> 16 & 0xFF),
                (char) (value >> 24 & 0xFF),
            }));
        }

        // See https://docs.microsoft.com/en-us/windows/win32/medfound/how-to-set-the-video-capture-format
        internal static FormatEnumeration EnumerateCaptureFormats(IMFMediaSource* mediaSource)
        {
            var result = new List<Format>();

            IMFPresentationDescriptor* descriptor = null;
            IMFStreamDescriptor* streamDescriptor = null;
            IMFMediaTypeHandler* handler = null;

            try
            {
                mediaSource->CreatePresentationDescriptor(&descriptor);
                descriptor->GetStreamDescriptorByIndex(0, out _, &streamDescriptor);
                streamDescriptor->GetMediaTypeHandler(&handler);
                handler->GetMediaTypeCount(out var mediaTypeCount);
                for (uint i = 0; i < mediaTypeCount; i++)
                {
                    IMFMediaType* mediaType;
                    handler->GetMediaTypeByIndex(i, &mediaType);
                    mediaType->GetMajorType(out var majorType);
                    if (majorType == MFMediaType_Video)
                        result.Add(ToFormat(mediaType));
                }
            }
            finally
            {
                handler->Release();
                streamDescriptor->Release();
                descriptor->Release();
            }

            return new(result);
        }

        // See https://docs.microsoft.com/en-us/windows/win32/medfound/how-to-set-the-video-capture-format
        internal static void SetCaptureFormat(IMFMediaSource* mediaSource, IMFMediaType* mediaType)
        {
            IMFPresentationDescriptor* pd;
            mediaSource->CreatePresentationDescriptor(&pd);
            IMFStreamDescriptor* sd;
            pd->GetStreamDescriptorByIndex(0, out _, &sd);
            IMFMediaTypeHandler* handler;
            sd->GetMediaTypeHandler(&handler);
            handler->SetCurrentMediaType(mediaType);

            handler->Release();
            sd->Release();
            pd->Release();
        }

        internal static IDisposable Subscribe(IMFMediaSource* mediaSource, IObservable<CameraControls> cameraControls)
        {
            return cameraControls.Subscribe(p => Subscribe(mediaSource, p.Name, p.Subject));
        }

        internal static IDisposable Subscribe(IMFMediaSource* mediaSource, IObservable<VideoControls> videoControls)
        {
            return videoControls.Subscribe(p => Subscribe(mediaSource, p.Name, p.Subject));
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

        static IDisposable Subscribe(IMFMediaSource* mediaSource, CameraControlProperty propertyName, IObservable<Optional<float>> observableValue)
        {
            int flags = default;

            // Fetch defaults
            int min = default, max = default, step = default, @default = default;
            {
                mediaSource->QueryInterface(in IAMCameraControl.IID_Guid, out var x);
                var cameraControl = (IAMCameraControl*)x;
                if (cameraControl != null)
                {
                    cameraControl->GetRange((int)propertyName, out min, out max, out step, out @default, out flags);

                    cameraControl->Release();
                }
            }

            if (flags == default)
                return Disposable.Empty;

            return observableValue.Subscribe(value =>
            {
                // Need to fetch the interface again because of thread affinity
                mediaSource->QueryInterface(in IAMCameraControl.IID_Guid, out var x);
                var cameraControl = (IAMCameraControl*)x;
                if (cameraControl != null)
                {
                    if (value.HasValue)
                        cameraControl->Set((int)propertyName, (int)VLMath.Map(value.Value, 0f, 1f, min, max, MapMode.Clamp), (int)CameraControlFlags.CameraControl_Flags_Manual);
                    else
                        cameraControl->Set((int)propertyName, @default, (int)CameraControlFlags.CameraControl_Flags_Auto);

                    cameraControl->Release();
                }
            });
        }

        static IDisposable Subscribe(IMFMediaSource* mediaSource, VideoProcAmpProperty propertyName, IObservable<Optional<float>> observableValue)
        {
            int flags = default;

            // Fetch defaults
            int min = default, max = default, step = default, @default = default;
            {
                mediaSource->QueryInterface(in IAMVideoProcAmp.IID_Guid, out var x);
                var videoControl = (IAMVideoProcAmp*)x;
                if (videoControl != null)
                {
                    videoControl->GetRange((int)propertyName, out min, out max, out step, out @default, out flags);
                    videoControl->Release();
                }
            }

            if (flags == default)
                return Disposable.Empty;

            return observableValue.Subscribe(value =>
            {
                // Need to fetch the interface again because of thread affinity
                mediaSource->QueryInterface(in IAMVideoProcAmp.IID_Guid, out var x);
                var videoControl = (IAMVideoProcAmp*)x;
                if (videoControl != null)
                {
                    if (value.HasValue)
                        videoControl->Set((int)propertyName, (int)VLMath.Map(value.Value, 0f, 1f, min, max, MapMode.Clamp), (int)VideoProcAmpFlags.VideoProcAmp_Flags_Manual);
                    else
                        videoControl->Set((int)propertyName, @default, (int)VideoProcAmpFlags.VideoProcAmp_Flags_Auto);

                    videoControl->Release();
                }
            });
        }
    }
}
