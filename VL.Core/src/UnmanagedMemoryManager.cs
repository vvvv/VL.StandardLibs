using System;
using System.Buffers;

namespace VL.Core
{
    public unsafe sealed class UnmanagedMemoryManager<T> : MemoryManager<T>
        where T : unmanaged
    {
        private readonly T* pointer;
        private readonly int length;

        public UnmanagedMemoryManager(IntPtr pointer, int lengthInBytes)
            : this((T*)pointer.ToPointer(), lengthInBytes / sizeof(T))
        {
        }

        public UnmanagedMemoryManager(T* pointer, int length)
        {
            this.pointer = pointer;
            this.length = length;
        }

        public override Span<T> GetSpan()
        {
            return new Span<T>(pointer, length);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            if (elementIndex < 0 || elementIndex >= length)
                throw new ArgumentOutOfRangeException(nameof(elementIndex));
            return new MemoryHandle(pointer + elementIndex);
        }

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
