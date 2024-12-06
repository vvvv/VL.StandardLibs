#nullable enable
using System;
using VL.Lib.Reactive;

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLNode : IVLObject, IDisposable
    {
        IVLNodeDescription NodeDescription { get; }
        IVLPin[] Inputs { get; }
        IVLPin[] Outputs { get; }

        /// <summary>
        /// Optional channel to express that the node is capable of "learning" from user input.
        /// If set the node inspector will show a learn button.
        /// </summary>
        IChannel<bool>? LearnMode => null;

        void Update();
    }
}
#nullable restore