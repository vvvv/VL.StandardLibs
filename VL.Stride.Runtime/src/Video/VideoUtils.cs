#nullable enable
using SharpDX.Direct3D11;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using System;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Stride.Utils;

namespace VL.Stride.Video
{
    // Used by VideoSourceToTexture patch
    public static class VideoUtils
    {
        public static IResourceProvider<Texture> ToTexture(this IResourceProvider<VideoFrame> videoFrameProvider, RenderDrawContext renderDrawContext)
        {
            var handle = videoFrameProvider.GetHandle();
            var videoFrame = handle.Resource;
            if (videoFrame.TryGetTexture(out var videoTexture))
            {
                // Tie the lifetime of the Stride texture to the texture object (which might be pooled)
                // But hold on to the video frame to prevent the texture object from being used for another frame
                var device = renderDrawContext.GraphicsDevice;
                var texture = videoTexture.Get<Texture>() ?? videoTexture.Attach(AsTexture(videoTexture, device));
                if (device.ColorSpace == ColorSpace.Gamma || texture.Format.IsSRgb() || !texture.Format.HasSRgbEquivalent())
                    return ResourceProvider.Return(texture, handle, handle => handle.Dispose());

                var srgbTexture = videoTexture.Get<SRgbTexture>() ?? videoTexture.Attach(SRgbTexture.Create(texture, device));
                renderDrawContext.CommandList.Copy(texture, srgbTexture.Texture);
                return ResourceProvider.Return(srgbTexture.Texture, handle, handle => handle.Dispose());
            }
            else
            {
                var texture = CopyFromMemory(videoFrame, renderDrawContext);
                // Since we made a copy, we can release the video frame (might put it back into a pool)
                handle.Dispose();
                return ResourceProvider.Return(
                    singleItem: texture!,
                    disposeAction: texture => texture.Dispose());
            }
        }

        private static unsafe Texture? CopyFromMemory(VideoFrame videoFrame, RenderDrawContext renderDrawContext)
        {
            if (!videoFrame.TryGetMemory(out var memory))
                return null;

            fixed (byte* data = memory.Span)
            {
                var description = TextureDescription.New2D(
                    width: videoFrame.Width,
                    height: videoFrame.Height,
                    format: ImageUtils.ToPixelFormat(videoFrame.PixelFormat),
                    usage: GraphicsResourceUsage.Immutable);

                return Texture.New(
                        renderDrawContext.GraphicsDevice,
                        description,
                        new DataBox(new IntPtr(data), videoFrame.RowLengthInBytes, videoFrame.LengthInBytes));
            }
        }

        private static Texture AsTexture(VideoTexture videoTexture, GraphicsDevice graphicsDevice)
        {
            var nativeTexture = new Texture2D(videoTexture.NativePointer);
            return SharpDXInterop.CreateTextureFromNative(graphicsDevice, nativeTexture, takeOwnership: true, isSRgb: false);
        }

        sealed class SRgbTexture : IDisposable
        {
            // Create texture with sRGB format
            public static SRgbTexture Create(Texture texture, GraphicsDevice graphicsDevice)
            {
                var desc = texture.Description;
                desc.Format = desc.Format.ToSRgb();
                desc.Flags = TextureFlags.ShaderResource;
                return new SRgbTexture(Texture.New(graphicsDevice, desc));
            }

            public readonly Texture Texture;

            private SRgbTexture(Texture texture)
            {
                Texture = texture;
            }

            public void Dispose()
            {
                Texture?.Dispose();
            }
        }
    }
}
