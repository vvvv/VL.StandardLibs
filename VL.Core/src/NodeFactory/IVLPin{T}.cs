#nullable enable

namespace VL.Core
{
    /// <summary>
    /// Typed variant of <see cref="IVLPin"/>, avoiding boxing for value types.
    /// </summary>
    public interface IVLPin<T> : IVLPin
    {
        /// <summary>
        /// The current value of the pin.
        /// </summary>
        new T? Value { get; set; }
    }
}
#nullable restore