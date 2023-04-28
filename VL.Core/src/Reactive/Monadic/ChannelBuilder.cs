using System;
using System.Collections.Generic;
using VL.Core;

#nullable enable
namespace VL.Lib.Reactive.Monadic
{
    /// <summary>
    /// Part of infrastructure to support connecting <typeparamref name="T"/> to <see cref="Channel{T}"/>
    /// </summary>
    public class ChannelBuilder<T> : IMonadBuilder<T, IChannel<T>>, IDisposable
    {
        private readonly IChannel<T> channel = new Channel<T>();
        private T? lastValue;

        public IChannel<T> Return(T value)
        {
            // Changed check before writing into the channel
            if (!EqualityComparer<T>.Default.Equals(value, lastValue))
            {
                lastValue = value;
                channel.Value = value;
            }
            return channel;
        }

        public void Dispose()
        {
            channel.Dispose();
        }
    }
}
#nullable restore