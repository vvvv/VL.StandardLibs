using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Graphics
{
    static partial class GraphicsNodes
    {
        class TextureBuilder : IDisposable
        {
            private TextureDescription description;
            private TextureViewDescription viewDescription;
            private IGraphicsDataProvider[] initalData;
            private bool needsRebuild = true;
            private Texture texture;
            internal bool Recreate;
            private readonly IResourceHandle<Game> gameHandle;

            public Texture Texture
            {
                get
                {
                    if (needsRebuild || Recreate)
                    {
                        RebuildTexture();
                        needsRebuild = false;
                    }
                    return texture;
                }

                private set => texture = value;
            }

            public TextureDescription Description
            {
                get => description;
                set
                {
                    description = value;
                    needsRebuild = true;
                }
            }

            public TextureViewDescription ViewDescription
            {
                get => viewDescription;
                set
                {
                    viewDescription = value;
                    needsRebuild = true;
                }
            }

            public IGraphicsDataProvider[] InitalData
            {
                get => initalData;
                set
                {
                    initalData = value;
                    needsRebuild = true;
                }
            }

            public TextureBuilder(NodeContext nodeContext)
            {
                gameHandle = AppHost.Current.Services.GetGameHandle();
            }

            public void Dispose()
            {
                texture?.Dispose();
                texture = null;
                gameHandle.Dispose();
            }

            PinnedGraphicsData[] pinnedGraphicsDatas = new PinnedGraphicsData[0];
            DataBox[] boxes = new DataBox[0];

            private void RebuildTexture()
            {
                var dataCount = 0;
                if (initalData != null)
                {
                    dataCount = initalData.Length;

                    if (pinnedGraphicsDatas.Length != dataCount)
                    {
                        pinnedGraphicsDatas = new PinnedGraphicsData[dataCount];
                        boxes = new DataBox[dataCount];
                    }

                    if (dataCount > 0)
                    {
                        var pixelSize = description.Format.BlockSize();
                        var minRowSize = description.Width * pixelSize;
                        var minSliceSize = description.Depth * minRowSize;

                        for (int i = 0; i < dataCount; i++)
                        {
                            var id = initalData[i];
                            if (id is null)
                            {
                                pinnedGraphicsDatas[i] = PinnedGraphicsData.None;
                                boxes[i] = new DataBox(IntPtr.Zero, minRowSize, minSliceSize);
                            }
                            else
                            {
                                pinnedGraphicsDatas[i] = id.Pin();
                                var rowSize = Math.Max(id.RowSizeInBytes, minRowSize);
                                var sliceSize = Math.Max(id.SliceSizeInBytes, minSliceSize);
                                boxes[i] = new DataBox(pinnedGraphicsDatas[i].Pointer, rowSize, sliceSize);
                            }
                        } 
                    }
                }

                try
                {
                    texture?.Dispose();
                    texture = null;
                    var game = gameHandle.Resource;
                    texture = Texture.New(game.GraphicsDevice, description, viewDescription, boxes);
                }
                catch
                {
                    texture = null;
                }
                finally
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        pinnedGraphicsDatas[i].Dispose();
                    }
                }
            }
        }

        class TextureViewBuilder : IDisposable
        {
            private Texture texture;
            private TextureViewDescription viewDescription;
            private bool needsRebuild = true;
            private Texture textureView;
            internal bool Recreate;
            private readonly IResourceHandle<Game> gameHandle;

            public Texture TextureView
            {
                get
                {
                    if (needsRebuild || Recreate)
                    {
                        RebuildTextureView();
                        needsRebuild = false;
                    }
                    return textureView;
                }

                private set => textureView = value;
            }

            public Texture Input
            {
                get => texture;
                set
                {
                    texture = value;
                    needsRebuild = true;
                }
            }

            public TextureViewDescription ViewDescription
            {
                get => viewDescription;
                set
                {
                    viewDescription = value;
                    needsRebuild = true;
                }
            }

            public TextureViewBuilder(NodeContext nodeContext)
            {
                gameHandle = AppHost.Current.Services.GetGameHandle();
            }

            public void Dispose()
            {
                textureView?.Dispose();
                textureView = null;
                gameHandle.Dispose();
            }

            private void RebuildTextureView()
            {
                try
                {
                    if (textureView != null)
                    {
                        textureView.Dispose();
                        textureView = null; 
                    }

                    if (texture != null && (
                        viewDescription.Format == PixelFormat.None
                        || (texture.Format == viewDescription.Format)
                        || (texture.Format.IsTypeless() && (texture.Format.BlockSize() == viewDescription.Format.BlockSize()))
                        ))
                    {
                        var game = gameHandle.Resource;
                        textureView = texture.ToTextureView(viewDescription);
                    }
                }
                catch
                {
                    textureView = null;
                }
            }
        }
    }
}
