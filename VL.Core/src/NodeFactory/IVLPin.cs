#nullable enable

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLPin
    {
        object? Value { get; set; }
    }
}
#nullable restore