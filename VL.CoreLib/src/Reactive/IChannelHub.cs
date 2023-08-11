using System;
using VL.Lib.Reactive;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using Stride.Core.Mathematics;

#nullable enable

namespace VL.Core.Reactive
{
    /// <summary>
    /// A ChannelHub manages the lifetime of channels.
    /// </summary>
    /*
     * Channels & Bindings:
     * The user shall be able to edit global channels manually in an editor extension window.
     * It shall be possible to remove channels, even if some come with a couple of bindings. Those should get deleted as well.
     *     Bindings could get stored as components attached to a channel.
     *     A Redis app component would only store which channels to sync (their names e.g. via a regular expression query and look 
     *     up the Redis bindings in the found channels in the ChannelHub).
     * 
     * Entry points:
     * A user wants to think per entry point. 
     * Reasoning: 
     *     Opening help patches shall not mess up your project entry point; 
     *     After closing a patch the dev session should be as clean as possible..
     * Per entry point we have one channel hub that represents all global channels for the app. 
     * There still could be a way to opt-into a global channel behavior that binds channels of different entry points:
     *     a certain app component that bridges one entry-point with the other (merges their ChannelHubs on localhost).
     * 
     * IDE & imperative API:
     * Editor extension windows (for editing global channels and their bindings) show the global channels
     * for one entry point - which could be synced to what patch we currently look at. There could be different
     * such windows that all want to help the user from different angels (MIDI specific view, overview over 
     * all channels and their bindings...). 
     *     In order to implement those windows the channel hub has an imperative way to add and remove or modify channels. 
     * 
     * Descriptive API:
     * An alternative design is to have channel description providers, which can register and tell of their needs via
     * something like a IObservable<IEnumerable<ChannelDescription>>. The distinct, merged needs of all channel 
     * providers would determine which channels exist. This could be added later. 
     *     All those channels could be locked; they wouldn't be editable by the user, since those are needed by some app component.
     *     When adding an IOBoxSyncAppComponent this could be thought of as a collection of channeldescriptions, resulting in locked channels
     * 
     * vvvv Editor ChannelHub:
     * The vvvv editor runtime can be seen as yet another app, which comes with its own channels hub.
     * A boygroup app component e.g. would be part of the vvvv IDE, which shall work even when in paused or stopped mode.
     * Such a boygrouping app component would setup a Redis binding for some central channels inside the editor channel hub.
     *     A channel Boygrouping/DocDiffs is not visible to the user as it is part of the IDE channelhub, not an entry-point channelhub.
     * 
     * Usage & Storage:
     * For the user app the user shall place a GlobalChannels node somewhere. 
     * That's the place where to store the channel descriptions in a pin. 
     * This way we don't need to invent config files or hidden areas in a vl document to store those settings. 
     * The user is boss where to place this central node and how to reference the file in question.
     * Ideally the patcher would only get access to the global channels when there is a reference to the document that 
     * has setup those channels. 
     * 
     * Accessing channels:
     * Access to channels can be granted by statically typed nodes - named after the channel - and by dynamic lookups.
     * Whenever the ChannelHub for an entry point changed its entries new nodes get built by a factory accordingly. 
     * BeginChange can be used to group changes in bulks, so that the event OnChannelsChanged triggers less often.
     * The ChannelHub implementation takes care of creating the nodes.
     * 
     * Implementation:
     * The GlobalChannels node would aquire the IChannelHub app component per app via IAppHost.GetComponent;
     * And then take control over it, representing the single source of truth for the channels for this app.
     * App Components (e.g. Redis) are managed by the apphost. It disposes all app components on Stop.
     * They can and should make use of this moment and leave a clean state. E.g. remove certain channels from a Redis database.
     */
    public interface IChannelHub
    {
        public static IChannelHub HubForApp
        {
            get
            {
                return AppHost.Current.Services.GetService<IChannelHub>()!;
            }
        }

        AppHost AppHost { get; }

        IDictionary<string, IChannel<object>> Channels { get; }

        IEnumerable<IModule> Modules { get; }

        IChannel<object>? TryGetChannel(string key);
        
        IChannel<object>? TryAddChannel(string key, Type typeOfValues);

        bool TryRemoveChannel(string key);

        IChannel<object>? TryRenameChannel(string key, string newKey);

        IChannel<object>? TryChangeType(string key, Type typeOfValues);        

        /// <summary>
        /// Do several changes to the ChannelHub in one go. using BeginChange()
        /// </summary>
        /// <returns></returns>
        IDisposable BeginChange();

        IChannel<object> OnChannelsChanged { get; }

        void BatchUpdate(Action<IChannelHub> action)
        {
            using var _ = BeginChange();
            action(this);
        }

        /// <summary>
        /// Please call this inside a static RegisterServices operation
        /// </summary>
        /// <param name="module"></param>
        void RegisterModule(IModule module);
    }


    public interface IModule
    {
        string Name { get; }

        string Description { get; }

        bool SupportsType(Type type);
    }

    public struct ChannelHubFolder
    {
    }


    [Flags]
    public enum BindingUserEditingCapabilities
    {
        Default = OnlyAllowSingle | Editable | ManuallyRemovable,
        NoManualAdd = 1 << 0,
        OnlyAllowSingle = 1 << 1,
        AllowMultiple = 1 << 2,
        //SpecifyAllowAddPerChannel = 1 << 3,

        Editable = 1 << 4,
        //EditMeansMutate = 1 << 5,
        //EditMeansRecreate = 1 << 6,
        //SpecifyAllowEditPerChannel = 1 << 7,

        ManuallyRemovable = 1 << 8,
        //SpecifyAllowRemovablePerChannel = 1 << 9,
    }


    public interface IModuleView
    {
        BindingUserEditingCapabilities? BindingEditingCapabilities { get; }

        IPlainProcessNode CreateAddBindingDialog(string channelPath, IChannel channel, IChannel<Action> responeChannel, IBinding? initialBinding, Vector2 expectedSize);

        void RemoveBinding(IBinding binding);
    }


    public interface IPlainProcessNode
    {
        void Update();
    }


    public enum BindingType
    {
        None = 0,
        Send = 1,
        Receive = 2,
        SendAndReceive = Send | Receive,
    }


    public interface IBinding : IDisposable
    {
        IModule Module { get; }

        string ShortLabel => Module.Name;

        string? Description { get; }

        BindingType BindingType { get; }
    }








    //public interface IChannelDescriptionProvider
    //{
    //    IObservable<IEnumerable<ChannelDescription>> DescribeNeeds();
    //}

}

#nullable restore