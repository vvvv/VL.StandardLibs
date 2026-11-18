#nullable enable
using System;

namespace VL.Core
{
    /// <summary>
    /// A running instance of a node, created by <see cref="IVLNodeDescription.CreateInstance"/>.
    /// Pins are exchanged through <see cref="Inputs"/> and <see cref="Outputs"/>: the runtime writes the input pin values and
    /// calls <see cref="Update"/> once per frame (or whenever the node needs to be evaluated), then reads the output pin values.
    /// The node is disposed when it gets removed from the patch.
    /// </summary>
    public interface IVLNode : IVLObject, IDisposable
    {
        /// <summary>
        /// The description this node was created from.
        /// </summary>
        IVLNodeDescription NodeDescription { get; }
        /// <summary>
        /// The input pins, in the order of <see cref="IVLNodeDescription.Inputs"/>.
        /// </summary>
        IVLPin[] Inputs { get; }
        /// <summary>
        /// The output pins, in the order of <see cref="IVLNodeDescription.Outputs"/>.
        /// </summary>
        IVLPin[] Outputs { get; }

        /// <summary>
        /// Evaluates the node: reads the current input pin values and updates the output pin values.
        /// </summary>
        void Update();
    }
}
#nullable restore