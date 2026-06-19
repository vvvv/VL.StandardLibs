using System;
using VL.Lib.Collections;

namespace VL.IO.Redis;

/// <summary>
/// Options for client-side caching in Redis.
/// </summary>
/// <param name="UseBroadcastMode">Indicates whether to use broadcast mode for client-side caching.</param>
/// <param name="BroadcastPrefixes">The prefixes we're interested.</param>
internal record struct ClientSideCachingOptions(bool UseBroadcastMode, Spread<string> BroadcastPrefixes)
{
    public static readonly ClientSideCachingOptions Default = new(true, Spread.Create<string>());

    public bool Equals(ClientSideCachingOptions other)
    {
        return UseBroadcastMode == other.UseBroadcastMode &&
               BroadcastPrefixes.SequenceEqual(other.BroadcastPrefixes);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(UseBroadcastMode);
        foreach (var prefix in BroadcastPrefixes)
            hash.Add(prefix);
        return hash.ToHashCode();
    }
}
