namespace VL.ImGui.Styling
{
    using ImGui = ImGuiNET.ImGui;

    internal abstract class StyleBase : IStyle
    {
        public IStyle? Input { protected get; set; }

        protected int colorCount;
        protected int valueCount;
        
        public void Set(Context context)
        {
            Input?.Set(context);
            colorCount = 0;
            valueCount = 0;
            SetCore(context);
        }

        public virtual void Reset(Context context)
        {
            if (colorCount > 0)
                ImGui.PopStyleColor(colorCount);
            if (valueCount > 0)
                ImGui.PopStyleVar(valueCount);
            Input?.Reset(context);
        }

        internal abstract void SetCore(Context context);


        // not really used
        internal void Update(Context context)
        { }
    }
}
