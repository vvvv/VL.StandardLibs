#nullable enable

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLPin<T> : IVLPin
    {
        new T? Value { get; set; }
    }
}
#nullable restore