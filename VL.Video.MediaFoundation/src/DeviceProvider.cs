using SharpDX.Direct3D11;
using System;

namespace VL.Video.MediaFoundation
{
    /// <summary>
    /// Provides access to the <see cref="SharpDX.Direct3D11.Device"/> used to initialize media foundation services.
    /// </summary>
    public abstract class DeviceProvider : IDisposable
    {
        public abstract Device Device { get; }

        public abstract bool UsesLinearColorspace { get; }

        public abstract void Dispose();
    }
}
