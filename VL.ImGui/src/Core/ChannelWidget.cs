using ImGuiNET;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    internal abstract class ChannelWidget<T> : Widget
    {
        private T? value;

        public IChannel<T>? Channel
        {
            protected get;
            set;
        }

        public bool Bang 
        { 
            get; 
            private set; 
        }

        public T? Value
        {
            get => value;
            protected set
            {
                this.value = value;
                Bang = true;
                if (Channel.IsValid())
                    Channel.Value = value!;
            }
        }

        protected T? Update()
        {
            Bang = false;
            if (Channel.IsValid())
                value = Channel.Value;
            return value;
        }

        protected void SetValueWithoutNotifiying(T value)
        {
            this.value = value;
        }

        protected void SetValueIfChanged(T? oldValue, T newValue, ImGuiInputTextFlags flags)
        {
            if (flags.HasFlag(ImGuiInputTextFlags.EnterReturnsTrue))
                Value = newValue;
            else if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
                Value = newValue;
        }
    }
}
