using System;

namespace VL.ImGui
{
    public abstract class Widget
    {
        internal virtual void Reset() { }

        internal abstract void UpdateCore(Context context);

        [Pin(Priority = 10)]
        public IStyle? Style { set; protected get; }

        protected virtual bool HasItemState => true;

        protected WidgetLabel widgetLabel = new();

        internal void Update(Context? context)
        {
            context = context.Validate();
            if (context != null)
            {
                // If the item comes with its own state, clear any captured state of ours
                if (HasItemState)
                    context.CapturedItemState = default;

                using (context.ApplyStyle(Style))
                    UpdateCore(context);
            }
        }
    }
}
