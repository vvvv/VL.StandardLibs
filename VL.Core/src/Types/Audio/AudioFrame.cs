#nullable enable
using CommunityToolkit.HighPerformance;
using System;

namespace VL.Lib.Basics.Audio
{
    public record class AudioFrame(ReadOnlyMemory2D<float> Data, int SampleRate, bool IsInterleaved = false, string? Metadata = null, TimeSpan Timecode = default)
    {
        public static readonly AudioFrame Empty = new AudioFrame(new float[0, 0].AsMemory2D(), 0);

        public bool IsPlanar => !IsInterleaved;

        public int ChannelCount => IsPlanar ? Data.Height : Data.Width;

        public int SampleCount => IsPlanar ? Data.Width : Data.Height;

        public ReadOnlySpan<float> GetChannel(int index) => IsPlanar ? Data.Span.GetRowSpan(index) : default;

        public ReadOnlySpan<float> GetSamples(int index) => IsInterleaved ? Data.Span.GetRowSpan(index) : default;

        public void CopyChannelTo(int index, Span<float> destination)
        {
            if (IsPlanar)
            {
                GetChannel(index).CopyTo(destination);
            }
            else
            {
                var span = Data.Span;
                var column = span.Slice(row: 0, column: index, height: span.Height, width: 1);
                column.CopyTo(destination);
            }
        }

        public void CopySamplesTo(int index, Span<float> destination)
        {
            if (IsInterleaved)
            {
                GetSamples(index).CopyTo(destination);
            }
            else
            {
                var span = Data.Span;
                var column = span.Slice(row: 0, column: index, height: span.Height, width: 1);
                column.CopyTo(destination);
            }
        }
    }
}
#nullable restore