using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.MediaFoundation;
using Stride.Core.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using VL.Lib.Basics.Resources;

namespace VL.Video.MediaFoundation
{
    // Good source: https://stackoverflow.com/questions/40913196/how-to-properly-use-a-hardware-accelerated-media-foundation-source-reader-to-dec
    public sealed partial class VideoCapture : IDisposable
    {
        private static readonly Guid s_IID_ID3D11Texture2D = new Guid("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        private readonly SerialDisposable deviceSubscription = new SerialDisposable();
        private readonly Producing<VideoFrame> output = new Producing<VideoFrame>();
        private readonly DeviceProvider deviceProvider;
        private BlockingCollection<VideoFrame> videoFrames;
        private string deviceSymbolicLink;
        private Int2 preferredSize;
        private float preferredFps;
        private bool enabled = true;
        private int discardedFrames;
        private float actualFps;

        public VideoCapture(DeviceProvider deviceProvider)
        {
            this.deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
        }

        public VideoCaptureDeviceEnumEntry Device
        {
            set
            {
                var s = value?.Tag as string;
                if (s != deviceSymbolicLink)
                {
                    deviceSymbolicLink = s;
                    deviceSubscription.Disposable = null;
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
                    deviceSubscription.Disposable = null;
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
                    deviceSubscription.Disposable = null;
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
                if (value != Enabled)
                {
                    enabled = value;
                    deviceSubscription.Disposable = null;
                }
            }
        }

        public VideoFrame CurrentVideoFrame
        {
            get => output.Resource;
        }

        public int DiscardedFrames => discardedFrames;
        public float ActualFPS => actualFps;

        public VideoFrame Update(int waitTimeInMilliseconds)
        {
            if (enabled)
            {
                if (deviceSubscription.Disposable is null)
                {
                    deviceSubscription.Disposable = StartNewCapture();
                }

                FetchCurrentVideoFrame(waitTimeInMilliseconds);
            }
            return CurrentVideoFrame;

            MediaSource CreateMediaSource()
            {
                using var mediaSourceAttributes = new MediaAttributes();
                mediaSourceAttributes.Set(CaptureDeviceAttributeKeys.SourceType, CaptureDeviceAttributeKeys.SourceTypeVideoCapture.Guid);

                if (!string.IsNullOrEmpty(deviceSymbolicLink))
                {
                    // Use symbolic link (https://docs.microsoft.com/en-us/windows/win32/medfound/audio-video-capture-in-media-foundation)
                    mediaSourceAttributes.Set(CaptureDeviceAttributeKeys.SourceTypeVidcapSymbolicLink, deviceSymbolicLink);
                    MediaFactory.CreateDeviceSource(mediaSourceAttributes, out var mediaSource);
                    return mediaSource;
                }
                else
                {
                    // Auto select
                    using var activate = MediaFactory.EnumDeviceSources(mediaSourceAttributes).FirstOrDefault();
                    return activate?.ActivateObject<MediaSource>();
                }
            }

            IDisposable StartNewCapture()
            {
                var videoFrames = new BlockingCollection<VideoFrame>(boundedCapacity: 1);

                var pollTask = Task.Run(() =>
                {
                    // Create the media source based on the selected symbolic link
                    using var mediaSource = CreateMediaSource();
                    if (mediaSource is null)
                        return;

                    // Setup source reader arguments
                    using var sourceReaderAttributes = new MediaAttributes();
                    // Enable low latency - we don't want frames to get buffered
                    sourceReaderAttributes.Set(SinkWriterAttributeKeys.LowLatency, true);
                    // Needed in order to read data as Argb32
                    sourceReaderAttributes.Set(SourceReaderAttributeKeys.EnableAdvancedVideoProcessing, true);

                    // Connect camera and video controls
                    using var controlSubscription = new CompositeDisposable(
                        mediaSource.Subscribe(cameraControls), 
                        mediaSource.Subscribe(videoControls));

                    // Find best capture format for device
                    var bestCaptureFormat = mediaSource.EnumerateCaptureFormats()
                        .FirstOrDefault(f => f.size == preferredSize && f.fr == preferredFps);
                    if (bestCaptureFormat != null)
                        mediaSource.SetCaptureFormat(bestCaptureFormat.mediaType);

                    // Create the source reader
                    using var reader = new SourceReader(mediaSource, sourceReaderAttributes);

                    // Set output format to BGRA
                    using var mt = new MediaType();
                    mt.Set(MediaTypeAttributeKeys.MajorType, MediaTypeGuids.Video);
                    mt.Set(MediaTypeAttributeKeys.Subtype, VideoFormatGuids.Argb32);
                    reader.SetCurrentMediaType(SourceReaderIndex.FirstVideoStream, mt);

                    // Read the actual FPS
                    using var currentMediaType = reader.GetCurrentMediaType(SourceReaderIndex.FirstVideoStream);
                    actualFps = VideoCaptureHelpers.ParseFrameRate(currentMediaType.Get(MediaTypeAttributeKeys.FrameRate));
                    VideoCaptureHelpers.ParseSize(currentMediaType.Get(MediaTypeAttributeKeys.FrameSize), out var width, out var height);

                    // Reset the discared frame count
                    discardedFrames = 0;

                    while (!videoFrames.IsAddingCompleted)
                    {
                        var sample = reader.ReadSample(SourceReaderIndex.FirstVideoStream, SourceReaderControlFlags.None, out var streamIndex, out var streamFlags, out var timestamp);
                        if (sample is null)
                            continue;

                        if (sample.BufferCount == 0)
                        {
                            sample.Dispose();
                            continue;
                        }

                        var buffer = sample.ConvertToContiguousBuffer();
                        var ptr = buffer.Lock(out var maxLength, out var currentLength);
                        try
                        {
                            var texture = new Texture2D(deviceProvider.Device, new Texture2DDescription()
                            {
                                Width = width,
                                Height = height,
                                ArraySize = 1,
                                // Weired: RenderTarget is needed for ToRasterImage to work
                                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                                CpuAccessFlags = CpuAccessFlags.None,
                                Format = deviceProvider.UsesLinearColorspace ? Format.B8G8R8A8_UNorm_SRgb : Format.B8G8R8A8_UNorm,
                                MipLevels = 1,
                                OptionFlags = ResourceOptionFlags.None,
                                SampleDescription = new SampleDescription(1, 0),
                                // Due to RenderTarget flag we can't use Immutable
                                Usage = ResourceUsage.Default
                            }, new SharpDX.DataRectangle(ptr, width * 4));

                            using var frame = new VideoFrame(texture, texture);
                            if (videoFrames.TryAdd(frame))
                                frame.AddRef();
                        }
                        finally
                        {
                            buffer.Unlock();
                            buffer.Dispose();
                            sample.Dispose();
                        }
                    }
                });

                // Make the queue available
                this.videoFrames = videoFrames;

                return Disposable.Create(() =>
                {
                    videoFrames.CompleteAdding();
                    try
                    {
                        // There're cameras (like NDI) which get stuck in the 2. ReadSample call
                        if (!pollTask.Wait(500))
                            pollTask = null;

                        foreach (var f in videoFrames)
                            f.Dispose();
                    }
                    catch (Exception e)
                    {
                        // Just log them
                        Trace.TraceError(e.ToString());
                    }
                    finally
                    {
                        pollTask?.Dispose();
                        pollTask = default;
                        videoFrames.Dispose();
                    }
                });
            }
        }

        void FetchCurrentVideoFrame(int waitTimeInMilliseconds)
        {
            // Fetch the texture
            if (videoFrames != null && videoFrames.TryTake(out var frame, waitTimeInMilliseconds))
            {
                output.Resource = frame;
            }
        }

        public void Dispose()
        {
            deviceSubscription.Dispose();
            output.Dispose();
            deviceProvider.Dispose();
        }
    }
}
