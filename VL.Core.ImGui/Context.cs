using ImGuiNET;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using VL.Lib.Collections;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    public static class ContextHelpers
    {        
        public static Context? Validate(this Context? c) => c ?? Context.Current;
    }


    public class Context : IDisposable
    {
        [ThreadStatic]
        public static int WidgetCreationCounter;

        [ThreadStatic]
        static Dictionary<object, string> Labels = new Dictionary<object, string>();

        private readonly IntPtr _context;
        private readonly List<Widget> _widgetsToReset = new List<Widget>();

        [ThreadStatic]
        internal static Context? Current = null;

        internal ImDrawListPtr DrawListPtr;
        internal DrawList DrawList;
        internal System.Numerics.Vector2 DrawListOffset;
        internal bool IsInBeginTables;

        public Context()
        {
            _context = ImGui.CreateContext();
        }

        public ImGuiIOPtr IO
        {
            get
            {
                ThrowIfNotCurrent();
                return ImGui.GetIO();
            }
        }

        public ImGuiPlatformIOPtr PlatformIO
        {
            get
            {
                ThrowIfNotCurrent();
                return ImGui.GetPlatformIO();
            }
        }

        public uint DockSpaceId { get; set; }

        private void ThrowIfNotCurrent()
        {
            if (Current != this)
                throw new InvalidOperationException("The context must be current");
        }

        public virtual void NewFrame()
        {
            try
            {
                foreach (var widget in _widgetsToReset)
                    widget.Reset();
            }
            finally
            {
                _widgetsToReset.Clear();
            }

            ImGui.NewFrame();
            SetDrawList(DrawList.Foreground);
        }

        public Frame MakeCurrent()
        {
            return new Frame(_context, this);
        }

        public void Update(Widget? widget)
        {
            if (widget is null)
                return;

            widget.Update(this);
            _widgetsToReset.Add(widget);
        }

        internal void AddToResetQueue(Widget widget)
        {
            _widgetsToReset.Add(widget);
        }

        public void SetDrawList(DrawList drawList)
        {
            DrawList = drawList;

            DrawListPtr = drawList switch
            {
                DrawList.AtCursor => ImGui.GetWindowDrawList(),
                DrawList.Window => ImGui.GetWindowDrawList(),
                DrawList.Foreground => ImGui.GetForegroundDrawList(),
                DrawList.Background => ImGui.GetBackgroundDrawList(),
                _ => throw new NotImplementedException()
            };

            DrawListOffset = drawList switch
            {
                DrawList.AtCursor => ImGui.GetWindowPos() + ImGui.GetCursorPos() - new System.Numerics.Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()),
                DrawList.Window => ImGui.GetWindowPos(),
                DrawList.Foreground => default,
                DrawList.Background => default,
                _ => throw new NotImplementedException()
            };             

            // TODO: All points are drawn in the main viewport. In order to have them drawn inside the window without having to transform them manually
            // we should look into the drawList.AddCallback(..., ...) method. It should allow us to modify the transformation matrix and clipping rects.
        }

        public void BuildImFontAtlas(float scaling, Spread<FontConfig> fonts)
        {
            var atlas = IO.Fonts;

            atlas.Clear();
            Fonts.Clear();

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
                    Fonts[font.Name] = f;
                }
            }

            if (!anyFontLoaded)
            {
                atlas.AddFontDefault();
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

        public void Dispose()
        {
            ImGui.DestroyContext(_context);
        }

        internal static string GetLabel(object widget, string? label)
        {
            if (!string.IsNullOrWhiteSpace(label))
                return label;

            if (Labels is null)
                Labels = new Dictionary<object, string>();

            if (Labels.TryGetValue(widget, out label))
                return label;

            label = $"##__<{++WidgetCreationCounter}>";
            Labels.Add(widget, label);
            return label;
        }

        internal readonly Dictionary<string, ImFontPtr> Fonts = new Dictionary<string, ImFontPtr>();

        public readonly struct Frame : IDisposable
        {
            readonly IntPtr previous;
            readonly Context? previous2;

            public Frame(IntPtr context, Context c)
            {
                previous = ImGui.GetCurrentContext();
                ImGui.SetCurrentContext(context);
                previous2 = Current;
                Current = c;
            }

            public void Dispose()
            {
                Current = previous2;
                ImGui.SetCurrentContext(previous);
            }
        }
    }
}
