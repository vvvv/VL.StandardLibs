#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace VL.Core
{

    public static class InfoUtils
    {
        private static char[] separators = new char[] { ' ', ',' };

        public static ImmutableArray<string> ParseTags(string? tags) => ParseTags(tags, ImmutableArray<string>.Empty);

        public static ImmutableArray<string> ParseTags(string? tags, ImmutableArray<string> fallback)
        {
            if (string.IsNullOrWhiteSpace(tags))
                return fallback;

            return tags.Split(separators, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        }

        public static ITaggedInfo CreateInfo(Func<string>? getSummary = default, Func<string>? getRemarks = default, Func<ImmutableArray<string>>? getTags = default)
        {
            return new TaggedInfo(getSummary, getRemarks, getTags);
        }

        sealed class TaggedInfo : ITaggedInfo
        {
            private readonly Lazy<string>? summary, remarks;
            private readonly Lazy<ImmutableArray<string>>? tags;

            public TaggedInfo(Func<string>? summary, Func<string>? remarks, Func<ImmutableArray<string>>? tags)
            {
                this.summary = summary != null ? new Lazy<string>(summary) : default;
                this.remarks = remarks != null ? new Lazy<string>(remarks) : default;
                this.tags = tags != null ? new Lazy<ImmutableArray<string>>(tags) : default;
            }

            public ImmutableArray<string> Tags => tags?.Value ?? ImmutableArray<string>.Empty;

            public string? Summary => summary?.Value;

            public string? Remarks => remarks?.Value;
        }
    }
}
#nullable restore