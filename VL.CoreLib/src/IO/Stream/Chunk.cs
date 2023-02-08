using System;

namespace VL.Lib.IO
{
    /// <summary>
    /// A chunk of data part of a larger data stream.
    /// </summary>
    /// <typeparam name="T">The elment type.</typeparam>
    public struct Chunk<T>
    {
        public static readonly Chunk<T> Default = new Chunk<T>(Array.Empty<T>(), 0, 0);

        public static Chunk<T> Create(T[] buffer, int offset, int count, long processedCount, long totalLength)
        {
            if (offset == 0 && count == buffer.Length)
                return new Chunk<T>(buffer, processedCount, totalLength);
            var data = new T[count];
            Array.Copy(buffer, offset, data, 0, count);
            return new Chunk<T>(data, processedCount, totalLength);
        }

        public readonly T[] Data;
        public readonly long ProcessedCount;
        public readonly long TotalLength;

        public Chunk(T[] data, long processedCount, long totalLength)
        {
            Data = data;
            ProcessedCount = processedCount;
            TotalLength = totalLength;
        }

        public float Progress
        {
            get
            {
                if (TotalLength > 0)
                    return (float)((double)ProcessedCount / TotalLength);
                return -1f;
            }
        }
    }
}
