using Stride.Graphics;
using System;
using VL.Core;
using VL.Lib.Basics.Imaging;
using MapMode = Stride.Graphics.MapMode;
using StridePixelFormat = Stride.Graphics.PixelFormat;
using VLPixelFormat = VL.Lib.Basics.Imaging.PixelFormat;

namespace VL.Stride.Utils
{
    // Currently only used by TypeConverter sketch
    public static class ImageUtils
    {
        /// <summary>
        /// Copies the image into a new texture of same size and format.
        /// </summary>
        public static unsafe Texture ToTexture(IImage image, GraphicsDevice graphicsDevice, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            var info = image.Info;
            using var imageData = image.GetData();
            fixed (byte* data = imageData.Bytes.Span)
            {
                var format = info.Format.ToTexturePixelFormat(graphicsDevice.ColorSpace);
                var description = TextureDescription.New2D(info.Width, info.Height, format, textureFlags: textureFlags, usage: usage);
                return Texture.New(graphicsDevice, description, new DataBox(new IntPtr(data), info.ScanSize, info.ImageSize));
            }
        }

        /// <summary>
        /// Makes the texture available as an image. No data is copied, the resulting image accesses the texture memory. The texture must be a staging texture.
        /// </summary>
        public static IImage AsImage(Texture texture, CommandList commandList)
        {
            if (commandList is null)
                throw new ArgumentNullException(nameof(commandList));
            if (texture is null)
                throw new ArgumentNullException(nameof(texture));
            if (texture.Usage != GraphicsResourceUsage.Staging)
                throw new ArgumentException("Not a staging texture. Must have Usage = GraphicsResourceUsage.Staging", nameof(texture));

            var info = new ImageInfo(texture.Width, texture.Height, texture.Format.ToImagePixelFormat());
            return new TextureImage(commandList, texture, info);
        }

        public static StridePixelFormat ToTexturePixelFormat(this VLPixelFormat format, ColorSpace colorSpace)
        {
            var pixelFormat = ToTexturePixelFormat(format);
            return colorSpace == ColorSpace.Linear ? pixelFormat.ToSRgb() : pixelFormat;

            static StridePixelFormat ToTexturePixelFormat(VLPixelFormat format)
            {
                switch (format)
                {
                    case VLPixelFormat.Unknown: return StridePixelFormat.None;
                    case VLPixelFormat.R8: return StridePixelFormat.R8_UNorm;
                    case VLPixelFormat.R16: return StridePixelFormat.R16_UNorm;
                    case VLPixelFormat.R32F: return StridePixelFormat.R32_Float;
                    case VLPixelFormat.R8G8B8A8: return StridePixelFormat.R8G8B8A8_UNorm;
                    case VLPixelFormat.B8G8R8X8: return StridePixelFormat.B8G8R8X8_UNorm;
                    case VLPixelFormat.B8G8R8A8: return StridePixelFormat.B8G8R8A8_UNorm;
                    case VLPixelFormat.R16G16B16A16F: return StridePixelFormat.R16G16B16A16_Float;
                    case VLPixelFormat.R32G32F: return StridePixelFormat.R32G32_Float;
                    case VLPixelFormat.R32G32B32A32F: return StridePixelFormat.R32G32B32A32_Float;
                }
                throw new UnsupportedPixelFormatException(format);
            }
        }

        public static VLPixelFormat ToImagePixelFormat(this StridePixelFormat format)
        {
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
                case StridePixelFormat.R8G8B8A8_UNorm_SRgb:
                    return VLPixelFormat.R8G8B8A8;
                case StridePixelFormat.B8G8R8X8_UNorm:
                case StridePixelFormat.B8G8R8X8_UNorm_SRgb:
                    return VLPixelFormat.B8G8R8X8;
                case StridePixelFormat.B8G8R8A8_UNorm:
                case StridePixelFormat.B8G8R8A8_UNorm_SRgb:
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

        sealed class TextureImage : IImage
        {
            private readonly CommandList commandList;
            private readonly Texture texture;

            public TextureImage(CommandList commandList, Texture texture, ImageInfo info)
            {
                this.commandList = commandList;
                this.texture = texture;
                Info = info;
            }

            public ImageInfo Info { get; }

            public bool IsVolatile => true;

            public IImageData GetData() => new TextureData(commandList, texture);
        }

        sealed class TextureData : IImageData
        {
            private readonly CommandList commandList;
            private readonly MappedResource mappedResource;
            private readonly UnmanagedMemoryManager<byte> memoryManager;

            public TextureData(CommandList commandList, Texture texture)
            {
                this.commandList = commandList;
                mappedResource = commandList.MapSubresource(texture, 0, MapMode.Read);
                memoryManager = new UnmanagedMemoryManager<byte>(mappedResource.DataBox.DataPointer, mappedResource.DataBox.SlicePitch);
            }

            public ReadOnlyMemory<byte> Bytes => memoryManager.Memory;

            public int ScanSize => mappedResource.DataBox.RowPitch;

            public void Dispose()
            {
                commandList.UnmapSubresource(mappedResource);
            }
        }
    }
}
