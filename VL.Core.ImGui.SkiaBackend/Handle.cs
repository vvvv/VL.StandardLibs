using System.Runtime.InteropServices;

namespace VL.Core.ImGui.SkiaBackend
{
    readonly struct Handle<T> : IDisposable
        where T : class
    {
        private readonly GCHandle _handle;

        public Handle(T skiaObject)
        {
            _handle = GCHandle.Alloc(skiaObject);
        }

        public T? Target => _handle.Target as T;

        public IntPtr Ptr => GCHandle.ToIntPtr(_handle);

        public void Dispose()
        {
            _handle.Free();
        }
    }
}
