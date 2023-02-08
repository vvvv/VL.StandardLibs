using System;
using System.Runtime.CompilerServices;

namespace VL.Core
{
    // Idea taken from discussion and design proposal at https://github.com/dotnet/corefx/issues/21281 and https://github.com/dotnet/corefx/issues/22404
    /// <summary>
    /// Allows to get access to the backing <see cref="ReadOnlyMemory{T}"/> or <see cref="Memory{T}"/> of the implementing class.
    /// </summary>
    /// <remarks>
    /// Currently implemented by <see cref="VL.Lib.Collections.Spread{T}" /> and <see cref="VL.Lib.Collections.SpreadBuilder{T}"/>
    /// </remarks>
    /// <typeparam name="T">The elment type.</typeparam>
    public interface IHasMemory<T>
    {
        /// <summary>
        /// Tries to get access on the backing continuous <see cref="Memory{T}"/>.
        /// </summary>
        /// <param name="memory">The continuous block of <see cref="Memory{T}"/>.</param>
        /// <returns>Whether or not access to the <see cref="Memory{T}"/> is granted.</returns>
        bool TryGetMemory(out Memory<T> memory);

        /// <summary>
        /// Tries to get access on the backing continuous <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        /// <param name="memory">The continuous block of <see cref="ReadOnlyMemory{T}"/>.</param>
        /// <returns>Whether or not access to the <see cref="ReadOnlyMemory{T}"/> is granted.</returns>
        bool TryGetMemory(out ReadOnlyMemory<T> memory);
    }

    public static class HasMemoryExtensions
    {
        /// <summary>
        /// Returns empty memory.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>The empty memory.</returns>
        public static IHasMemory<T> Empty<T>() => EmptyHasMemoryImpl<T>.Default;

        /// <summary>
        /// Wether or not not-empty <see cref="ReadOnlyMemory{T}"/> is available.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="instance">The instance providing the memory.</param>
        /// <returns>True if not-empty <see cref="ReadOnlyMemory{T}"/> is available.</returns>
        public static bool HasData<T>(this IHasMemory<T> instance) => instance.TryGetMemory(out ReadOnlyMemory<T> memory) && !memory.IsEmpty;

        /// <summary>
        /// The length in elements of the memory block.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="instance">The instance providing the memory.</param>
        /// <returns>The length of the memory block.</returns>
        public static int Length<T>(this IHasMemory<T> instance) => instance.TryGetMemory(out ReadOnlyMemory<T> memory) ? memory.Length : 0;

        /// <summary>
        /// The size in bytes of the memory block.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="instance">The instance providing the memory.</param>
        /// <returns>The size in bytes of the memory block.</returns>
        public static long Size<T>(this IHasMemory<T> instance) => instance.Length() * Unsafe.SizeOf<T>();

        /// <summary>
        /// Returns the size in bytes of one element of the memory.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="instance">The instance providing the memory.</param>
        /// <returns>The size of one element in bytes.</returns>
        public static int ElementSize<T>(this IHasMemory<T> instance) => Unsafe.SizeOf<T>();

        class EmptyHasMemoryImpl<T> : IHasMemory<T>
        {
            public static readonly EmptyHasMemoryImpl<T> Default = new EmptyHasMemoryImpl<T>();

            public bool TryGetMemory(out Memory<T> memory)
            {
                memory = Memory<T>.Empty;
                return true;
            }

            public bool TryGetMemory(out ReadOnlyMemory<T> memory)
            {
                memory = ReadOnlyMemory<T>.Empty;
                return true;
            }

            public override string ToString() => "Empty Memory";
        }
    }
}
