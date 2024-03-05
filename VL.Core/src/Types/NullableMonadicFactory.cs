namespace VL.Core
{
    // System expects this class to exist!
    public sealed class NullableMonadicFactory<T> : IMonadicFactory<T, T?>
        where T : struct
    {
        public static readonly NullableMonadicFactory<T> Default = new NullableMonadicFactory<T>();

        public IMonadBuilder<T, T?> GetMonadBuilder(bool isConstant)
        {
            return Builder.Instance;
        }

        public IMonadicValueEditor<T, T?> GetEditor(T? nullable) => new Editor();

        sealed class Editor : IMonadicValueEditor<T, T?>
        {
            public bool HasValue(T? nullable) => nullable.HasValue;

            public T GetValue(T? nullable) => nullable.Value;

            public T? SetValue(T? nullable, T value)
            {
                return new T?(value);
            }
        }

        sealed class Builder : IMonadBuilder<T, T?>
        {
            public static readonly Builder Instance = new Builder();
            public T? Return(T value) => value;
            public T? Default() => default;
        }
    }
}
