#nullable enable
using CommunityToolkit.HighPerformance;
using Stride.Core.Mathematics;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Direct3D10;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

using static Windows.Win32.PInvoke;

namespace VL.Video.MF
{
    [SupportedOSPlatform("windows6.1")]
    internal sealed class SourceReader
    {
        public static unsafe SourceReader CreateFromUrl(string url, ID3D11Device* device, bool readAsync)
        {
            using var _ = MediaFoundation.Use();

            // Setup source reader arguments
            var sourceReaderAttributes = CreateSourceReaderAttributes(device, readAsync, out var sourceReaderCB);
            try
            {
                IMFSourceReader* sourceReader;
                MFCreateSourceReaderFromURL(url, sourceReaderAttributes, &sourceReader).ThrowOnFailure();
                return new SourceReader(sourceReader, sourceReaderCB);
            }
            finally
            {
                sourceReaderAttributes->Release();
            }
        }

        public static unsafe SourceReader CreateFromMediaSource(IMFMediaSource* mediaSource, ID3D11Device* device, bool readAsync)
        {
            using var _ = MediaFoundation.Use();

            // Setup source reader arguments
            var sourceReaderAttributes = CreateSourceReaderAttributes(device, readAsync, out var sourceReaderCB);
            try
            {
                IMFSourceReader* sourceReader;
                MFCreateSourceReaderFromMediaSource(mediaSource, sourceReaderAttributes, &sourceReader).ThrowOnFailure();
                return new SourceReader(sourceReader, sourceReaderCB);
            }
            finally
            {
                sourceReaderAttributes->Release();
            }
        }

        private static unsafe IMFAttributes* CreateSourceReaderAttributes(ID3D11Device* device, bool readAsync, out SourceReaderCB? sourceReaderCB)
        {
            // Setup source reader arguments
            IMFAttributes* sourceReaderAttributes;
            MFCreateAttributes(&sourceReaderAttributes, 1).ThrowOnFailure();

            // Enable low latency - we don't want frames to get buffered
            sourceReaderAttributes->SetUINT32(in MF_LOW_LATENCY, 1);
            // Needed in order to read data as Argb32
            sourceReaderAttributes->SetUINT32(in MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING, 1);

            if (device != null && OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                // Add multi thread protection on device (MF is multi-threaded)
                if (device->QueryInterface(in ID3D10Multithread.IID_Guid, out var x).Succeeded)
                    ((ID3D10Multithread*)x)->SetMultithreadProtected(true);

                // Reset device
                IMFDXGIDeviceManager* manager;
                MFCreateDXGIDeviceManager(out var resetToken, &manager).ThrowOnFailure();
                manager->ResetDevice((IUnknown*)device, resetToken);
                sourceReaderAttributes->SetUnknown(in MF_SOURCE_READER_D3D_MANAGER, (IUnknown*)manager);
                manager->Release();
            }

            if (readAsync)
            {
                sourceReaderCB = new SourceReaderCB();
                var sourceReaderCBCom = SourceReaderCB.ComWrappers.GetOrCreateComInterfaceForObject(sourceReaderCB, CreateComInterfaceFlags.None);
                sourceReaderAttributes->SetUnknown(in MF_SOURCE_READER_ASYNC_CALLBACK, (IUnknown*)sourceReaderCBCom);
            }
            else
                sourceReaderCB = null;

            return sourceReaderAttributes;
        }

        const uint firstVideoStream = unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_FIRST_VIDEO_STREAM);

        private readonly IDisposable mf;
        private readonly IntPtr reader;
        private readonly Size2 size;
        private readonly (int n, int d) frameRate;
        private readonly SourceReaderCB? sourceReaderCB;

        private unsafe SourceReader(IMFSourceReader* reader, SourceReaderCB? sourceReaderCB)
        {
            this.mf = MediaFoundation.Use();
            this.reader = new IntPtr(reader);
            this.sourceReaderCB = sourceReaderCB;

            // Set output format to BGRA8
            {
                IMFMediaType* mt;
                MFCreateMediaType(&mt).ThrowOnFailure();
                mt->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
                mt->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_ARGB32);
                reader->SetCurrentMediaType(firstVideoStream, null, mt);
                mt->Release();
            }

            // Retrieve frame rate & size
            {
                IMFMediaType* currentMediaType;
                reader->GetCurrentMediaType(firstVideoStream, &currentMediaType);
                currentMediaType->GetUINT64(MF_MT_FRAME_SIZE, out var frameSize);
                Utils.ParseSize(frameSize, out size.Width, out size.Height);

                currentMediaType->GetUINT64(MF_MT_FRAME_RATE, out var frameRateRatio);
                Utils.ParseFrameRate(frameRateRatio, out frameRate.n, out frameRate.d);
                currentMediaType->Release();
            }
        }

        public (int n, int d) FrameRate => frameRate;

        private unsafe void TriggerRead()
        {
            var reader = (IMFSourceReader*)this.reader;
            reader->ReadSample(firstVideoStream, 0, null, null, null, null);
        }

        public unsafe IResourceProvider<VideoFrame>? GrabVideoFrame()
        {
            IMFSample* sample;

            if (sourceReaderCB != null)
            {
                TriggerRead();

                if (!sourceReaderCB.Samples.TryTake(out var pSample, 1000))
                    return null;

                sample = (IMFSample*)pSample;
            }
            else
            {
                var reader = (IMFSourceReader*)this.reader;
                uint streamIndex, streamFlags;
                long timestamp;
                reader->ReadSample(firstVideoStream, 0, &streamIndex, &streamFlags, &timestamp, &sample);
            }

            if (sample is null)
                return null;

            sample->GetBufferCount(out var bufferCount);
            if (bufferCount == 0)
            {
                sample->Release();
                return null;
            }

            IMFMediaBuffer* buffer;
            sample->ConvertToContiguousBuffer(&buffer);
            if (buffer is null)
            {
                sample->Release();
                return null;
            }

            // Get time & duration
            TimeSpan duration;
            sample->GetSampleDuration((long*)&duration);
            TimeSpan time;
            sample->GetSampleTime((long*)&time);

            if (OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                // Texture?
                if (buffer->QueryInterface(in IMFDXGIBuffer.IID_Guid, out var pDxgiBuffer).Succeeded)
                {
                    var dxgiBuffer = (IMFDXGIBuffer*)pDxgiBuffer;
                    dxgiBuffer->GetResource(in ID3D11Texture2D.IID_Guid, out var pD3D11Texture);
                    if (pD3D11Texture != null)
                    {
                        var d3D11Texture = (ID3D11Texture2D*)pD3D11Texture;
                        d3D11Texture->GetDesc(out var desc);
                        var videoTexture = new VideoTexture(new IntPtr(pD3D11Texture), size.Width, size.Height, Lib.Basics.Imaging.PixelFormat.B8G8R8A8);
                        var frame = new GpuVideoFrame<BgraPixel>(videoTexture, Timecode: time, FrameRate: frameRate);
                        return ResourceProvider.Return(frame, (texture: new IntPtr(pD3D11Texture), dxgiBuffer: new IntPtr(pDxgiBuffer), buffer: new IntPtr(buffer), sample: new IntPtr(sample), videoTexture),
                            disposeAction: static x =>
                            {
                                ((IUnknown*)x.texture)->Release();
                                ((IUnknown*)x.dxgiBuffer)->Release();
                                ((IUnknown*)x.buffer)->Release();
                                ((IUnknown*)x.sample)->Release();
                                x.videoTexture.Dispose();
                            });
                    }

                    dxgiBuffer->Release();
                }

                // Buffer2d?
                IMF2DBuffer2* buffer2;
                if (buffer->QueryInterface(in IMF2DBuffer2.IID_Guid, out var x).Succeeded)
                {
                    buffer2 = (IMF2DBuffer2*)x;
                    buffer2->IsContiguousFormat(out var pfIsContiguous);
                    //if (pfIsContiguous)
                    {
                        buffer2->Lock2DSize(MF2DBuffer_LockFlags.MF2DBuffer_LockFlags_Read, out var scanline, out var pitch, out var bufferStart, out var bufferLength);

                        var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(new IntPtr(bufferStart), (int)bufferLength);
                        var offset = scanline - bufferStart;
                        var p = pitch - size.Width * sizeof(BgraPixel);
                        var memory = memoryOwner.Memory.AsMemory2D((int)offset, size.Height, size.Width, p);
                        var frame = new VideoFrame<BgraPixel>(memory, Timecode: time, FrameRate: frameRate);
                        return ResourceProvider.Return(frame, (new IntPtr(sample), new IntPtr(buffer), new IntPtr(buffer2), (IDisposable)memoryOwner),
                            disposeAction: static x =>
                            {
                                Debug.Assert(OperatingSystem.IsWindowsVersionAtLeast(8));

                                var (s, b, b2, memoryOwner) = x;
                                memoryOwner.Dispose();
                                var buffer2 = (IMF2DBuffer2*)b2;
                                buffer2->Unlock2D();
                                buffer2->Release();

                                var mediaBuffer = (IMFMediaBuffer*)b.ToPointer();
                                mediaBuffer->Release();
                                var sample = (IMFSample*)b.ToPointer();
                                sample->Release();
                            });
                    }
                }
            }

            {
                byte* ptr;
                uint maxLength, currentLength;
                buffer->Lock(&ptr, &maxLength, &currentLength);

                var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(new IntPtr(ptr), (int)currentLength);
                var memory = memoryOwner.Memory.AsMemory2D(size.Height, size.Width);
                var frame = new VideoFrame<BgraPixel>(memory, Timecode: time, FrameRate: frameRate);
                return ResourceProvider.Return(frame, (new IntPtr(sample), new IntPtr(buffer), (IDisposable)memoryOwner),
                    disposeAction: static x =>
                    {
                        var (s, b, memoryOwner) = x;
                        memoryOwner.Dispose();
                        var mediaBuffer = (IMFMediaBuffer*)b.ToPointer();
                        mediaBuffer->Unlock();
                        mediaBuffer->Release();
                        var sample = (IMFSample*)b.ToPointer();
                        sample->Release();
                    });
            }
        }

        public unsafe void Dispose()
        {
            if (sourceReaderCB != null)
                sourceReaderCB.Samples.CompleteAdding();

            var reader = (IMFSourceReader*)this.reader;
            reader->Release();

            mf.Dispose();
        }

        sealed unsafe class SourceReaderCB : IMFSourceReaderCallback.Interface
        {
            public static readonly ComWrappers ComWrappers = new SourceReaderCBComWrappers();

            private readonly BlockingCollection<IntPtr> samples = new(boundedCapacity: 1);

            public BlockingCollection<IntPtr> Samples => samples;

            public HRESULT OnReadSample(HRESULT hrStatus, uint dwStreamIndex, uint dwStreamFlags, long llTimestamp, [Optional] IMFSample* sample)
            {
                if (sample != null && samples.TryAddSafe(new IntPtr(sample)))
                    sample->AddRef();

                return default;
            }

            public HRESULT OnFlush(uint dwStreamIndex)
            {
                return default;
            }

            public HRESULT OnEvent(uint dwStreamIndex, IMFMediaEvent* pEvent)
            {
                return default;
            }

            sealed class SourceReaderCBComWrappers : ComWrappers
            {
                protected override ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
                {
                    // https://github.com/microsoft/CsWin32/issues/751#issuecomment-1304268295
                    var vtable = (IMFSourceReaderCallback.Vtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(IMFSourceReaderCallback), sizeof(IMFSourceReaderCallback.Vtbl)).ToPointer();

                    IUnknown.Vtbl* unknown = (IUnknown.Vtbl*)vtable;

                    GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);
                    unknown->QueryInterface_1 = (delegate* unmanaged[Stdcall]<IUnknown*, Guid*, void**, HRESULT>)fpQueryInterface;
                    unknown->AddRef_2 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpAddRef;
                    unknown->Release_3 = (delegate* unmanaged[Stdcall]<IUnknown*, uint>)fpRelease;

                    IMFSourceReaderCallback.PopulateVTable(vtable);

                    var comInterfaceEntry = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(SourceReaderCBComWrappers), sizeof(ComInterfaceEntry)).ToPointer();
                    comInterfaceEntry->IID = IMFSourceReaderCallback.IID_Guid;
                    comInterfaceEntry->Vtable = new IntPtr(vtable);
                    count = 1;
                    return comInterfaceEntry;
                }

                protected override object CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
                {
                    throw new NotImplementedException();
                }

                protected override void ReleaseObjects(IEnumerable objects)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
