#nullable enable
using System.Collections.Immutable;

namespace VL.Core
{
    /// <summary>
    /// Implement this interface to provide tags the node browser can use to find stuff.
    /// </summary>
    public interface ITaggedInfo : IInfo
    {
        /// <summary>
        /// For being able to find the entity.
        /// </summary>
        ImmutableArray<string> Tags { get; }
    }
}
#nullable restore