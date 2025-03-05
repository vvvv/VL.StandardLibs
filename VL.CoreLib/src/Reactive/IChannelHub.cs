using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Threading;
using VL.Lib.Reactive;

#nullable enable

namespace VL.Core.Reactive
{
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
        IModule? Module { get; }

        string ShortLabel => Module?.Name ?? GetType().Name;

        string? Description { get; }

        BindingType BindingType { get; }

        bool GotCreatedViaNode => Module == null;
    }





    //public interface IChannelDescriptionProvider
    //{
    //    IObservable<IEnumerable<ChannelDescription>> DescribeNeeds();
    //}

}

#nullable restore