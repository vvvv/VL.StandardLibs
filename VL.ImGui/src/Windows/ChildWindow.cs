using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    /// <summary>
    /// Use child windows to begin into a self-contained independent scrolling/clipping regions within a host window. Child windows can embed their own child.
    /// </summary>
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class ChildWindow : Widget
    {
        public Widget? Content { get; set; }

        public string? Label { get; set; }

        public bool HasBorder { get; set; }

        /// <summary>
        /// For each independent axis of 'size': 
        /// ==0.0f: use remaining host window size 
        /// &gt;0.0f: fixed size 
        /// &lt;0.0f: use remaining window size minus abs(size) 
        /// Each axis can use a different mode, e.g. (0,400).
        /// </summary>
        public Vector2 Size { get; set; }

        public ImGuiNET.ImGuiWindowFlags Flags { private get; set; }

        /// <summary>
        /// Returns true if content is visible.
        /// </summary>
        public bool ContentIsVisible { get; private set; } = false;

        internal override void UpdateCore(Context context)
        {

            ContentIsVisible = ImGui.BeginChild(widgetLabel.Update(Label), Size.FromHectoToImGui(), HasBorder, Flags);
            
            try
            {
                if (ContentIsVisible)
                {
                    context.Update(Content);
                }
            }
            finally
            {
                ImGui.EndChild();
            }
        }
    }
}
