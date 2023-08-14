using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Access the Style structure (colors, sizes).
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries")]
    internal partial class GetStyle : Query
    {
        public StyleSnapshot? Value { get; private set; }

        internal override unsafe void UpdateCore(Context context)
        {
            var style = ImGuiNET.ImGui.GetStyle();
            Value = new StyleSnapshot(style);
        }
    }

    /// <summary>
    /// Immutable copy of all ImGui styles
    /// </summary>
    public record StyleSnapshot
    {
        internal StyleSnapshot(ImGuiStylePtr ptr)
        {
            Alpha = ptr.Alpha;
            DisabledAlpha = ptr.DisabledAlpha;
            WindowPadding = ptr.WindowPadding.ToVLHecto();
            WindowRounding = ptr.WindowRounding.ToVLHecto();
            WindowBorderSize = ptr.WindowBorderSize.ToVLHecto();
            WindowMinSize = ptr.WindowMinSize.ToVLHecto();
            WindowTitleAlign = ptr.WindowTitleAlign.ToVL();
            WindowMenuButtonPosition = ptr.WindowMenuButtonPosition;
            ChildRounding = ptr.ChildRounding.ToVLHecto();
            ChildBorderSize = ptr.ChildBorderSize.ToVLHecto();
            PopupRounding = ptr.PopupRounding.ToVLHecto();
            PopupBorderSize = ptr.PopupBorderSize.ToVLHecto();
            FramePadding = ptr.FramePadding.ToVLHecto();
            FrameRounding = ptr.FrameRounding.ToVLHecto();
            FrameBorderSize = ptr.FrameBorderSize.ToVLHecto();
            ItemSpacing = ptr.ItemSpacing.ToVLHecto();
            ItemInnerSpacing = ptr.ItemInnerSpacing.ToVLHecto();
            CellPadding = ptr.CellPadding.ToVLHecto();
            TouchExtraPadding = ptr.TouchExtraPadding.ToVLHecto();
            IndentSpacing = ptr.IndentSpacing.ToVLHecto();
            ColumnsMinSpacing = ptr.ColumnsMinSpacing.ToVLHecto();
            ScrollbarSize = ptr.ScrollbarSize.ToVLHecto();
            ScrollbarRounding = ptr.ScrollbarRounding.ToVLHecto();
            GrabMinSize = ptr.GrabMinSize.ToVLHecto();
            GrabRounding = ptr.GrabRounding.ToVLHecto();
            LogSliderDeadzone = ptr.LogSliderDeadzone.ToVLHecto();
            TabRounding = ptr.TabRounding.ToVLHecto();
            TabBorderSize = ptr.TabBorderSize.ToVLHecto();
            TabMinWidthForCloseButton = ptr.TabMinWidthForCloseButton.ToVLHecto();
            ColorButtonPosition = ptr.ColorButtonPosition;
            ButtonTextAlign = ptr.ButtonTextAlign.ToVL();
            SelectableTextAlign = ptr.SelectableTextAlign.ToVL();
            DisplayWindowPadding = ptr.DisplayWindowPadding.ToVLHecto();
            DisplaySafeAreaPadding = ptr.DisplaySafeAreaPadding.ToVLHecto();
            MouseCursorScale = ptr.MouseCursorScale.ToVLHecto();
            AntiAliasedLines = ptr.AntiAliasedLines;
            AntiAliasedLinesUseTex = ptr.AntiAliasedLinesUseTex;
            AntiAliasedFill = ptr.AntiAliasedFill;
            CurveTessellationTol = ptr.CurveTessellationTol.ToVLHecto();
            CircleTessellationMaxError = ptr.CircleTessellationMaxError.ToVLHecto();
            Colors = ptr.Colors.Select(x => x.ToVLColor4()).ToSpread();
        }

        public float Alpha { get; }

        public float DisabledAlpha { get; }

        public Vector2 WindowPadding { get; }

        public float WindowRounding { get; }

        public float WindowBorderSize { get; }

        public Vector2 WindowMinSize { get; }

        public Vector2 WindowTitleAlign { get; }

        public ImGuiDir WindowMenuButtonPosition { get; }

        public float ChildRounding { get; }

        public float ChildBorderSize { get; }

        public float PopupRounding { get; }

        public float PopupBorderSize { get; }

        public Vector2 FramePadding { get; }

        public float FrameRounding { get; }

        public float FrameBorderSize { get; }

        public Vector2 ItemSpacing { get; }

        public Vector2 ItemInnerSpacing { get; }

        public Vector2 CellPadding { get; }

        public Vector2 TouchExtraPadding { get; }

        public float IndentSpacing { get; }

        public float ColumnsMinSpacing { get; }

        public float ScrollbarSize { get; }

        public float ScrollbarRounding { get; }

        public float GrabMinSize { get; }

        public float GrabRounding { get; }

        public float LogSliderDeadzone { get; }

        public float TabRounding { get; }

        public float TabBorderSize { get; }

        public float TabMinWidthForCloseButton { get; }

        public ImGuiDir ColorButtonPosition { get; }

        public Vector2 ButtonTextAlign { get; }

        public Vector2 SelectableTextAlign { get; }

        public Vector2 DisplayWindowPadding { get; }

        public Vector2 DisplaySafeAreaPadding { get; }

        public float MouseCursorScale { get; }

        public bool AntiAliasedLines { get; }

        public bool AntiAliasedLinesUseTex { get; }

        public bool AntiAliasedFill { get; }

        public float CurveTessellationTol { get; }

        public float CircleTessellationMaxError { get; }

        public Spread<Color4> Colors { get; }
    }
}
