using System.Reactive.Subjects;
using VL.Core;

namespace VL.Video.CaptureControl
{
    sealed class Property<TName> : IVLPin<Optional<float>>
    {
        public readonly TName Name;
        public readonly string Display;
        public readonly BehaviorSubject<Optional<float>> Subject;

        public Property(TName name, string display)
        {
            Name = name;
            Display = display;
            Subject = new BehaviorSubject<Optional<float>>(default);
        }

        public Optional<float> Value
        {
            get => Subject.Value;
            set
            {
                if (!value.Equals(Value))
                    Subject.OnNext(value);
            }
        }

        object IVLPin.Value { get => Value; set => Value = (Optional<float>)value; }
    }
}
