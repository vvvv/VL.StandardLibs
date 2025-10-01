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

            if (!context.IsBeforeFrame || CanDoStuffBeforeFrame)
            {
                colorCount = 0;
                valueCount = 0;
                SetCore(context);
            }
        }

        public void Reset(Context context)
        {
            if (!context.IsBeforeFrame || CanDoStuffBeforeFrame)
            {
                ResetCore(context);
            }

            Input?.Reset(context);
        }

        internal abstract void SetCore(Context context);
        internal virtual void ResetCore(Context context)
        {
            if (colorCount > 0)
                ImGui.PopStyleColor(colorCount);
            if (valueCount > 0)
                ImGui.PopStyleVar(valueCount);
        }

        internal virtual bool CanDoStuffBeforeFrame => false;

        // not really used
        internal void Update(Context context)
        { }
    }
}
