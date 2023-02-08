using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    internal abstract class ChannelWidget<T> : Widget
    {
        private T? value;

        public Channel<T> Channel
        {
            protected get;
            set;
        } = DummyChannel<T>.Instance; // This is the VL default

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
    }
}
