﻿#nullable enable
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace VL.Lib.Reactive
{
    public class ChannelFlange<T> where T: struct
    {
        T value;
        IChannel<T>? channel;

        public ChannelFlange(T initialValue)
        {
            value = initialValue;
        }

        public T Update(IChannel<T>? channel)
        {
            this.channel = channel;
            if (channel.IsValid())
                value = channel.Value;
            return value;
        }

        public T Update(IChannel<T>? channel, out bool hasChanged)
        {
            this.channel = channel;
            hasChanged = CopyFromUpstream();
            return value;
        }

        bool CopyFromUpstream()
        {
            if (channel.IsValid() && !EqualityComparer<T>.Default.Equals(channel.Value, value))
            {
                value = channel.Value;
                return true;
            }
            return false;
        }

        public T Value
        {
            get => value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(value, this.value))
                {
                    this.value = value;
                    if (channel.IsValid())
                        channel.Value = value;
                }
            }
        }
    }
}
#nullable restore