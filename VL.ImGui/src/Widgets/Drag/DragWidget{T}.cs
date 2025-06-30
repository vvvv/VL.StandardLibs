using VL.Core;

namespace VL.ImGui.Widgets
{
    internal abstract class DragWidgetBase<T, TComponent> : ChannelWidget<T>
        where T : unmanaged
        where TComponent : unmanaged
    {
        protected ValueSelector<TComponent> min;
        protected ValueSelector<TComponent> max;

        public DragWidgetBase()
        {
        }

        public float Speed { protected get; set; } = typeof(TComponent) == typeof(float) || typeof(TComponent) == typeof(double) ? 0.01f : 1f;

        public Optional<TComponent> Min { protected get => default; set => min.SetPinValue(value); }

        public Optional<TComponent> Max { protected get => default; set => max.SetPinValue(value); }

        /// <summary>
        /// Adjust format string to decorate the value with a prefix, a suffix, or adapt the editing and display precision e.g. "%.3f" -> 1.234; "%5.2f secs" -> 01.23 secs; "Biscuit: % .0f" -> Biscuit: 1; etc.
        /// </summary>
        public string? Format { protected get; set; }

        public ImGuiNET.ImGuiSliderFlags Flags { protected get; set; }

        public bool NotifyWhileTyping { protected get; set; }

        internal override sealed void UpdateCore(Context context)
        {
            var previousValue = Value;
            var value = Update();
            if (NotifyWhileTyping)
            {
                if (Drag(widgetLabel.Update(label.Value), ref value, Speed, min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                    Value = value;
            }
            else
            {
                if (Drag(widgetLabel.Update(label.Value), ref value, Speed, min.Value, max.Value, string.IsNullOrWhiteSpace(Format) ? null : Format, Flags))
                {
                    if (ImGuiNET.ImGui.IsMouseDragging(ImGuiNET.ImGuiMouseButton.Left))
                    {
                        wasDragging = true;
                        Value = value;
                    }
                    else
                    {
                        // We're in typing state
                        wasDragging = false;
                        SetValueWithoutNotifiying(value);
                    }
                }
                else if (ImGuiNET.ImGui.IsItemDeactivatedAfterEdit() && !ImGuiNET.ImGui.IsKeyDown(ImGuiNET.ImGuiKey.Escape))
                {
                    // In case we TAB out of the widget, ImGui reports false and gives us the initial value which is not what we want.
                    value = previousValue;
                }

                if (ImGuiNET.ImGui.IsItemDeactivatedAfterEdit() && !wasDragging)
                    Value = value;
            }
        }

        private bool wasDragging;

        protected abstract bool Drag(string label, ref T value, float speed, TComponent min, TComponent max, string? format, ImGuiNET.ImGuiSliderFlags flags);
    }


    internal abstract class DragWidget<T, TComponent> : DragWidgetBase<T, TComponent>
        where T : unmanaged
        where TComponent : unmanaged, System.Numerics.IMinMaxValue<TComponent>
    {
        public DragWidget()
        {
            AddValueSelector(this.min = new MinValueSelector<TComponent>(default));
            AddValueSelector(this.max = new MaxValueSelector<TComponent>(default));
        }
    }

    internal abstract class DragWidget_Simple<T, TComponent> : DragWidgetBase<T, TComponent>
        where T : unmanaged
        where TComponent : unmanaged
    {

        public DragWidget_Simple(TComponent min, TComponent max)
        {
            AddValueSelector(this.min = new MinValueSelector_Simple<TComponent>(min));
            AddValueSelector(this.max = new MaxValueSelector_Simple<TComponent>(max));
        }
    }
}
