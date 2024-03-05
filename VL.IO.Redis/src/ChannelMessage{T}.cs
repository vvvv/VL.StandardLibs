namespace VL.IO.Redis;

/// <summary>
/// Represents a message that is broadcast via publish/subscribe
/// </summary>
/// <typeparam name="T">The type of the value</typeparam>
/// <param name="Channel">The channel on which the message was broadcasted.</param>
/// <param name="Value">The value of the message.</param>
public readonly record struct ChannelMessage<T>(string Channel, T Value);
