using System.Diagnostics.CodeAnalysis;
using VL.Core;

namespace VL.Lib.Reactive.Monadic
{
    /// <summary>
    /// Part of infrastructure to support connecting <typeparamref name="T"/> to <see cref="Channel{T}"/>
    /// </summary>
    public class ChannelFactory<T> : IMonadicFactory<T, IChannel<T>>
    {
        public static readonly ChannelFactory<T> Default = new ChannelFactory<T>();

        public IMonadBuilder<T, IChannel<T>> GetMonadBuilder(bool isConstant)
        {
            return new ChannelBuilder<T>();
        }

        public IMonadicValueEditor<T, IChannel<T>> GetEditor() => new Editor();

        sealed class Editor : IMonadicValueEditor<T, IChannel<T>>
        {
            public IChannel<T> Create(T value) => Channel.Create(value);

            public T GetValue(IChannel<T> channel) => channel.Value;

            public bool HasValue([NotNullWhen(true)] IChannel<T> channel) => channel.IsValid();

            public IChannel<T> SetValue(IChannel<T> channel, T value)
            {
                channel.EnsureValue(value);
                return channel;
            }
        }
    }
}
