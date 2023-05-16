using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using System;
using VL.Core;
using VL.Lib.Basics.Resources;
using Buffer = Stride.Graphics.Buffer;
using ServiceRegistry = VL.Core.ServiceRegistry;

namespace VL.Stride.Graphics
{
    static partial class GraphicsNodes
    {
        class BufferBuilder : IDisposable
        {
            private BufferDescription description;
            private BufferViewDescription viewDescription;
            private IGraphicsDataProvider initalData;
            private bool needsRebuild = true;
            private Buffer buffer;
            internal bool Recreate;
            private readonly IResourceHandle<Game> gameHandle;

            public Buffer Buffer
            {
                get
                {
                    if (needsRebuild || Recreate)
                    {
                        RebuildBuffer();
                        needsRebuild = false;
                    }
                    return buffer;
                }

                private set => buffer = value;
            }

            public BufferDescription Description
            {
                get => description;
                set
                {
                    description = value;
                    needsRebuild = true;
                }
            }

            public BufferViewDescription ViewDescription
            {
                get => viewDescription;
                set
                {
                    viewDescription = value;
                    needsRebuild = true;
                }
            }

            public IGraphicsDataProvider InitalData
            {
                get => initalData;
                set
                {
                    initalData = value;
                    needsRebuild = true;
                }
            }

            public BufferBuilder(NodeContext nodeContext)
            {
                gameHandle = AppHost.Current.Services.GetGameHandle();
            }

            public void Dispose()
            {
                buffer?.Dispose();
                buffer = null;
                gameHandle.Dispose();
            }

            private void RebuildBuffer()
            {
                var pin = PinnedGraphicsData.None;
                if (initalData != null)
                {
                    pin = initalData.Pin();
                }

                try
                {
                    buffer?.Dispose();
                    buffer = null;
                    var game = gameHandle.Resource;

                    if (viewDescription.Flags == BufferFlags.None)
                        viewDescription.Flags = description.BufferFlags;

                    buffer = BufferExtensions.New(game.GraphicsDevice, description, viewDescription, pin.Pointer);
                }
                catch
                {
                    buffer = null;
                }
                finally
                {
                    pin.Dispose();
                }
            }
        }

        class BufferViewBuilder : IDisposable
        {
            private Buffer buffer;
            private BufferViewDescription viewDescription;
            private bool needsRebuild = true;
            private Buffer bufferView;
            internal bool Recreate;
            private readonly IResourceHandle<Game> gameHandle;

            public Buffer Buffer
            {
                get
                {
                    if (needsRebuild || Recreate)
                    {
                        RebuildBufferView();
                        needsRebuild = false;
                    }
                    return bufferView;
                }

                private set => bufferView = value;
            }

            public Buffer Input
            {
                get => buffer;
                set
                {
                    buffer = value;
                    needsRebuild = true;
                }
            }

            public BufferViewDescription ViewDescription
            {
                get => viewDescription;
                set
                {
                    viewDescription = value;
                    needsRebuild = true;
                }
            }

            public BufferViewBuilder(NodeContext nodeContext)
            {
                gameHandle = AppHost.Current.Services.GetGameHandle();
            }

            public void Dispose()
            {
                bufferView?.Dispose();
                bufferView = null;
                gameHandle.Dispose();
            }

            private void RebuildBufferView()
            {
                try
                {
                    if (bufferView != null)
                    {
                        bufferView.Dispose();
                        bufferView = null; 
                    }

                    if (buffer != null 
                        && viewDescription.Flags != BufferFlags.None
                        && ((buffer.Flags & BufferFlags.RawBuffer) != 0))
                    {
                        bufferView ??= new Buffer();
                        var game = gameHandle.Resource;
                        bufferView = buffer.ToBufferView(bufferView, viewDescription, game.GraphicsDevice);
                    }
                    else
                    {
                        bufferView = null;
                    }
                }
                catch
                {
                    bufferView = null;
                }
            }
        }
    }
}
