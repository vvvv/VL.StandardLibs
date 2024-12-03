#nullable enable
using System;
using VL.Model;

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
        PinGroupKind PinGroupKind => PinGroupKind.None;
        int PinGroupDefaultCount => 0;
    }
}
#nullable restore