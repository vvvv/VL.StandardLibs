using System;
using VL.Core;
using System.Collections.Concurrent;
using VL.Lib.Reactive;
using System.Reactive.Subjects;

namespace VL.Core.Reactive
{
    public class ChannelHub
    {
        public static ConcurrentDictionary<string, Channel> Channels = new ConcurrentDictionary<string, Channel>();

        public static IObservable<object> OnChannelsChanged = new Subject<object>();

        //public static void SetChannel
    }
}