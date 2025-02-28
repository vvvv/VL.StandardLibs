using Stride.Core.Collections;
using Stride.Input;
using System;

namespace VL.Stride.Input
{
    sealed class EmptyInputSource : IInputSource
    {
        public static readonly EmptyInputSource Instance = new();

        private EmptyInputSource() { }

        TrackingDictionary<Guid, IInputDevice> IInputSource.Devices { get; } = new();

        void IDisposable.Dispose() { }

        void IInputSource.Initialize(InputManager inputManager) { }

        void IInputSource.Pause() { }

        void IInputSource.Resume() { }

        void IInputSource.Scan() { }

        void IInputSource.Update() { }
    }
}
