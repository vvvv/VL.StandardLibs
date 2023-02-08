//These Styles are not implemented in .NET wrapper?

//using VL.Core;
//using ImGuiNET;

//namespace VL.ImGui.Styling
//{
//    using ImGui = ImGuiNET.ImGui;

//    // We decided that the style nodes shall take all the relevant values in one go (= disable fragments).
//    [GenerateNode(Fragmented = false, Category = "ImGui.Styling", GenerateImmediate = false,
//        Tags = "DisabledAlpha")]
//    internal partial class SetPrimitiveStyle : StyleBase
//    {
//        /// <summary>
//        /// Enable anti-aliased lines/borders. Disable if you are really tight on CPU/GPU.
//        /// </summary>
//        public Optional<bool> AntiAliasedLines { private get; set; }

//        /// <summary>
//        /// Enable anti-aliased lines/borders using textures where possible. Require backend to render with bilinear filtering (NOT point/nearest filtering).
//        /// </summary>
//        public Optional<bool> AntiAliasedLinesUseTex { private get; set; }

//        /// <summary>
//        /// Enable anti-aliased edges around filled shapes (rounded rectangles, circles, etc.). Disable if you are really tight on CPU/GPU.
//        /// </summary>
//        public Optional<bool> AntiAliasedFill { private get; set; }

//        internal override void SetCore(Context context)
//        {
//            if (AntiAliasedLines.HasValue)
//            {
//                valueCount++;
//                ImGui.PushStyleVar(ImGuiStyleVar.AntiAliasedLines, AntiAliasedLines.Value);
//            }
//            if (AntiAliasedLinesUseTex.HasValue)
//            {
//                valueCount++;
//                ImGui.PushStyleVar(ImGuiStyleVar.AntiAliasedLinesUseTex, AntiAliasedLinesUseTex.Value);
//            }
//            if (AntiAliasedFill.HasValue)
//            {
//                valueCount++;
//                ImGui.PushStyleVar(ImGuiStyleVar.AntiAliasedFill, AntiAliasedFill.Value);
//            }
//        }
//    }
//}
