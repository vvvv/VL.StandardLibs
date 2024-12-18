using ImGuiNET;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    internal abstract class ChannelWidget<T> : Widget, IHasLabel
    {
        protected readonly LabelSelector label;
        private readonly SerialDisposable attributesSubscription = new();
        private readonly List<ValueSelector> valueSelectors = new();
        private IChannel<T>? channel;
        private T? value;

        public ChannelWidget()
        {
            AddValueSelector(label = new());
        }

        public IChannel<T>? Channel
        {
            protected get => channel;
            set
            {
                if (value != channel)
                {
                    var old = channel;
                    channel = value;
                    attributesSubscription.Disposable = channel?.Attributes().StartWith([channel?.Attributes().Value]).Subscribe(a => UpdateDefaultsFromAttributes(a ?? Spread<Attribute>.Empty));

                    OnChannelChanged(value, old);
                }
            }
        }

        protected virtual void OnChannelChanged(IChannel<T>? channel, IChannel<T>? old)
        {
        }

        public Optional<string> Label { get => default /* Only accessed by generated code*/; set => label.SetPinValue(value!); }

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

        protected void AddValueSelector(ValueSelector valueSelector) => valueSelectors.Add(valueSelector);

        private void UpdateDefaultsFromAttributes(Spread<Attribute> attributes)
        {
            foreach (var valueSelector in valueSelectors)
                valueSelector.Update(attributes);
        }

        string? IHasLabel.Label { get => label.Value; set => label.SetPinValue(value); }

        protected override void Dispose(bool disposing)
        {
            Channel = null;
            base.Dispose(disposing);
        }

    }
}
