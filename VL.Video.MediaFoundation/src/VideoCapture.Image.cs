using SharpDX;
using SharpDX.MediaFoundation;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;
using ColorBGRA = Stride.Core.Mathematics.ColorBGRA;

namespace VL.Video.MediaFoundation
{
    partial class VideoCapture
    {
        // For 2020.2
        /*
        class MFImage : Image<ColorBGRA>
        {
            readonly MediaBuffer buffer;
            readonly ImageInfo info;

            public MFImage(ImageInfo info, MediaBuffer buffer)
            {
                this.info = info;
                this.buffer = buffer;
            }

            public override ImageInfo Info => info;

            public override bool IsVolatile => true;

            protected override IMemoryOwner<ColorBGRA> GetPixelsCore()
            {
                return new MediaBufferMemory(buffer);
            }

            unsafe class MediaBufferMemory : MemoryManager<ColorBGRA>
            {
                readonly MediaBuffer buffer;
                readonly IntPtr ptr;
                readonly int length;

                public MediaBufferMemory(MediaBuffer buffer)
                {
                    this.buffer = buffer;
                    this.ptr = buffer.Lock(out var maxLength, out length);
                }

                public override Span<ColorBGRA> GetSpan()
                {
                    var bytes = new Span<byte>(ptr.ToPointer(), length);
                    return MemoryMarshal.Cast<byte, ColorBGRA>(bytes);
                }

                public override MemoryHandle Pin(int elementIndex = 0)
                {
                    return new MemoryHandle(ptr.ToPointer(), pinnable: this);
                }

                public override void Unpin()
                {
                }

                protected override void Dispose(bool disposing)
                {
                    buffer.Unlock();
                }
            }
        }
        */
    }
}
