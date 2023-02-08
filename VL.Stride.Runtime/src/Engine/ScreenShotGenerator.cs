using System;
using VL.Stride.Graphics;
using Stride.Games;
using Stride.Graphics;

namespace VL.Stride.Engine
{
    public static class ScreenshotBuilder
    {
        /// <summary>
        /// Request a screenshot and save it to disc.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="screenShotUrl">The screenshot URL.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="pixelFormat">The pixel format.</param>
        /// <param name="depthBufferFormat">The depth buffer format.</param>
        /// <param name="fileType">Type of the file.</param>
        /// <returns>
        /// True on success
        /// </returns>
        public static bool SaveScreenshot(GameBase game, string screenshotUrl, int width, int height, PixelFormat pixelFormat = PixelFormat.R8G8B8A8_UNorm, PixelFormat depthBufferFormat = PixelFormat.D24_UNorm_S8_UInt, ImageFileType fileType = ImageFileType.Png)
        {
            var status = true;
            var graphicsContext = game.GraphicsContext;
            var graphicsDevice = game.GraphicsDevice;
            var commandList = graphicsContext.CommandList;
            var gameSystems = game.GameSystems;
            var drawTime = game.DrawTime;
            var previousPresenter = graphicsDevice.Presenter;

            try
            {
                // set the master output
                var renderTarget = graphicsContext.Allocator.GetTemporaryTexture2D(width, height, pixelFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
                var depthStencil = graphicsContext.Allocator.GetTemporaryTexture2D(width, height, depthBufferFormat, TextureFlags.DepthStencil);

                var tempPresenter = new RenderTargetGraphicsPresenter(graphicsDevice, renderTarget, depthStencil.ViewFormat);

                try
                {
                    // temp presenter
                    graphicsDevice.Presenter = tempPresenter;

                    // Always clear the state of the GraphicsDevice to make sure a scene doesn't start with a wrong setup 
                    commandList.ClearState();

                    // render the screenshot
                    graphicsContext.ResourceGroupAllocator.Reset(graphicsContext.CommandList);
                    gameSystems.Draw(drawTime);

                    // write the screenshot to the file
                    renderTarget.SaveTexture(commandList, screenshotUrl, fileType);

                }
                finally
                {
                    // Cleanup
                    graphicsDevice.Presenter = previousPresenter;
                    tempPresenter.Dispose();

                    graphicsContext.Allocator.ReleaseReference(depthStencil);
                    graphicsContext.Allocator.ReleaseReference(renderTarget);
                }
            }
            catch (Exception e)
            {
                status = false;
            }

            return status;
        }
    }
}
