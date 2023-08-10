using VL.Lib.IO.Notifications;
using VL.Skia;
using ImGuiNET;
using SkiaSharp;
using System.Runtime.InteropServices;

using MouseButtons = VL.Lib.IO.MouseButtons;
using Keys = VL.Lib.IO.Keys;
using System.Buffers;
using VL.Lib.Collections;
using System.Text;
using SixLabors.Fonts;

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

        public bool DefaultWindow = true;

        // OpenGLES rendering (https://github.com/dotnet/Silk.NET/tree/v2.15.0/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui)
        private readonly SkiaContext _context;
        private readonly RenderContext _renderContext;
        private readonly Handle<SKPaint> _fontPaint;
        float _fontScaling;
        float _uiScaling;
        Spread<FontConfig> _fonts = Spread<FontConfig>.Empty;

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
                _io.NativePtr->IniFilename = null;

                _renderContext = RenderContext.ForCurrentThread();

                _fontPaint = new Handle<SKPaint>(new SKPaint());

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                updateScaling(fontScaling: scaling, uiScaling: scaling);
            }
        }

        public ILayer Update(Widget widget, bool dockingEnabled, Spread<FontConfig> fonts, IStyle? Style)
        {
            if (_lastCallerInfo is null)
                return this;

            using (_context.MakeCurrent())
            {
                if (!_fonts.SequenceEqual(fonts))
                {
                    _fonts = fonts;
                    BuildImFontAtlas(_io.Fonts, _fontPaint, _fontScaling, fonts);
                }

                var bounds = _lastCallerInfo.ViewportBounds;

                _io.DisplaySize = new Vector2(bounds.Width, bounds.Height);

                // Enable Docking
                if (dockingEnabled)
                    _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                _context.NewFrame();
                try
                {
                    using var _ = _context.ApplyStyle(Style);

                    if (DefaultWindow)
                    {
                        var viewPort = ImGui.GetMainViewport();
                        ImGui.SetNextWindowPos(viewPort.WorkPos);
                        ImGui.SetNextWindowSize(viewPort.WorkSize);
                        ImGui.Begin(widgetLabel.Update(null),
                            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                            ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                            ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                            ImGuiWindowFlags.NoBackground);
                    }

                    // Enable Docking
                    if (dockingEnabled)
                    {
                        ImGui.DockSpaceOverViewport();
                    }

                    _context.SetDrawList(DrawList.Foreground);
                    // ImGui.ShowDemoWindow();
                    _context.Update(widget);
                }
                finally
                {
                    if (dockingEnabled)
                    {
                        ImGui.End();
                    }

                    if (DefaultWindow)
                    {
                        ImGui.End();
                    }

                    // Render (builds mesh with texture coordinates)
                    ImGui.Render();
                }

                // Render the mesh
                _drawDataPtr = ImGui.GetDrawData();
                _readyToBeDrawn = true;
            }

            return this;
        }

        void updateScaling(float fontScaling, float uiScaling)
        {
            if (fontScaling != _fontScaling)
            {
                _fontScaling = fontScaling;
                BuildImFontAtlas(_io.Fonts, _fontPaint, fontScaling, _fonts);
            }
            if (uiScaling != _uiScaling) 
            {
                _uiScaling = uiScaling;
                UpdateUIScaling(uiScaling);
            }
        }

        public unsafe void Render(CallerInfo caller)
        {
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

        void BuildImFontAtlas(ImFontAtlasPtr atlas, Handle<SKPaint> paintHandle, float scaling, Spread<FontConfig> fonts)
        {
            atlas.Clear();
            _context.Fonts.Clear();

            var anyFontLoaded = false;
            var fontsfolder = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

            if (fonts.IsEmpty && FontConfig.Default != null)
                fonts = Spread.Create(FontConfig.Default);

            foreach (var font in fonts)
            {
                if (font is null)
                    continue;

                var size = Math.Clamp(font.Size * 100 /* hecto pixel */ * scaling, 1, short.MaxValue);

                var family = SystemFonts.Families.FirstOrDefault(f => f.Name == font.FamilyName.Value);
                if (family.Name is null)
                    continue;

                var systemFont = family.CreateFont((float)(size * 0.75f) /* PT */, font.FontStyle);
                if (!systemFont.TryGetPath(out var path))
                    continue;

                ImFontConfig cfg = new ImFontConfig()
                {
                    SizePixels = size,
                    FontDataOwnedByAtlas = 0,
                    EllipsisChar = unchecked((ushort)-1),
                    OversampleH = 1,
                    OversampleV = 1,
                    PixelSnapH = 1,
                    GlyphOffset = new Vector2(0, 0),
                    GlyphMaxAdvanceX = float.MaxValue,
                    RasterizerMultiply = 1.0f
                };

                unsafe
                {
                    // Write name
                    Span<byte> s = Encoding.Default.GetBytes(font.ToString());
                    var dst = new Span<byte>(cfg.Name, 40);
                    s.Slice(0, Math.Min(s.Length, dst.Length)).CopyTo(dst);

                    var f = atlas.AddFontFromFileTTF(path, cfg.SizePixels, &cfg, GetGlypthRange(atlas, font.GlyphRange));
                    anyFontLoaded = true;
                    _context.Fonts[font.Name] = f;
                }
            }

            if (!anyFontLoaded)
            {
                atlas.AddFontDefault();
            }

            atlas.GetTexDataAsAlpha8(out IntPtr pixels, out var w, out var h);

            if (w == 0)
            {
                // Something went wrong, load default font
                atlas.Clear();
                _context.Fonts.Clear();
                atlas.AddFontDefault();
                atlas.GetTexDataAsAlpha8(out pixels, out w, out h);
            }

            var info = new SKImageInfo(w, h, SKColorType.Alpha8);
            using var pmap = new SKPixmap(info, pixels, info.RowBytes);
            var localMatrix = SKMatrix.CreateScale(1.0f / w, 1.0f / h);
            var fontImage = SKImage.FromPixelCopy(pmap);
            // makeShader(SkSamplingOptions(SkFilterMode::kLinear), localMatrix);
            var fontShader = fontImage.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, localMatrix);
            var paint = paintHandle.Target;
            if (paint != null)
            {
                paint.Shader = fontShader;
                paint.Color = SKColors.White;
                atlas.TexID = paintHandle.Ptr;
            }

            static IntPtr GetGlypthRange(ImFontAtlasPtr atlas, GlyphRange glyphRange)
            {
                return glyphRange switch
                {
                    GlyphRange.ChineseFull => atlas.GetGlyphRangesChineseFull(),
                    GlyphRange.ChineseSimplifiedCommon => atlas.GetGlyphRangesChineseSimplifiedCommon(),
                    GlyphRange.Cyrillic => atlas.GetGlyphRangesCyrillic(),
                    GlyphRange.Greek => atlas.GetGlyphRangesGreek(),
                    GlyphRange.Japanese => atlas.GetGlyphRangesJapanese(),
                    GlyphRange.Korean => atlas.GetGlyphRangesKorean(),
                    GlyphRange.Thai => atlas.GetGlyphRangesThai(),
                    GlyphRange.Vietnamese => atlas.GetGlyphRangesVietnamese(),
                    _ => atlas.GetGlyphRangesDefault()
                };
            }
        }

        static unsafe void UpdateUIScaling(float scaling = 1f)
        {
            var style = ImGui.GetStyle();
            style.ScaleAllSizes(scaling);

            // From https://github.com/ocornut/imgui/discussions/3925
            //ImGuiStyle* p = style;
            //ImGuiStyle styleold = *p; // Backup colors
            //var style = new ImGuiStyle(); // IMPORTANT: ScaleAllSizes will change the original size, so we should reset all style config
            //style.WindowBorderSize = 1.0f;
            //style.ChildBorderSize = 1.0f;
            //style.PopupBorderSize = 1.0f;
            //style.FrameBorderSize = 1.0f;
            //style.TabBorderSize = 1.0f;
            //style.WindowRounding = 0.0f;
            //style.ChildRounding = 0.0f;
            //style.PopupRounding = 0.0f;
            //style.FrameRounding = 0.0f;
            //style.ScrollbarRounding = 0.0f;
            //style.GrabRounding = 0.0f;
            //style.TabRounding = 0.0f;
            //style.ScaleAllSizes(scale);
        }

        // From https://github.com/google/skia/blob/main/tools/viewer/ImGuiLayer.cpp
        private void Render(CallerInfo caller, ImDrawDataPtr drawData)
        {
            var canvas = caller.Canvas;
            //using var _ = new SKAutoCanvasRestore(canvas, true);
            canvas.Save();
            try
            {
                var us = PushTransformation(caller, SKMatrix.CreateScale(ImGuiConversion.FromImGuiScaling, ImGuiConversion.FromImGuiScaling));
                    canvas.SetMatrix(us.Transformation);
                //updateScaling(us.Transformation.ScaleY);

                for (int i = 0; i < drawData.CmdListsCount; ++i)
                {
                    var drawList = drawData.CmdListsRange[i];

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
                            
                            // TODO: Find min/max index for each draw, so we know how many vertices (sigh)
                            if (drawCmd.UserCallback != IntPtr.Zero)
                            {
                                var handle = GCHandle.FromIntPtr(drawCmd.UserCallback);
                                try
                                {
                                    if (handle.Target is DrawCallback callback)
                                        callback(drawList, drawCmd);
                                }
                                finally
                                {
                                    handle.Free();
                                }
                            }
                            else
                            {
                                var idIndex = drawCmd.TextureId.ToInt64();
                                if (0 <= idIndex && idIndex < _context.Layers.Count)
                                {
                                    // Small image IDs are actually indices into a list of callbacks. We directly
                                    // examing the vertex data to deduce the image rectangle, then reconfigure the
                                    // canvas to be clipped and translated so that the callback code gets to use
                                    // Skia to render a widget in the middle of an ImGui panel.
                                    var rectIndex = drawList.IdxBuffer[indexOffset];
                                    var tl = pos[rectIndex];
                                    var br = pos[rectIndex + 2];
                                    var imageClipRect = new SKRect(tl.X, tl.Y, br.X, br.Y);

                                        canvas.SetMatrix(caller.Transformation);
                                    try
                                    {
                                        _context.Layers[(int)idIndex].Render(caller);
                                    }
                                    finally
                                    {
                                            canvas.SetMatrix(us.Transformation);
                                    }
                                }
                                else if (drawCmd.ElemCount > 0)
                                {
                                    var handle = GCHandle.FromIntPtr(drawCmd.TextureId);
                                    var paint = handle.Target as SKPaint ?? _fontPaint.Target;
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
            using (_context.MakeCurrent())
            {
                if (notification is KeyNotification keyNotification)
                {
                    // Submit modifiers, see https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_win32.cpp#L620
                    _io.AddKeyEvent(ImGuiKey.ModCtrl, keyNotification.CtrlKey);
                    _io.AddKeyEvent(ImGuiKey.ModShift, keyNotification.ShiftKey);
                    _io.AddKeyEvent(ImGuiKey.ModAlt, keyNotification.AltKey);

                    if (keyNotification is KeyCodeNotification keyCodeNotification)
                    {
                        _io.AddKeyEvent(ToImGuiKey(keyCodeNotification.KeyCode), keyCodeNotification.IsKeyDown);
                    }
                    else if (keyNotification is KeyPressNotification keyPressNotification)
                    {
                        _io.AddInputCharacter(keyPressNotification.KeyChar);
                    }
                }
                else if (notification is MouseNotification mouseNotification)
                {
                    var button = 0;
                    if (mouseNotification is MouseButtonNotification b)
                    {
                        button = b.Buttons switch
                        {
                            MouseButtons.Left => 0,
                            MouseButtons.Right => 1,
                            MouseButtons.Middle => 2,
                            MouseButtons.XButton1 => 3,
                            MouseButtons.XButton2 => 4,
                            _ => 0
                        };
                    }

                    // The up & down event methods don't take the position as an argument. Therefor make sure it's present, or we end up with wrong clicks when using touch devices.
                    var pos = mouseNotification.PositionInWorldSpace.FromHectoToImGui();
                    _io.AddMousePosEvent(pos.X, pos.Y);

                    switch (mouseNotification.Kind)
                    {
                        case MouseNotificationKind.MouseDown:
                            _io.AddMouseButtonEvent(button, true);
                            break;
                        case MouseNotificationKind.MouseUp:
                            _io.AddMouseButtonEvent(button, false);
                            break;
                        case MouseNotificationKind.MouseWheel:
                            if (mouseNotification is MouseWheelNotification wheel)
                                _io.AddMouseWheelEvent(0, wheel.WheelDelta / 120);
                            break;
                        case MouseNotificationKind.MouseHorizontalWheel:
                            if (mouseNotification is MouseHorizontalWheelNotification hWheel)
                                _io.AddMouseWheelEvent(hWheel.WheelDelta / 120, 0);
                            break;
                        case MouseNotificationKind.DeviceLost:
                            _io.ClearInputCharacters();
                            _io.ClearInputKeys();
                            break;
                        default:
                            break;
                    }
                }
                else if (notification is TouchNotification touchNotification)
                {
                    // ImGui has no touch - we rely on the mouse emulation of the event system
                }

                foreach (var layer in _context.Layers)
                {
                    layer.Notify(notification, caller);
                }

                return false;
            }
        }

        static ImGuiKey ToImGuiKey(Keys key)
        {
            switch (key)
            {
                case Keys.Back: return ImGuiKey.Backspace;
                case Keys.Tab: return ImGuiKey.Tab;
                case Keys.Enter: return ImGuiKey.Enter;
                case Keys.ShiftKey: return ImGuiKey.LeftShift;
                case Keys.ControlKey:  return ImGuiKey.LeftCtrl;
                case Keys.Menu:  return ImGuiKey.Menu;
                case Keys.Pause:  return ImGuiKey.Pause;
                case Keys.CapsLock: return ImGuiKey.CapsLock;
                case Keys.Escape: return ImGuiKey.Escape;
                case Keys.Space: return ImGuiKey.Space;
                case Keys.PageUp: return ImGuiKey.PageUp;
                case Keys.PageDown: return ImGuiKey.PageDown;
                case Keys.End: return ImGuiKey.End;
                case Keys.Home: return ImGuiKey.Home;
                case Keys.Left: return ImGuiKey.LeftArrow;
                case Keys.Up:  return ImGuiKey.UpArrow;
                case Keys.Right: return ImGuiKey.RightArrow;
                case Keys.Down: return ImGuiKey.DownArrow;
                case Keys.Snapshot: return ImGuiKey.PrintScreen;
                case Keys.Insert: return ImGuiKey.Insert;
                case Keys.Delete: return ImGuiKey.Delete;
                case Keys.Oem7: return ImGuiKey.Apostrophe;
                case Keys.Oemcomma: return ImGuiKey.Comma;
                case Keys.OemMinus: return ImGuiKey.Minus;
                case Keys.OemPeriod: return ImGuiKey.Period;
                case Keys.Oem2: return ImGuiKey.Slash;
                case Keys.Oem1: return ImGuiKey.Semicolon;
                case Keys.Oemplus: return ImGuiKey.Equal;
                case Keys.Oem4: return ImGuiKey.LeftBracket;
                case Keys.Oem5: return ImGuiKey.Backslash;
                case Keys.Oem6: return ImGuiKey.RightBracket;
                case Keys.Oem3: return ImGuiKey.GraveAccent;
                case Keys.D0: return ImGuiKey._0;
                case Keys.D1: return ImGuiKey._1;
                case Keys.D2: return ImGuiKey._2;
                case Keys.D3: return ImGuiKey._3;
                case Keys.D4: return ImGuiKey._4;
                case Keys.D5: return ImGuiKey._5;
                case Keys.D6: return ImGuiKey._6;
                case Keys.D7: return ImGuiKey._7;
                case Keys.D8: return ImGuiKey._8;
                case Keys.D9: return ImGuiKey._9;
                case Keys.A: return ImGuiKey.A;
                case Keys.B: return ImGuiKey.B;
                case Keys.C: return ImGuiKey.C;
                case Keys.D: return ImGuiKey.D;
                case Keys.E: return ImGuiKey.E;
                case Keys.F: return ImGuiKey.F;
                case Keys.G: return ImGuiKey.G;
                case Keys.H: return ImGuiKey.H;
                case Keys.I: return ImGuiKey.I;
                case Keys.J: return ImGuiKey.J;
                case Keys.K: return ImGuiKey.K;
                case Keys.L: return ImGuiKey.L;
                case Keys.M: return ImGuiKey.M;
                case Keys.N: return ImGuiKey.N;
                case Keys.O: return ImGuiKey.O;
                case Keys.P: return ImGuiKey.P;
                case Keys.Q: return ImGuiKey.Q;
                case Keys.R: return ImGuiKey.R;
                case Keys.S: return ImGuiKey.S;
                case Keys.T: return ImGuiKey.T;
                case Keys.U: return ImGuiKey.U;
                case Keys.V: return ImGuiKey.V;
                case Keys.W: return ImGuiKey.W;
                case Keys.X: return ImGuiKey.X;
                case Keys.Y: return ImGuiKey.Y;
                case Keys.Z: return ImGuiKey.Z;
                case Keys.NumPad0: return ImGuiKey.Keypad0;
                case Keys.NumPad1: return ImGuiKey.Keypad1;
                case Keys.NumPad2: return ImGuiKey.Keypad2;
                case Keys.NumPad3: return ImGuiKey.Keypad3;
                case Keys.NumPad4: return ImGuiKey.Keypad4;
                case Keys.NumPad5: return ImGuiKey.Keypad5;
                case Keys.NumPad6: return ImGuiKey.Keypad6;
                case Keys.NumPad7: return ImGuiKey.Keypad7;
                case Keys.NumPad8: return ImGuiKey.Keypad8;
                case Keys.NumPad9: return ImGuiKey.Keypad9;
                case Keys.Multiply: return ImGuiKey.KeypadMultiply;
                case Keys.Add: return ImGuiKey.KeypadAdd;
                case Keys.Subtract: return ImGuiKey.KeypadSubtract;
                case Keys.Decimal: return ImGuiKey.KeypadDecimal;
                case Keys.Divide: return ImGuiKey.KeypadDivide;
                case Keys.F1: return ImGuiKey.F1;
                case Keys.F2: return ImGuiKey.F2;
                case Keys.F3: return ImGuiKey.F3;
                case Keys.F4: return ImGuiKey.F4;
                case Keys.F5: return ImGuiKey.F5;
                case Keys.F6: return ImGuiKey.F6;
                case Keys.F7: return ImGuiKey.F7;
                case Keys.F8: return ImGuiKey.F8;
                case Keys.F9: return ImGuiKey.F9;
                case Keys.F10: return ImGuiKey.F10;
                case Keys.F11: return ImGuiKey.F11;
                case Keys.F12: return ImGuiKey.F12;
                case Keys.NumLock: return ImGuiKey.NumLock;
                case Keys.Scroll: return ImGuiKey.ScrollLock;
                case Keys.LShiftKey: return ImGuiKey.LeftShift;
                case Keys.RShiftKey: return ImGuiKey.RightShift;
                case Keys.LControlKey: return ImGuiKey.LeftCtrl;
                case Keys.RControlKey: return ImGuiKey.RightCtrl;
                case Keys.LMenu: return ImGuiKey.LeftAlt;
                case Keys.RMenu: return ImGuiKey.RightAlt;
                case Keys.LWin: return ImGuiKey.LeftSuper;
                case Keys.RWin: return ImGuiKey.RightSuper;
                default:
                    return ImGuiKey.None;
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
            _renderContext.Dispose();

            _context.Dispose();
        }
    }
}
