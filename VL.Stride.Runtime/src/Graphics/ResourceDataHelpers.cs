using Stride.Core;
using Stride.Graphics;
using System;
using System.Buffers;
using System.Runtime.InteropServices;
using VL.Lib.Basics.Imaging;
using VL.Lib.Collections;

namespace VL.Stride.Graphics
{
    public interface IGraphicsDataProvider
    {
        int SizeInBytes { get; }
        int ElementSizeInBytes { get; }
        int RowSizeInBytes { get; }
        int SliceSizeInBytes { get; }
        PinnedGraphicsData Pin();
    }

    public interface IMemoryPinner
    {
        public IntPtr Pin();
        public void Unpin();
    }

    public class DataPointerPinner : IMemoryPinner
    {
        public DataPointer DataPointer;

        public unsafe IntPtr Pin()
        {
            return DataPointer.Pointer;
        }

        public void Unpin()
        {
        }
    }

    public class ReadonlyMemoryPinner<T> : IMemoryPinner where T : struct
    {
        public ReadOnlyMemory<T> Memory;
        MemoryHandle memoryHandle;

        public unsafe IntPtr Pin()
        {
            memoryHandle = Memory.Pin();
            return new IntPtr(memoryHandle.Pointer);
        }

        public void Unpin()
        {
            memoryHandle.Dispose();
        }
    }

    public class ImagePinner : IMemoryPinner
    {
        public IImage Image;

        IImageData imageData;
        MemoryHandle memoryHandle;

        public unsafe IntPtr Pin()
        {
            imageData = Image.GetData();
            memoryHandle = imageData.Bytes.Pin();
            return new IntPtr(memoryHandle.Pointer);
        }

        public void Unpin()
        {
            memoryHandle.Dispose();
            imageData.Dispose();
        }
    }

    public class NonePinner : IMemoryPinner
    {
        public IntPtr Pin()
        {
            return IntPtr.Zero;
        }

        public void Unpin()
        {
        }
    }

    public struct PinnedGraphicsData : IDisposable
    {
        public static readonly PinnedGraphicsData None = new PinnedGraphicsData(new NonePinner());

        public readonly IntPtr Pointer;
        readonly IMemoryPinner pinner;

        public PinnedGraphicsData(IMemoryPinner pinner)
        {
            Pointer = pinner.Pin();
            this.pinner = pinner;
        }

        public void Dispose()
        {
            pinner.Unpin();
        }
    }

    public class MemoryDataProvider : IGraphicsDataProvider
    {
        public IMemoryPinner Pinner = new NonePinner();

        public void SetDataPointer(DataPointer dataPointer, int offsetInBytes = 0, int sizeInBytes = 0, int elementSizeInBytes = 0, int rowSizeInBytes = 0, int sliceSizeInBytes = 0)
        {
            var pnr = Pinner as DataPointerPinner;
            pnr ??= new DataPointerPinner();

            pnr.DataPointer = dataPointer;
            Pinner = pnr;

            OffsetInBytes = offsetInBytes;
            SizeInBytes = sizeInBytes > 0 ? Math.Min(sizeInBytes, dataPointer.Size) : dataPointer.Size;
            ElementSizeInBytes = elementSizeInBytes;
            RowSizeInBytes = rowSizeInBytes;
            SliceSizeInBytes = sliceSizeInBytes;
        }

        public void SetMemoryData<T>(ReadOnlyMemory<T> memory, int offsetInBytes = 0, int sizeInBytes = 0, int elementSizeInBytes = 0, int rowSizeInBytes = 0, int sliceSizeInBytes = 0) where T : struct
        {
            var pnr = Pinner as ReadonlyMemoryPinner<T>;
            pnr ??= new ReadonlyMemoryPinner<T>();

            pnr.Memory = memory;
            Pinner = pnr;

            OffsetInBytes = offsetInBytes;
            var tSize = Utilities.SizeOf<T>();
            SizeInBytes = sizeInBytes > 0 ? sizeInBytes : memory.Length * tSize;
            ElementSizeInBytes = elementSizeInBytes > 0 ? elementSizeInBytes : tSize;
            RowSizeInBytes = rowSizeInBytes;
            SliceSizeInBytes = sliceSizeInBytes;
        }

        public void SetImageData(IImage image, int offsetInBytes = 0, int sizeInBytes = 0, int elementSizeInBytes = 0, int rowSizeInBytes = 0, int sliceSizeInBytes = 0)
        {
            var pnr = Pinner as ImagePinner;
            pnr ??= new ImagePinner();

            pnr.Image = image;
            Pinner = pnr;

            OffsetInBytes = offsetInBytes;

            SizeInBytes = sizeInBytes > 0 ? sizeInBytes : image.Info.ImageSize;
            ElementSizeInBytes = elementSizeInBytes > 0 ? elementSizeInBytes : image.Info.Format.GetPixelSize();
            RowSizeInBytes = rowSizeInBytes > 0 ? rowSizeInBytes : image.Info.ScanSize;
            SliceSizeInBytes = sliceSizeInBytes > 0 ? sliceSizeInBytes : RowSizeInBytes * image.Info.Height;
        }

        public int OffsetInBytes { get; set; }

        public int SizeInBytes { get; set; }

        public int ElementSizeInBytes { get; set; }

        public int RowSizeInBytes { get; set; }

        public int SliceSizeInBytes { get; set; }

        public PinnedGraphicsData Pin()
        {
            return new PinnedGraphicsData(Pinner);
        }
    }

    public struct VLImagePinner : IDisposable
    {
        IImageData imageData;
        MemoryHandle imageDataHandle;
        IntPtr pointer;

        public unsafe VLImagePinner(IImage image)
        {
            if (image != null)
            {
                imageData = image.GetData();
                imageDataHandle = imageData.Bytes.Pin();
                pointer = (IntPtr)imageDataHandle.Pointer; 
            }
            else
            {
                imageData = null;
                imageDataHandle = new MemoryHandle();
                pointer = IntPtr.Zero;
            }
        }

        public IntPtr Pointer
        {
            get => pointer;
        }

        public int SizeInBytes
        {
            get => imageData.Bytes.Length;
        }

        public int ScanSize
        {
            get => imageData.ScanSize;
        }

        public void Dispose()
        {
            imageDataHandle.Dispose();
            imageData?.Dispose();
        }
    }

    public struct GCPinner : IDisposable
    {
        GCHandle pinnedObject;

        public GCPinner(object obj)
        {
            if (obj != null)
                pinnedObject = GCHandle.Alloc(obj, GCHandleType.Pinned);
            else
                pinnedObject = new GCHandle();
        }

        public IntPtr Pointer
        {
            get => pinnedObject.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            pinnedObject.Free();
        }
    }

    public static class ResourceDataHelpers
    {
        public static void PinSpread<T>(Spread<T> input, out IntPtr pointer, out int sizeInBytes, out int byteStride, out GCPinner pinner) where T : struct
        {
            pointer = IntPtr.Zero;
            sizeInBytes = 0;
            byteStride = 0;

            var count = input.Count;
            if (count > 0)
            {
                byteStride = Utilities.SizeOf<T>();
                sizeInBytes = byteStride * count;

                pinner = new GCPinner(input);
                pointer = pinner.Pointer;
                return;
            }

            pinner = new GCPinner(null);
        }

        public static void PinArray<T>(T[] input, out IntPtr pointer, out int sizeInBytes, out int byteStride, out GCPinner pinner) where T : struct
        {
            pointer = IntPtr.Zero;
            sizeInBytes = 0;
            byteStride = 0;

            var count = input.Length;
            if (count > 0)
            {
                input.AsMemory();
                byteStride = Utilities.SizeOf<T>();
                sizeInBytes = byteStride * count;

                pinner = new GCPinner(input);
                pointer = pinner.Pointer;
                return;
            }

            pinner = new GCPinner(null);
        }

        public static void PinImage(IImage input, out IntPtr pointer, out int sizeInBytes, out int bytePerRow, out int bytesPerPixel, out VLImagePinner pinner)
        {
            pointer = IntPtr.Zero;
            sizeInBytes = 0;
            bytePerRow = 0;
            bytesPerPixel = 0;

            if (input != null)
            {
                pinner = new VLImagePinner(input);
                sizeInBytes = pinner.SizeInBytes;
                bytePerRow = pinner.ScanSize;
                bytesPerPixel = input.Info.PixelSize;
                pointer = pinner.Pointer;
                return;
            }

            pinner = new VLImagePinner(null);
        }
    }
}
