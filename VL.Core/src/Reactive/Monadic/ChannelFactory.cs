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
    }
}
