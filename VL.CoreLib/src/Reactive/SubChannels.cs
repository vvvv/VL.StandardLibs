using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core.Reactive;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lang;
using Microsoft.Extensions.Logging;

namespace VL.Lib.Reactive
{
    record class UpLink(IDisposable Disposable) : IDisposable
    {
        public void Dispose()
        {
            Disposable.Dispose();
        }
    };

    public static class SubChannelHelpers
    {
        internal static void addWarnings(this CompositeDisposable disposables, Func<string> getWarning, NodeContext nodeContext)
        {
            var stack = nodeContext.Path.Stack.Pop(out var id);
            var warning = getWarning();
            IVLRuntime.Current?.AddPersistentMessage(new Message(id, MessageSeverity.Warning, warning))
                .DisposeBy(disposables);
            while (!stack.IsEmpty)
            {
                stack = stack.Pop(out id);
                IVLRuntime.Current?.AddPersistentMessage(new Message(id, MessageSeverity.Warning, warning))
                    .DisposeBy(disposables);
            }
        }

        /// <summary>
        /// suitable for when Update calls refresh the warnings constantly
        /// </summary>
        internal static void warnForAWhile(this NodeContext nodeContext, Func<string> getWarning)
        {
            ResourceProvider.NewPooledSystemWide(nodeContext.Path,
                _ =>
                {
                    var messages = new CompositeDisposable();
                    messages.addWarnings(getWarning, nodeContext);
                    return messages;
                }, delayDisposalInMilliseconds: 3000)
                .GetHandle()
                .Dispose(); // messages will stick for some seconds
        }

        static IEnumerable<ObjectGraphNode> yieldPathToNode(ObjectGraphNode node)
        {
            if (node.Parent != null)
                foreach (var n in yieldPathToNode(node.Parent))
                    yield return n;
            yield return node;
        }

        public static void GetOrAddSubChannel<A, B>(this IChannel<A> main, string relativePath,
            NodeContext nodeContext, out IChannel<B> subChannel, out IDisposable handle)
            where A : class
        {
            subChannel = DummyChannel<B>.Instance;
            var disposables = new CompositeDisposable();
            handle = disposables;

            if (main == null || !main.IsValid())
            {
                disposables.addWarnings(() => "Main channel not set.", nodeContext);
                return;
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                disposables.addWarnings(() => "Path not set.", nodeContext);
                return;
            }

            if (string.IsNullOrWhiteSpace(main.Path))
            {
                disposables.addWarnings(() => "Select (ByPath) only supported on shared channels.", nodeContext);
                return;
            }

            var node = main.Object.TryGetObjectGraphNodeByPath(relativePath);

            if (!node.HasValue)
            {
                disposables.addWarnings(() => $"Couldn't resolve path {relativePath}.", nodeContext);
                return;
            }

            var channelHub = AppHost.Current.Services.GetRequiredService<IChannelHub>();
            var parent = main as IChannel<object>;
            IChannel<object> channel = null;
            var handleOnSomeSyncs = new CompositeDisposable();

            foreach (var n in yieldPathToNode(node.Value).Skip(1))
            {
                var globalPath = main.Path + n.Path;
                channel = channelHub.TryGetChannel(globalPath);
                if (channel == null)
                {
                    channel = channelHub.TryAddChannel(globalPath, typeof(object));
                    ChannelHelpers.InitSubChannel(channel, n);
                }

                IResourceProvider<UpLink> subSync = GetUpLinkProvider(parent, channel, n);

                handleOnSomeSyncs.Add(subSync.GetHandle()); 
                parent = channel;
            }

            subChannel = new ChannelView<B>(channel);
        }

        private static IResourceProvider<UpLink> GetUpLinkProvider(IChannel<object> parent, IChannel<object> channel, ObjectGraphNode n)
        {
            IResourceProvider<UpLink> provider = null;

            provider = channel.EnsureSingleComponentOfType(() =>
                ResourceProvider.New(
                    () => CreateUpLink(parent, channel, n.AccessedViaKeyPath),
                    _ => channel.RemoveComponent(provider))
                .ShareInParallel());

            return provider;
        }

        private static UpLink CreateUpLink<A, B>(IChannel<A> main, IChannel<B> sub, string relativePath)
            where A : class
        {
            static void takeFromMainAndEnsureOnSub(IChannel<A> main, IChannel<B> sub, string relativePath, string author)
            {
                if (main.Value.TryGetValueByPath(relativePath, default, out B value, out var pathExists))
                    sub.EnsureValue(value, author: author);
                else
                    if (pathExists)
                        AppHost.CurrentDefaultLogger.LogWarning($"SubChannel: Data is not of type {typeof(B).FullName}. Path {relativePath}.");
                    else
                        AppHost.CurrentDefaultLogger.LogWarning($"SubChannel: Path not found {relativePath}.");
            }


            if (!main.IsValid() || !sub.IsValid())
                return new UpLink(Disposable.Empty);

            var subscription = new CompositeDisposable();

            takeFromMainAndEnsureOnSub(main, sub, relativePath, "SubChannelSyncer.Init");

            var isBusy = false;
            subscription.Add(main.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        takeFromMainAndEnsureOnSub(main, sub, relativePath, "SubChannelSyncer.FromParent");
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));
            subscription.Add(sub.Subscribe(v =>
            {
                if (!isBusy)
                {
                    isBusy = true;
                    try
                    {
                        var newBig = main.Value.WithValueByPath(relativePath, v, out var pathExists);

                        if (pathExists)
                            main.SetValueAndAuthor(newBig, author: "SubChannelSyncer.FromChild");
                        else
                            AppHost.CurrentDefaultLogger.LogWarning($"SubChannel: Couldn't write to parent via {relativePath}.");
                    }
                    finally
                    {
                        isBusy = false;
                    }
                }
            }));

            return new UpLink(subscription);


        }
    }
}