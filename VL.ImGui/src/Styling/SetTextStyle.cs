using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core;

namespace VL.ImGui.Styling
{
    using ImGui = ImGuiNET.ImGui;

    // We decided that the style nodes shall take all the relevant values in one go (= disable fragments).
    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false, 
        Tags = "Color DisabledColor SelectedTextBg")]
    internal partial class SetTextStyle : StyleBase
    {
        private bool fontPushed;

        public Optional<Color4> Color { private get; set; }

        public Optional<Color4> DisabledColor { private get; set; }

        public Optional<Color4> SelectedTextBackground { private get; set; }

        public string? FontName { private get; set; }

        /// <summary>
        /// The size of the font in device independent hecto pixel (1 = 100 DIP).
        /// </summary>
        public Optional<float> FontSize { private get; set; }

        internal override void SetCore(Context context)
        {
            if (Color.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.Text, Color.Value.ToImGui());
            }
            if (DisabledColor.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TextDisabled, DisabledColor.Value.ToImGui());
            }
            if (SelectedTextBackground.HasValue)
            {
                colorCount++;
                ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, SelectedTextBackground.Value.ToImGui());
            }
            if (FontName != null || FontSize.HasValue)
            {
                var font = FontName != null ? context.Fonts.GetValueOrDefault(FontName) : default;
                var fontSize = FontSize.HasValue ? Math.Clamp(FontSize.Value.FromHectoToImGui(), 0f, float.MaxValue) : font.LegacySize;
                ImGui.PushFont(font, fontSize);
                fontPushed = true;
            }
        }

        internal override void ResetCore(Context context)
        {
            if (fontPushed)
            {
                ImGui.PopFont();
                fontPushed = false;
            }

            base.ResetCore(context);
        }
    }
}
