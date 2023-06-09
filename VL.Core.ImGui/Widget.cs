using System;

namespace VL.ImGui
{
    public abstract class Widget
    {
        internal virtual void Reset() { }

        protected abstract void UpdateCore(Context context);

        [Pin(Priority = 10)]
        public IStyle? Style { set; protected get; }

        public void Update(Context? context)
        {
            context = context.Validate();
            if (context != null)
            {
                try
                {
                    Style?.Set(context);
                    UpdateCore(context);
                }
                finally
                {
                    Style?.Reset(context);
                }
            }
        }
    }
}
