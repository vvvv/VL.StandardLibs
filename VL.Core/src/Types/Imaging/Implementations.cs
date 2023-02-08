using System;
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace VL.Lib.Basics.Imaging
{
    /// <summary>
    /// Image implementation using an array as backing store.
    /// </summary>
    /// <typeparam name="T">The elment type of one channel.</typeparam>
    public class ArrayImage<T> : IImage
        where T : struct
    {
        public static readonly ArrayImage<T> Default = new ArrayImage<T>(Array.Empty<T>(), default, false);

        class Data : MemoryManager<byte>, IImageData
        {
            readonly Memory<T> FMemory;

            public Data(T[] array, int scanSize)
            {
                FMemory = array;
                ScanSize = scanSize;
            }

            public int ScanSize { get; }

            public ReadOnlyMemory<byte> Bytes => Memory;

            public override Span<byte> GetSpan()
            {
                return MemoryMarshal.AsBytes(FMemory.Span);
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                return FMemory.Pin();
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        readonly T[] FData;
        readonly int FScanSize;

        public ArrayImage(T[] data, ImageInfo info, bool isVolatile)
            : this(data, info, isVolatile, info.ScanSize)
        {

        }

        public ArrayImage(T[] data, ImageInfo info, bool isVolatile, int scanSize)
        {
            FData = data;
            Info = info;
            IsVolatile = isVolatile;
            FScanSize = scanSize;
        }

        public ImageInfo Info { get; }

        public bool IsVolatile { get; }

        public IImageData GetData() => new Data(FData, FScanSize);
    }

    /// <summary>
    /// Image implementation using a <see cref="Bitmap"/> as backing store. Must be disposed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class BitmapImage : IImage, IDisposable
    {
        unsafe class Data : MemoryManager<byte>, IImageData
        {
            readonly Bitmap FBitmap;
            readonly System.Drawing.Imaging.BitmapData FBitmapData;

            public Data(Bitmap bitmap, bool canWrite)
            {
                FBitmap = bitmap;
                var flags = canWrite ? System.Drawing.Imaging.ImageLockMode.ReadWrite : System.Drawing.Imaging.ImageLockMode.ReadOnly;
                FBitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), flags, bitmap.PixelFormat);
            }

            public int ScanSize => FBitmapData.Stride;

            public ReadOnlyMemory<byte> Bytes => Memory;

            public override Span<byte> GetSpan()
            {
                var ptr = FBitmapData.Scan0.ToPointer();
                return new Span<byte>(ptr, FBitmapData.Height * FBitmapData.Stride);
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                // Already pinned
                return new MemoryHandle(FBitmapData.Scan0.ToPointer(), pinnable: this);
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
                FBitmap.UnlockBits(FBitmapData);
            }
        }

        readonly Bitmap FBitmap;
        readonly bool FIsOwner;
        readonly bool FCanWrite;
        bool FIsDisposed;

        internal BitmapImage(Bitmap bitmap, bool isOwner, bool isVolatile, bool canWrite)
        {
            FBitmap = bitmap;
            FIsOwner = isOwner;
            IsVolatile = isVolatile;
            FCanWrite = canWrite;
        }

         ~BitmapImage()
        {
            Dispose();
        }

        public ImageInfo Info => !FIsDisposed
            ? new ImageInfo(FBitmap.Width, FBitmap.Height, FBitmap.PixelFormat.ToPixelFormat(), FBitmap.PixelFormat.ToString())
            : ImageExtensions.Default.Info;

        public IImageData GetData() => !FIsDisposed ? new Data(FBitmap, FCanWrite) : ImageExtensions.Default.GetData();

        public bool IsVolatile { get; }

        public void Dispose()
        {
            if (!FIsDisposed)
            {
                FIsDisposed = true;
                GC.SuppressFinalize(this);
                if (FIsOwner)
                    FBitmap.Dispose();
            }
        }
    }

    /// <summary>
    /// Image implementation using unmanaged memory as backing store. Must be disposed.
    /// </summary>
    public class IntPtrImage : IImage, IDisposable
    {
        unsafe class Data : MemoryManager<byte>, IImageData
        {
            readonly IntPtr FPointer;
            readonly int FLength;

            public Data(IntPtr pointer, int length, int scanSize)
            {
                FPointer = pointer;
                FLength = length;
                ScanSize = scanSize;
            }

            public int ScanSize { get; }

            public ReadOnlyMemory<byte> Bytes => Memory;

            public override Span<byte> GetSpan()
            {
                return new Span<byte>(FPointer.ToPointer(), FLength);
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                // Already pinned
                return new MemoryHandle(FPointer.ToPointer(), pinnable: this);
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        readonly IntPtr FPointer;
        readonly int FSize;
        readonly ImageInfo FInfo;
        bool FDisposed;

        public IntPtrImage(IntPtr pointer, int size, ImageInfo info)
        {
            FPointer = pointer;
            FSize = size;
            FInfo = info;
        }

        public ImageInfo Info => FDisposed ? ImageExtensions.Default.Info : FInfo;

        public bool IsVolatile => true;

        public IImageData GetData() => FDisposed ? ImageExtensions.Default.GetData() : new Data(FPointer, FSize, Info.ScanSize);

        public void Dispose()
        {
            FDisposed = true;
        }
    }

    class ProxyImage : IImage
    {
        readonly IImage original;

        public ProxyImage(IImage original)
        {
            this.original = original;
        }

        public ImageInfo Info => original.Info;

        public bool IsVolatile => original.IsVolatile;

        public IImageData GetData()
        {
            return original.GetData();
        }
    }

    class MemoryBasedImage<T> : IImage
        where T : struct
    {
        class Data : MemoryManager<byte>, IImageData
        {
            readonly ReadOnlyMemory<T> FMemory;

            public Data(ReadOnlyMemory<T> data, int scanSize)
            {
                FMemory = data;
                ScanSize = scanSize;
            }

            public int ScanSize { get; }

            public ReadOnlyMemory<byte> Bytes => Memory;

            public override Span<byte> GetSpan()
            {
                var memory = MemoryMarshal.AsMemory(FMemory);
                return MemoryMarshal.AsBytes(memory.Span);
            }

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                return FMemory.Pin();
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        readonly ReadOnlyMemory<T> FData;
        readonly ImageInfo FInfo;
        readonly bool FIsVolatile;
        readonly int FScanSize;

        public MemoryBasedImage(ReadOnlyMemory<T> data, ImageInfo info, bool isVolatile, int? scanSize = null)
        {
            FData = data;
            FInfo = info;
            FIsVolatile = isVolatile;
            FScanSize = scanSize ?? info.ScanSize;
        }

        public ImageInfo Info => FInfo;

        public IImageData GetData() => new Data(FData, FScanSize);

        public bool IsVolatile => FIsVolatile;
    }

    class ImageStream : Stream
    {
        readonly IImageData FData;

        public unsafe ImageStream(IImageData data)
        {
            FData = data;
            Length = data.Bytes.Length;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var dst = buffer.AsSpan(offset, count);
            var remaining = (int)(Length - Position);
            var readCount = Math.Min(remaining, count);
            var src = FData.Bytes.Span.Slice((int)(Position += readCount), count);
            src.CopyTo(dst);
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
                default:
                    break;
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            FData.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class FinalizableHandle
    {
        readonly IImageData FData;
        readonly MemoryHandle FHandle;

        public FinalizableHandle(IImageData imageData, MemoryHandle handle)
        {
            FData = imageData;
            FHandle = handle;
        }

        ~FinalizableHandle()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            FHandle.Dispose();
            FData.Dispose();
        }
    }
}
