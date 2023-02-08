#nullable enable
using System;

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLPinDescription
    {
        string Name { get; }
        Type Type { get; }
        object? DefaultValue { get; }
    }
}
#nullable restore