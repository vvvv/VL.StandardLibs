using ImGuiNET;
using SkiaSharp;
using Stride.Core.Mathematics;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using VL.Lib.Basics.Video;
using VL.Lib.Collections;
using VL.Lib.IO.Notifications;
using VL.Skia;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    using RectangleF = Stride.Core.Mathematics.RectangleF;
    using Vector2 = System.Numerics.Vector2;

    public class ToSkiaLayer : IDisposable, ILayer
    {
        readonly struct Handle<T> : IDisposable
            where T : class
        {
            private readonly GCHandle _handle;

            public Handle(T skiaObject)
            {
                _handle = GCHandle.Alloc(skiaObject);
            }

            public T? Target => _handle.Target as T;

            public IntPtr Ptr => GCHandle.ToIntPtr(_handle);

            public void Dispose()
            {
                _handle.Free();
            }
        }

        readonly ImGuiIOPtr _io;

        // OpenGLES rendering (https://github.com/dotnet/Silk.NET/tree/v2.15.0/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui)
        private readonly SkiaContext _context;
        private readonly Handle<SKPaint> _fontPaint;

        private bool _disposed;
        float _uiScaling;
        Spread<FontConfig?> _fonts = Spread<FontConfig?>.Empty;

        CallerInfo? _lastCallerInfo;
        ImDrawDataPtr _drawDataPtr;
        bool _readyToBeDrawn;
        WidgetLabel widgetLabel = new();

        public unsafe ToSkiaLayer()
        {
            _context = new SkiaContext();
            using (_context.MakeCurrent())
            {
                _io = ImGui.GetIO();
                _io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;
                _io.NativePtr->IniFilename = null;
                _io.ConfigDpiScaleFonts = true;
                _io.ConfigDpiScaleViewports = true;

                _fontPaint = new Handle<SKPaint>(new SKPaint());
                _io.Fonts.BuildImFontAtlas(_context, _fonts);

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                updateScaling(uiScaling: scaling);
            }
        }

        public ILayer Update(Widget widget, bool dockingEnabled, Spread<FontConfig?> fonts, bool fullscreenWindow, IStyle? Style)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_lastCallerInfo is null)
                return this;

            using (_context.MakeCurrent())
            {
                if (!_fonts.SequenceEqual(fonts))
                {
                    _fonts = fonts;
                    _io.Fonts.BuildImFontAtlas(_context, fonts);
                }

                var bounds = _lastCallerInfo.ViewportBounds;

                _io.DisplaySize = new Vector2(bounds.Width, bounds.Height);

                var viewPort = ImGui.GetMainViewport();
                viewPort.DpiScale = _lastCallerInfo.Scaling;
                //var style = ImGui.GetStyle();
                //style.FontScaleDpi = _lastCallerInfo.Scaling;

                // Enable Docking
                if (dockingEnabled)
                    _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                else
                    _io.ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;

                ImGui.PushFont(_context.DefaultFont, 0f);

                using var _ = _context.ApplyStyle(Style);

                _context.NewFrame();
                try
                {
                    if (fullscreenWindow)
                    {
                        if (dockingEnabled)
                        {
                            ImGui.DockSpaceOverViewport(0, viewPort, ImGuiDockNodeFlags.PassthruCentralNode);
                        }
                        else
                        {
                            ImGui.SetNextWindowPos(viewPort.WorkPos);
                            ImGui.SetNextWindowSize(viewPort.WorkSize);

                            ImGui.Begin(widgetLabel.Update(null),
                                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                                ImGuiWindowFlags.NoBackground);
                        }
                    }

                    _context.SetDrawList(DrawList.Foreground);
                    _context.Update(widget);
                }
                finally
                {
                    if (fullscreenWindow && !dockingEnabled)
                    {
                        ImGui.End();
                    }

                    ImGui.PopFont();

                    // Render (builds mesh with texture coordinates)
                    ImGui.Render();
                }

                // Render the mesh
                _drawDataPtr = ImGui.GetDrawData();
                _readyToBeDrawn = true;
            }

            return this;
        }

        void updateScaling(float uiScaling)
        {
            if (uiScaling != _uiScaling) 
            {
                _uiScaling = uiScaling;
                RenderHelper.UpdateUIScaling(uiScaling);
            }
        }

        public unsafe void Render(CallerInfo caller)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (caller.IsTooltip)
            {
                if (_readyToBeDrawn && _lastCallerInfo != null)
                    Render(caller, _drawDataPtr);
                return;
            }

            _lastCallerInfo = caller;
            if (_readyToBeDrawn)
                Render(caller, _drawDataPtr);
        }

        // From https://github.com/google/skia/blob/main/tools/viewer/ImGuiLayer.cpp
        private unsafe void Render(CallerInfo caller, ImDrawDataPtr drawData)
        {
            var canvas = caller.Canvas;

            for (int i = 0; i < drawData.Textures.Size; i++)
            {
                var texture = drawData.Textures[i];
                if (texture.Status != ImTextureStatus.OK)
                    UpdateTexture(texture);
            }

            //using var _ = new SKAutoCanvasRestore(canvas, true);
            canvas.Save();
            try
            {
                var us = PushTransformation(caller, SKMatrix.CreateScale(ImGuiConversion.FromImGuiScaling, ImGuiConversion.FromImGuiScaling));
                    canvas.SetMatrix(us.Transformation);
                //updateScaling(us.Transformation.ScaleY);

                for (int i = 0; i < drawData.CmdListsCount; ++i)
                {
                    var drawList = drawData.CmdLists[i];

                    // De-interleave all vertex data (sigh), convert to Skia types
                    //pos.Clear(); uv.Clear(); color.Clear();
                    var size = drawList.VtxBuffer.Size;

                    // Managed approach - allocates
                    //var pos = new SKPoint[size];
                    //var uv = new SKPoint[size];
                    //var color = new SKColor[size];

                    // Native approach - allocation free
                    var pos = ArrayPool<SKPoint>.Shared.Rent(size);
                    var uv = ArrayPool<SKPoint>.Shared.Rent(size);
                    var color = ArrayPool<SKColor>.Shared.Rent(size);

                    for (int j = 0; j < size; ++j)
                    {
                        var vert = drawList.VtxBuffer[j];
                        pos[j] = new SKPoint(vert.pos.X, vert.pos.Y);
                        uv[j] = new SKPoint(vert.uv.X, vert.uv.Y);
                        color[j] = vert.col;
                    }

                    // ImGui colors are RGBA
                    SKSwizzle.SwapRedBlue(MemoryMarshal.AsBytes(color.AsSpan()), MemoryMarshal.AsBytes(color.AsSpan()), size);

                    // Draw everything with canvas.drawVertices...
                    for (int j = 0; j < drawList.CmdBuffer.Size; ++j)
                    {
                        var drawCmd = drawList.CmdBuffer[j];
                        var indexOffset = (int)drawCmd.IdxOffset;
                        var clipRect = new SKRect(drawCmd.ClipRect.X, drawCmd.ClipRect.Y, drawCmd.ClipRect.Z, drawCmd.ClipRect.W);
                        canvas.Save();
                        try
                        {
                            canvas.ClipRect(clipRect);

                            // get ILayer passed via Callback instead of spooky low Texture ID
                            if (drawCmd.UserCallback != IntPtr.Zero)
                            {
                                ILayer? layer = _context.GetLayer(drawCmd.UserCallback);
                                
                                if (layer != null)
                                {
                                    canvas.SetMatrix(caller.Transformation);
                                    try
                                    {
                                        layer.Render(caller);
                                    }
                                    finally
                                    {
                                        canvas.SetMatrix(us.Transformation);
                                    }
                                }
                            }
                            else if (drawCmd.ElemCount > 0)
                            {
                                var texData = new ImTextureDataPtr(drawCmd.TexRef._TexData);
                                var textureHandle = GCHandle.FromIntPtr(texData.BackendUserData);
                                var paint = (textureHandle.Target as PrivateTextureData)?.Paint;
                                if (paint is null)
                                    continue;

                                // Managed approach - we need to allocate arrays for each call
                                //var indices = new ushort[drawCmd.ElemCount];
                                //for (int k = 0; k < indices.Length; k++)
                                //    indices[k] = drawList.IdxBuffer[indexOffset + k];

                                //canvas.DrawVertices(SKVertexMode.Triangles, pos, uv, color, SKBlendMode.Modulate, indices, paint);

                                // Native approach - allocation free
                                unsafe
                                {
                                    var indexPtr = (ushort*)drawList.IdxBuffer.Data.ToPointer() + drawCmd.IdxOffset;
                                    fixed (SKPoint* pPos = pos)
                                    fixed (SKPoint* pTex = uv)
                                    fixed (SKColor* pColor = color)
                                    {
                                        var vertices = sk_vertices_make_copy(SKVertexMode.Triangles, size, pPos, pTex, (uint*)pColor, (int)drawCmd.ElemCount, indexPtr);
                                        sk_canvas_draw_vertices(canvas.Handle, vertices, SKBlendMode.Modulate, paint.Handle);
                                        sk_vertices_unref(vertices);
                                    }
                                }
                            } 
                        }
                        finally
                        {
                            canvas.Restore();
                        }
                    }

                    ArrayPool<SKPoint>.Shared.Return(pos);
                    ArrayPool<SKPoint>.Shared.Return(uv);
                    ArrayPool<SKColor>.Shared.Return(color);
                }
            }
            finally
            {
                canvas.Restore();
            }

            void UpdateTexture(ImTextureDataPtr texture)
            {
                if (texture.Status == ImTextureStatus.WantCreate)
                {
                    //var info = new SKImageInfo(texture.Width, texture.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    //using var pmap = new SKPixmap(info, texture.Pixels, info.RowBytes);
                    var bitmap = new SKBitmap(texture.Width, texture.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                    var localMatrix = SKMatrix.CreateScale(1.0f / texture.Width, 1.0f / texture.Height);
                    bitmap.SetPixels(texture.Pixels);
                    //var fontImage = SKImage.FromPixelCopy(pmap);
                    // makeShader(SkSamplingOptions(SkFilterMode::kLinear), localMatrix);
                    var fontShader = bitmap.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix);
                    var paint = new SKPaint
                    {
                        Shader = fontShader,
                        Color = SKColors.White
                    };
                    var backendData = new PrivateTextureData
                    {
                        Paint = paint,
                        Bitmap = bitmap,
                        Shader = fontShader
                    };
                    texture.BackendUserData = GCHandle.ToIntPtr(GCHandle.Alloc(backendData));
                    texture.SetStatus(ImTextureStatus.OK);
                }
                else if (texture.Status == ImTextureStatus.WantUpdates)
                {
                    var backendData = GCHandle.FromIntPtr(texture.BackendUserData).Target as PrivateTextureData;
                    if (backendData is null)
                        return;

                    var bitmap = backendData.Bitmap;
                    for (int i = 0; i < texture.Updates.Size; i++)
                    {
                        var rect = texture.Updates[i];
                        var rowBytes = (uint)(rect.w * sizeof(Color));
                        for (int r = 0; r < rect.h; r++)
                        {
                            var dst = bitmap.GetAddress(rect.x, rect.y + r);
                            var pitch = (uint)texture.GetPitch();
                            Unsafe.CopyBlock(dst.ToPointer(), texture.GetPixelsAt(rect.x, rect.y + r).ToPointer(), rowBytes);
                        }
                    }
                    bitmap.NotifyPixelsChanged();
                    var localMatrix = SKMatrix.CreateScale(1.0f / texture.Width, 1.0f / texture.Height);
                    backendData.Paint.Shader = bitmap.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix);

                    //var info = new SKImageInfo(texture.Width, texture.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                    //using var pmap = new SKPixmap(info, texture.Pixels, info.RowBytes);
                    //var localMatrix = SKMatrix.CreateScale(1.0f / texture.Width, 1.0f / texture.Height);
                    //var fontImage = SKImage.FromPixelCopy(pmap);
                    //// makeShader(SkSamplingOptions(SkFilterMode::kLinear), localMatrix);
                    //var fontShader = fontImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix);
                    //var paint = new SKPaint
                    //{
                    //    Shader = fontShader,
                    //    Color = SKColors.White
                    //};
                    //texture.BackendUserData = GCHandle.ToIntPtr(GCHandle.Alloc(paint));
                    texture.SetStatus(ImTextureStatus.OK);
                }
                else if (texture.Status == ImTextureStatus.WantDestroy && texture.UnusedFrames > 0)
                {
                    var handle = GCHandle.FromIntPtr(texture.BackendUserData);
                    var backendData = handle.Target as PrivateTextureData;
                    if (backendData is not null)
                    {
                        backendData.Paint.Dispose();
                        backendData.Shader.Dispose();
                        backendData.Bitmap.Dispose();
                    }
                    handle.Free();

                    texture.SetTexID(default);
                    texture.BackendUserData = default;
                    texture.SetStatus(ImTextureStatus.Destroyed);
                }
            }

            // Taken from SkiaApi - allows us to draw the vertices without allocating
            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            static extern void sk_canvas_draw_vertices(IntPtr ccanvas, IntPtr vertices, SKBlendMode mode, IntPtr paint);

            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            unsafe static extern IntPtr sk_vertices_make_copy(SKVertexMode vmode, int vertexCount, SKPoint* positions, SKPoint* texs, uint* colors, int indexCount, ushort* indices);

            [DllImport("libSkiaSharp", CallingConvention = CallingConvention.Cdecl)]
            static extern void sk_vertices_unref(IntPtr cvertices);
        }

        public CallerInfo PushTransformation(CallerInfo caller, SKMatrix relative)
        {
            SKMatrix target = caller.Transformation;
#pragma warning disable CS0618 // Type or member is obsolete
            SKMatrix.PreConcat(ref target, ref relative);
#pragma warning restore CS0618 // Type or member is obsolete
            return caller.WithTransformation(target);
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            using (_context.MakeCurrent())
            {
                _io.HandleNotification(notification);

                foreach (var layer in _context.Layers)
                {
                    if (layer.Notify(notification, caller))
                        return true;
                }

                return false;
            }
        }

        public RectangleF? Bounds => default;
        //public RectangleF? Bounds
        //{
        //    get
        //    {
        //        if (_lastCallerInfo == null)
        //            return new RectangleF(0, 0, 6, 6);
        //        var _ = _lastCallerInfo.ViewportBounds;
        //        return new RectangleF(0, 0, _.Width / 100, _.Height / 100);
        //    }
        //}

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _context.Dispose();
        }

        sealed class PrivateTextureData
        {
            public SKPaint Paint;
            public SKBitmap Bitmap;
            public SKShader Shader;
        }
    }
}
