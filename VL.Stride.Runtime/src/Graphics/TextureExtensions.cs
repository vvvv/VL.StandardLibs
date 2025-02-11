using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using VL.Lib.Basics.Imaging;
using VL.Lib.Collections;
using Stride.Graphics;
using StridePixelFormat = Stride.Graphics.PixelFormat;
using VLPixelFormat = VL.Lib.Basics.Imaging.PixelFormat;
using Stride.Core;
using System.Threading.Tasks;
using VL.Stride.Engine;
using VL.Core;
using Stride.Rendering;
using System.Buffers;
using System.Runtime.InteropServices;

namespace VL.Stride.Graphics
{
    public static class TextureExtensions
    {

        public static bool TryToTypeless(this StridePixelFormat format, out StridePixelFormat typelessFormat)
        {
            typelessFormat = format;

            var formatString = Enum.GetName(typeof(StridePixelFormat), format);
            var idx = formatString.IndexOf('_');

            if (idx > 0)
            {
                formatString = formatString.Remove(idx);
                formatString += "_Typeless";

                if (Enum.TryParse<StridePixelFormat>(formatString, out var newFormat))
                {
                    typelessFormat = newFormat;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the <paramref name="fromData"/> to the given <paramref name="texture"/> on GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="texture"></param>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice"></param>
        /// <param name="mipSlice"></param>
        /// <param name="region"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        /// <returns>The GPU buffer.</returns>
        public static unsafe Texture SetData<TData>(this Texture texture, CommandList commandList, Spread<TData> fromData, int arraySlice, int mipSlice, ResourceRegion? region)
            where TData : unmanaged
        {
            var immutableArray = fromData._array;
            var array = Unsafe.As<ImmutableArray<TData>, TData[]>(ref immutableArray);
            texture.SetData(commandList, array, arraySlice, mipSlice, region);
            return texture;
        }

        public static unsafe Texture SetDataFromIImage(this Texture texture, CommandList commandList, IImage image, int arraySlice, int mipSlice, ResourceRegion? region)
        {
            using (var data = image.GetData())
            {
                // Why is Stride not taking a ReadOnlySpan here? :(
                // https://github.com/dotnet/runtime/issues/23494
                var readonlySpan = data.Bytes.Span;
                var readwriteSpan = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(readonlySpan), readonlySpan.Length);
                texture.SetData(commandList, readwriteSpan, arraySlice, mipSlice, region);
            }

            return texture;
        }

        public static unsafe Texture SetDataFromProvider(this Texture texture, CommandList commandList, IGraphicsDataProvider data, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null)
        {
            if (texture != null && data != null)
            {
                using (var handle = data.Pin())
                {
                    var span = new Span<byte>((void*)handle.Pointer, data.SizeInBytes);
                    texture.SetData(commandList, span, arraySlice, mipSlice, region);
                }
            }

            return texture;
        }

        /// <summary>
        /// Similiar to <see cref="Texture.Load(GraphicsDevice, Stream, TextureFlags, GraphicsResourceUsage, bool)"/> but allocates memory on unmanaged heap only.
        /// </summary>
        public static unsafe Texture Load(GraphicsDevice device, string file, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable, bool loadAsSRGB = false)
        {
            const int bufferSize = 1024 * 1024 * 8;
            using var src = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            var ptr = Utilities.AllocateMemory((int)src.Length);
            using var dst = new UnmanagedMemoryStream((byte*)ptr, 0, (int)src.Length, FileAccess.ReadWrite);
            src.CopyTo(dst, bufferSize);
            using var image = Image.Load(new IntPtr(ptr), (int)dst.Length, makeACopy: false, loadAsSRGB: loadAsSRGB);
            return Texture.New(device, image, textureFlags, usage);
        }

        public static void SaveTexture(this Texture texture, CommandList commandList, string filename, TextureWriterFileType imageFileType = TextureWriterFileType.Png)
        {
            using (var resultFileStream = File.OpenWrite(filename))
            {
                texture.Save(commandList, resultFileStream, (ImageFileType)imageFileType);
            }
        }

        public static void SaveStagingTexture(this Texture stagingTexture, CommandList commandList, string filename, TextureWriterFileType imageFileType = TextureWriterFileType.Png)
        {
            if (stagingTexture is null)
                throw new ArgumentNullException(nameof(stagingTexture));

            if (!stagingTexture.Usage.HasFlag(GraphicsResourceUsage.Staging))
                throw new ArgumentException("The texture is not a staging texture", nameof(stagingTexture));

            using (var resultFileStream = File.OpenWrite(filename))
            {
                stagingTexture.Save(commandList, resultFileStream, stagingTexture, (ImageFileType)imageFileType);
            }
        }

        /// <summary>
        /// Copies the texture to an equivalent staging texture. The resulting task completes once the copy operation is done.
        /// </summary>
        public static async Task<Texture> CopyToStagingAsync(this Texture texture)
        {
            using var game = AppHost.Current.Services.GetGameHandle();
            var schedulerSystem = game.Resource.Services.GetService<SchedulerSystem>();
            var stagingTexture = texture.ToStaging();
            await texture.CopyToStagingAsync(stagingTexture, schedulerSystem);
            return stagingTexture;
        }

        /// <summary>
        /// Retrieves the texture data asynchronously.
        /// </summary>
        public static async Task<IMemoryOwner<T>> GetDataAsync<T>(this Texture texture) where T : unmanaged
        {
            using var game = AppHost.Current.Services.GetGameHandle();
            var commandList = game.Resource.GraphicsContext.CommandList;
            using var staging = await texture.CopyToStagingAsync();
            var pixelDataCount = texture.CalculatePixelDataCount<T>();
            var memoryOwner = MemoryPool<T>.Shared.Rent(pixelDataCount);
            var memory = memoryOwner.Memory.Slice(0, pixelDataCount);
            staging.GetData(commandList, staging, memory.Span);
            return new SlicedOwner<T>(memoryOwner,memory);
        }

        private sealed class SlicedOwner<T>(IMemoryOwner<T> upstream, Memory<T> memory) : IMemoryOwner<T>
        {
            public Memory<T> Memory => memory;

            public void Dispose()
            {
                upstream.Dispose();
            }
        }

        public static StridePixelFormat GetStridePixelFormat(ImageInfo info, bool isSRgb = true)
        {
            var format = info.Format;
            switch (format)
            {
                case VLPixelFormat.Unknown:
                    return StridePixelFormat.None;
                case VLPixelFormat.R8:
                    return StridePixelFormat.R8_UNorm;
                case VLPixelFormat.R16:
                    return StridePixelFormat.R16_UNorm;
                case VLPixelFormat.R32F:
                    return StridePixelFormat.R32_Float;
                case VLPixelFormat.R8G8B8X8:
                    return isSRgb ? StridePixelFormat.R8G8B8A8_UNorm_SRgb : StridePixelFormat.R8G8B8A8_UNorm;
                case VLPixelFormat.R8G8B8A8:
                    return isSRgb ? StridePixelFormat.R8G8B8A8_UNorm_SRgb : StridePixelFormat.R8G8B8A8_UNorm;
                case VLPixelFormat.B8G8R8X8:
                    return isSRgb ? StridePixelFormat.B8G8R8X8_UNorm_SRgb : StridePixelFormat.B8G8R8X8_UNorm;
                case VLPixelFormat.B8G8R8A8:
                    return isSRgb ? StridePixelFormat.B8G8R8A8_UNorm_SRgb : StridePixelFormat.B8G8R8A8_UNorm;
                case VLPixelFormat.R32G32F:
                    return StridePixelFormat.R32G32_Float;
                case VLPixelFormat.R16G16B16A16F:
                    return StridePixelFormat.R16G16B16A16_Float;
                case VLPixelFormat.R32G32B32A32F:
                    return StridePixelFormat.R32G32B32A32_Float;
                default:
                    throw new UnsupportedPixelFormatException(format);
            }
        }

        public static VLPixelFormat GetVLImagePixelFormat(Texture texture, out bool isSRgb)
        {
            isSRgb = false;

            if (texture == null)
                return VLPixelFormat.Unknown;

                var format = texture.Format;
            switch (format)
            {
                case StridePixelFormat.None:
                    return VLPixelFormat.Unknown;
                case StridePixelFormat.R8_UNorm:
                    return VLPixelFormat.R8;
                case StridePixelFormat.R16_UNorm:
                    return VLPixelFormat.R16;
                case StridePixelFormat.R32_Float:
                    return VLPixelFormat.R32F;
                case StridePixelFormat.R8G8B8A8_UNorm:
                    return VLPixelFormat.R8G8B8A8;
                case StridePixelFormat.R8G8B8A8_UNorm_SRgb:
                    isSRgb = true;
                    return VLPixelFormat.R8G8B8A8;
                case StridePixelFormat.B8G8R8X8_UNorm:
                    return VLPixelFormat.B8G8R8X8;
                case StridePixelFormat.B8G8R8X8_UNorm_SRgb:
                    isSRgb = true;
                    return VLPixelFormat.B8G8R8X8;
                case StridePixelFormat.B8G8R8A8_UNorm:
                    return VLPixelFormat.B8G8R8A8;
                case StridePixelFormat.B8G8R8A8_UNorm_SRgb:
                    isSRgb = true;
                    return VLPixelFormat.B8G8R8A8;
                case StridePixelFormat.R32G32_Float:
                    return VLPixelFormat.R32G32F;
                case StridePixelFormat.R16G16B16A16_Float:
                    return VLPixelFormat.R16G16B16A16F;
                case StridePixelFormat.R32G32B32A32_Float:
                    return VLPixelFormat.R32G32B32A32F;
                default:
                    throw new Exception("Unsupported Pixel Format");
            }
        }

    }

    public enum EncodedStrideImageFormat
    {
        Jpeg = 3,
        Png = 4,
        Webp = 6,
    }

    public enum TextureWriterFileType
    {
        Stride = 0,
        Dds = 1,
        Png = 2,
        Gif = 3,
        Jpg = 4,
        Bmp = 5,
        Tiff = 6,
    }
}
