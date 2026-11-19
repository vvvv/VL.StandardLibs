#nullable enable

namespace VL.Core
{
    /// <summary>
    /// An input or output pin of an <see cref="IVLNode"/> instance. See <see cref="IVLPin{T}"/> for the typed variant.
    /// </summary>
    public interface IVLPin
    {
        /// <summary>
        /// The current value of the pin. For input pins the runtime writes it before <see cref="IVLNode.Update"/> gets called, for output pins the node sets it during the update.
        /// </summary>
        object? Value { get; set; }
    }
}
#nullable restore