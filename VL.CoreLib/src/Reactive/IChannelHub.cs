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

    public interface IModuleView
    {
        //IPlainProcessNode CreateAddBindingDialog(string channelPath, IChannel channel, IChannel<Action> responeChannel, IBinding? initialBinding, Vector2 expectedSize);
    }


    public interface IPlainProcessNode
    {
        void Update();
    }








    //public interface IChannelDescriptionProvider
    //{
    //    IObservable<IEnumerable<ChannelDescription>> DescribeNeeds();
    //}

}

#nullable restore