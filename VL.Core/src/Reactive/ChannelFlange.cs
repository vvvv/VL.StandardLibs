#nullable enable
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace VL.Lib.Reactive
{
    public class ChannelFlange<T> : IDisposable
        where T: struct
    {
        private readonly SerialDisposable subscription = new();

        T value;
        T lastRetrievedValue;
        IChannel<T>? channel;

        public ChannelFlange(T initialValue)
        {
            value = initialValue;
        }

        public T Update(IChannel<T>? channel)
        {
            if (channel != this.channel)
            {
                this.channel = channel;

                if (channel.IsValid())
                    value = channel.Value;

                subscription.Disposable = channel?.Subscribe(v => value = v);
            }
            return value;
        }

        public T Update(IChannel<T>? channel, out bool hasChanged)
        {
            Update(channel);
            hasChanged = !EqualityComparer<T>.Default.Equals(lastRetrievedValue, value);
            return lastRetrievedValue = value;
        }

        public void Dispose()
        {
            subscription.Dispose();
        }

        public T Value
        {
            get => value;
            set
            {
                lastRetrievedValue = value;
                if (!EqualityComparer<T>.Default.Equals(value, this.value))
                {
                    this.value = value;
                    // Do not write into system generated channels
                    if (channel is not null && !channel.IsSystemGenerated())
                        channel.Value = value;
                }
            }
        }
    }
}
#nullable restore