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

            SetCore(context);
        }

        public void Reset(Context context)
        {
            ResetCore(context);

            Input?.Reset(context);
        }

        internal abstract void SetCore(Context context);
        internal virtual void ResetCore(Context context)
        {
            var colorCount = Interlocked.Exchange(ref this.colorCount, 0);
            if (colorCount > 0)
                ImGui.PopStyleColor(colorCount);
            var valueCount = Interlocked.Exchange(ref this.valueCount, 0);
            if (valueCount > 0)
                ImGui.PopStyleVar(valueCount);
        }

        // not really used
        internal void Update(Context context)
        { }
    }
}
