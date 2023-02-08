using System;

namespace VL.Lib.Reactive
{
    [Flags]
    public enum ChannelSelection
    {
        None = 0,
        ChannelA = 1,
        ChannelB = 2,
        Both = ChannelA | ChannelB
    }
}
