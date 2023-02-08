#nullable enable
using System;

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

        void Update();
    }
}
#nullable restore