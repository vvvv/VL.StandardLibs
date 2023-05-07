using System;
using VL.Lib.Reactive;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;

#nullable enable

namespace VL.Core.Reactive
{
    public class ChannelHub : IChannelHub, IDisposable
    {
        int lockCount = 0;
        int revision = 0;
        int revisionOnLockTaken = 0;

        public IChannel<object> OnChannelsChanged { get; }

        public ChannelHub()
        {
            OnChannelsChanged = new Channel<object>();
            OnChannelsChanged.Value = this;
        }

        IDisposable? MustHaveDescriptiveSubscription;
        public IObservable<IEnumerable<ChannelBuildDescription>> MustHaveDescriptive
        {
            set
            {
                MustHaveDescriptiveSubscription?.Dispose();
                MustHaveDescriptiveSubscription = value.Subscribe(descriptions =>
                {
                    ((IChannelHub)this).BatchUpdate(_ =>
                    {
                        // make sure all channels of the descriptive configuration exist.
                        // we don't delete channels that are not listed as the user might have added some more programmatically.
                        // the config only describes those that shall be there on startup.
                        foreach (var d in descriptions)
                        {
                            var name = d.Name;
                            var type = d.FetchType;
                            TryAddChannel(name, type);
                        }
                    });
                });
            }
        }

        internal ConcurrentDictionary<string, IChannel<object>> Channels = new();
        IDictionary<string, IChannel<object>> IChannelHub.Channels => Channels;

        public IDisposable BeginChange()
        {
            if (lockCount == 0)
                revisionOnLockTaken = revision;
            lockCount++;
            return Disposable.Create(EndChange);
        }

        void EndChange()
        {
            lockCount--;
            if (lockCount == 0 && revisionOnLockTaken != revision)
                OnChannelsChanged.Value = this;
        }

        public IChannel<object>? TryAddChannel(string key, Type typeOfValues)
        {
            using var _ = BeginChange();
            var c = Channels.GetOrAdd(key, _ => 
            { 
                var c = ChannelHelpers.CreateChannelOfType(typeOfValues); 
                revision++; 
                return c; 
            });
            if (c.ClrTypeOfValues != typeOfValues)
                return default;
            // discuss if replacing with new type is an option or should always occur.
            // for now never replacing. User of API shall call TryChangeType for now.
            return c;
        }

        public IChannel<object>? TryGetChannel(string key)
        {
            Channels.TryGetValue(key, out var c);
            return c;
        }

        public bool TryRemoveChannel(string key)
        {
            using var _ = BeginChange();
            var gotRemoved = Channels.TryRemove(key, out var c);
            if (c != null)
            {
                revision++;
                //c.Dispose();// might not really be necessary, but let's clean up for now. We are at least the ones who created the channels.
            }
            return gotRemoved;
        }

        public IChannel<object>? TryRenameChannel(string key, string newKey)
        {
            using var _ = BeginChange();
            var gotRemoved = Channels.TryRemove(key, out var c);
            if (c != null)
            {
                var o = c.Object;
                revision++;
                //c.Dispose();
                c = TryAddChannel(newKey, c.ClrTypeOfValues);
                if (c != null && o != null && c.ClrTypeOfValues.IsAssignableFrom(o.GetType()))
                    c.Object = o;
                return c;
            }
            return null;
        }

        public IChannel<object>? TryChangeType(string key, Type typeOfValues)
        {
            using var _ = BeginChange();
            var gotRemoved = Channels.TryRemove(key, out var c);
            if (c != null)
            {
                var o = c.Object;
                revision++;
                //c.Dispose();
                c = TryAddChannel(key, typeOfValues);
                if (c != null && o != null && typeOfValues.IsAssignableFrom(o.GetType()))
                    c.Object = o;
                return c;
            }
            return null;
        }

        public void Dispose()
        {
            {
                using var _ = BeginChange();
                var cs = Channels.Values;
                Channels.Clear();
                revision++;
                foreach (var c in cs)
                    c.Dispose();
            }
            OnChannelsChanged.Dispose();
            MustHaveDescriptiveSubscription?.Dispose();
        }
    
        public static IChannelHub HubForApp 
        { 
            get 
            { 
                return ServiceRegistry.Current.GetOrAddService<IChannelHub>(() =>
                {
                    var x = new ChannelHub();
                    ServiceRegistry.Current.GetService<IAppHost>().OnExit.Subscribe(_ =>
                    {
                        x.Dispose();
                    });
                    return x;
                });
            }
        }
    }
}

#nullable restore