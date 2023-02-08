using System.Runtime.CompilerServices;

namespace Stride.Core.Collections
{
    public static class CollectionExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ElementAtOrDefault<T>(this FastList<T> list, int index) => 
            index < list.Count ? list.Items[index] : default(T);
    }
}
