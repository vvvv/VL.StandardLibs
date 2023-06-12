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
using VL.Core.ImGui.SkiaBackend;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    using RectangleF = Stride.Core.Mathematics.RectangleF;
    using Vector2 = System.Numerics.Vector2;

    public class ToSkiaLayer : IDisposable, ILayer
    {
        readonly ImGuiIOPtr _io;
        readonly ImGuiPlatformIOPtr _platform;
        readonly SkiaImguiRenderer _renderer;

        public bool DefaultWindow = true;

        // OpenGLES rendering (https://github.com/dotnet/Silk.NET/tree/v2.15.0/src/OpenGL/Extensions/Silk.NET.OpenGL.Extensions.ImGui)
        private readonly SkiaContext _context;

        float _fontScaling;
        float _uiScaling;
        Spread<FontConfig> _fonts = Spread<FontConfig>.Empty;

        CallerInfo? _lastCallerInfo;
        ImDrawDataPtr _drawDataPtr;
        bool _readyToBeDrawn;

        public unsafe ToSkiaLayer()
        {
            _context = new SkiaContext();
            _renderer = new SkiaImguiRenderer(_context);
            using (_context.MakeCurrent())
            {
                _io = ImGui.GetIO();
                _io.NativePtr->IniFilename = null;

                _platform = ImGui.GetPlatformIO();

                var scaling = VL.UI.Core.DIPHelpers.DIPFactor();
                updateScaling(fontScaling: scaling, uiScaling: scaling);
            }
        }

        public ImGuiIOPtr IO => _io;

        public ImGuiPlatformIOPtr PlatformIO => _platform;

        public SkiaContext Context => _context;

        public ILayer Update(Widget widget, bool dockingEnabled, Spread<FontConfig> fonts, IStyle? Style)
        {
            if (_lastCallerInfo is null)
                return this;

            using (_context.MakeCurrent())
            {
                if (!_fonts.SequenceEqual(fonts))
                {
                    _fonts = fonts;
                    BuildImFontAtlas(_io.Fonts, _fontScaling, fonts);
                }

                var bounds = _lastCallerInfo.ViewportBounds;

                _io.DisplaySize = new Vector2(bounds.Width, bounds.Height);

                // Enable Docking
                if (dockingEnabled)
                    _io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

                _io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

                _context.NewFrame();
                try
                {
                    Style?.Set(_context);

                    if (DefaultWindow)
                    {
                        var viewPort = ImGui.GetMainViewport();
                        ImGui.SetNextWindowPos(viewPort.WorkPos);
                        ImGui.SetNextWindowSize(viewPort.WorkSize);
                        ImGui.Begin(VL.ImGui.Context.GetLabel(this, null),
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
                        //ImGui.End();
                    }

                    if (DefaultWindow)
                    {
                        ImGui.End();
                        Style?.Reset(_context);
                    }

                    // Render (builds mesh with texture coordinates)
                    ImGui.Render();
                }

                // Render the mesh
                _drawDataPtr = ImGui.GetDrawData();
                _readyToBeDrawn = true;

                // Update and Render additional Platform Windows
                if (_io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
                {
                    ImGui.UpdatePlatformWindows();
                    ImGui.RenderPlatformWindowsDefault();
                }
            }

            return this;
        }

        void updateScaling(float fontScaling, float uiScaling)
        {
            if (fontScaling != _fontScaling)
            {
                _fontScaling = fontScaling;
                BuildImFontAtlas(_io.Fonts, fontScaling, _fonts);
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

        void BuildImFontAtlas(ImFontAtlasPtr atlas, float scaling, Spread<FontConfig> fonts)
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

                var family = SystemFonts.Families.FirstOrDefault(f => f.Name == font.FamilyName);
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

            _renderer.CreateFontsTexture();

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
            _renderer.Render(caller, drawData, applyHectoPixelScaling: true);
        }

        public bool Notify(INotification notification, CallerInfo caller)
        {
            return _context.Notify(notification, caller, applyHectoPixelScaling: true);
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
            _renderer.Dispose();

            _context.Dispose();
        }
    }
}
