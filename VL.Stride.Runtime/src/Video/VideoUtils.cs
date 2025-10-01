#nullable enable
using SharpDX.Direct3D11;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using VL.Stride.Utils;

namespace VL.Stride.Video
{
    // Used by VideoSourceToTexture patch
    public static class VideoUtils
    {
        public static IResourceProvider<Texture> ToTexture(this IResourceProvider<VideoFrame> videoFrameProvider, RenderContext renderContext)
        {
            var device = renderContext.GraphicsDevice;
            var handle = videoFrameProvider.GetHandle();
            var videoFrame = handle.Resource;
            if (videoFrame.TryGetTexture(out var videoTexture))
            {
                // CreateTextureFromNative is quite expensive (creates bunch of auxiliary objects) -> use resource pool
                var textureProvider = ResourceProvider.NewPooledPerApp(videoTexture, videoTexture =>
                {
                    var nativeTexture = new Texture2D(videoTexture.NativePointer);
                    return SharpDXInterop.CreateTextureFromNative(device, nativeTexture, takeOwnership: true /* Stride bug, read: increase ref count? yes */, isSRgb: false);
                }, delayDisposalInMilliseconds: 1000 /* Keep the wrappers in memory for a bit - due to upstream pooling this might help */);

                // Color space check
                var textureHandle = textureProvider.GetHandle();
                var texture = textureHandle.Resource;
                if (device.ColorSpace == ColorSpace.Gamma || texture.Format.IsSRgb() || !texture.Format.HasSRgbEquivalent())
                    return ResourceProvider.Return(texture, new CompositeDisposable(textureHandle, handle), x => x.Dispose());

                // We need to do a color space conversion. Can be done by copying the texture to a SRGB based format.
                // To copy a texture we need to use the command list which we must access on the render thread.
                // Let's use the Defer operator for that - it defers the access on the Resource property of the handle.
                // That allows the caller of this method to fetch and store a handle (for lifetime mangement) without
                // triggering the copy command.
                return ResourceProvider.Return(handle, h => h.Dispose())
                    .BindNew(_ => textureHandle)
                    .Bind(textureHandle =>
                    {
                        return ResourceProvider.Defer(() =>
                        {
                            var desc = texture.Description;
                            desc.Format = desc.Format.ToSRgb();
                            desc.Flags = TextureFlags.ShaderResource;
                            desc.Options = TextureOptions.None;
                            var allocator = renderContext.Allocator;
                            var srgbTexture = allocator.GetTemporaryTexture(desc);
                            var renderDrawContext = renderContext.GetThreadContext();
                            renderDrawContext.CommandList.Copy(textureHandle.Resource, srgbTexture);
                            var state = (allocator, srgbTexture);
                            return ResourceProvider.Return(srgbTexture, state, static s =>
                            {
                                s.allocator.ReleaseReference(s.srgbTexture);
                            });
                        });
                    });
            }
            else
            {
                var texture = CopyFromMemory(videoFrame, device);
                // Since we made a copy, we can release the video frame (might put it back into a pool)
                handle.Dispose();
                return ResourceProvider.Return(
                    singleItem: texture!,
                    disposeAction: texture => texture.Dispose());
            }
        }

        private static unsafe Texture? CopyFromMemory(VideoFrame videoFrame, GraphicsDevice device)
        {
            if (!videoFrame.TryGetMemory(out var memory))
                return null;

            fixed (byte* data = memory.Span)
            {
                var description = TextureDescription.New2D(
                    width: videoFrame.Width,
                    height: videoFrame.Height,
                    format: videoFrame.PixelFormat.ToTexturePixelFormat(device.ColorSpace),
                    usage: GraphicsResourceUsage.Immutable);

                return Texture.New(
                        device,
                        description,
                        new DataBox(new IntPtr(data), videoFrame.RowLengthInBytes, videoFrame.LengthInBytes));
            }
        }

        private static Texture AsTexture(VideoTexture videoTexture, GraphicsDevice graphicsDevice)
        {
            var nativeTexture = new Texture2D(videoTexture.NativePointer);
            return SharpDXInterop.CreateTextureFromNative(graphicsDevice, nativeTexture, takeOwnership: true, isSRgb: false);
        }
    }
}
