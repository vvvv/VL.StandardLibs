using System.Runtime.CompilerServices;

namespace VL.Stride
{
    public static class Interop
    {
        /// <summary>
        /// Returns the size of the object in bytes.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="object">The object.</param>
        /// <returns>The size of the object in bytes.</returns>
        public static int SizeOf<T>(T @object) => Unsafe.SizeOf<T>();
    }
}
