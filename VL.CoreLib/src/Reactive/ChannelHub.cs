using System;
using VL.Lib.Reactive;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Linq;

#nullable enable

namespace VL.Core.Reactive
{
    internal class ChannelHub : IChannelHub, IDisposable
    {
        int lockCount = 0;
        int revision = 0;
        int revisionOnLockTaken = 0;

        public IChannel<object> OnChannelsChanged { get; }

        readonly IDisposable? OnSwapSubscription;

        public ChannelHub(AppHost appHost)
        {
            AppHost = appHost;

            OnChannelsChanged = new Channel<object>();
            OnChannelsChanged.Value = this;

            var e = appHost.Services.GetService<IHotSwappableEntryPoint>();
            if (e != null)
            {
                OnSwapSubscription = e.OnSwap.Subscribe(_ => Swap(e));
            }
        }

        public AppHost AppHost { get; }

        public override string? ToString() => AppHost.AppBasePath;

        IDisposable? MustHaveDescriptiveSubscription;
        public Channel<PublicChannelDescription[]> MustHaveDescriptive
        {
            set
            {
                MustHaveDescriptiveSubscription?.Dispose();
                recreateChannels(value.Value);
                MustHaveDescriptiveSubscription = ((IObservable<PublicChannelDescription[]>)value).Subscribe(recreateChannels);
            }
        }

        void recreateChannels(PublicChannelDescription[] descriptions)
        {
            ((IChannelHub)this).BatchUpdate(_ =>
            {
                // make sure all channels of the descriptive configuration exist.
                // we don't delete channels that are not listed as the user might have added some more programmatically.
                // the config only describes those that shall be there on startup.
                foreach (var d in descriptions)
                {
                    var name = d.Name;
                    var type = d.GetRuntimeType(AppHost.TypeRegistry);
                    TryAddChannel(name, type);
                }
            });
        }


        internal ConcurrentDictionary<string, IChannel<object>> Channels = new();
        internal ConcurrentDictionary<string, IChannel<object>> AnonymousChannels = new();

        internal ConcurrentDictionary<IModule, IModule> Modules = new();


        IDictionary<string, IChannel<object>> IChannelHub.Channels => Channels;

        IEnumerable<IModule> IChannelHub.Modules => Modules.Keys.OrderBy(m => m.Name);


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
            if (string.IsNullOrWhiteSpace(key))
                return default;

            using var _ = BeginChange();
            var c = Channels.GetOrAdd(key, _ => 
            { 
                var c = ChannelHelpers.CreateChannelOfType(typeOfValues); 
                ((IInternalChannel)c).SetPath(key);
                if (!c.IsAnonymous()) revision++;

                //var typeInfo = AppHost.TypeRegistry.GetTypeInfo(typeOfValues);
                //if (typeInfo != null && !typeInfo.IsPatched)
                //    c.Value = AppHost.TypeRegistry.GetTypeInfo(typeOfValues).GetDefaultValue();

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
            if (c is IInternalChannel ic)
                ic.Request(); // mark channel as asked for, so that it can be used in the UI
            return c;
        }

        public IChannel<object>? TryAddAnonymousChannel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return default;

            var c = AnonymousChannels.GetOrAdd(key, _ =>
            {
                var c = ChannelHelpers.CreateChannelOfType(typeof(object));
                ((IInternalChannel)c).SetPath(key);
                return c;
            });

            return c;
        }

        public IChannel<object>? TryGetAnonymousChannel(string key)
        {
            AnonymousChannels.TryGetValue(key, out var c);
            return c;
        }

        public void RemoveAnonymousChannel(string key)
        {
            AnonymousChannels.TryRemove(key, out var c);
        }

        public bool TryRemoveChannel(string key)
        {
            using var _ = BeginChange();
            var gotRemoved = Channels.TryRemove(key, out var c);
            if (c != null)
            {
                if (!c.IsAnonymous()) revision++;
                c.Dispose();
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
                if (!c.IsAnonymous()) revision++;
                c.Dispose();
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
                if (!c.IsAnonymous()) revision++;
                c.Dispose();
                c = TryAddChannel(key, typeOfValues);
                if (c != null && o != null && typeOfValues.IsAssignableFrom(o.GetType()))
                    c.Object = o;
                return c;
            }
            return null;
        }




        public void RegisterModule(IModule module)
        {
            Modules.TryAdd(module, module);
        }

        public void UnregisterModule(IModule module)
        {
            Modules.TryRemove(new KeyValuePair<IModule, IModule>(module, module));
        }



        public void Dispose()
        {
            {
                //using var _ = BeginChange(); //don't report
                revision++;
                foreach (var c in Channels.Values)
                    c.Dispose();
                Channels.Clear();
            }
            OnChannelsChanged.Dispose();
            MustHaveDescriptiveSubscription?.Dispose();
            OnSwapSubscription?.Dispose();
        }

        private void Swap(IHotSwappableEntryPoint entryPoint)
        {
            bool changed = false;
            var keys = new List<string>(Channels.Keys);
            var channels = Channels;
            foreach (string key in keys)
            {
                var channel = channels[key];
                var value = channel.Value;
                var newValue = entryPoint.Swap(value, typeof(object));
                if (newValue != value)
                {
                    var newElementType = entryPoint.SwapType(channel.ClrTypeOfValues);
                    var newChannel = (IChannel)entryPoint.Swap(channel, typeof(Channel<>).MakeGenericType(newElementType));
                    if (channel != newChannel)
                    {
                        channels[key] = (IChannel<object>)newChannel;
                        changed = true;
                    }
                    
                    newChannel.SetObjectAndAuthor(newValue, "hotswap");
                }
            }
            if (changed)
            {
                OnChannelsChanged.Value = this;
            }
        }
    }
}

#nullable restore