#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace VL.Core;

/// <summary>
/// What a <see cref="ValuePathSegment"/> addresses.
/// </summary>
public enum ValuePathSegmentKind
{
    /// <summary>
    /// A property, written as <c>.Name</c>.
    /// </summary>
    Property,
    /// <summary>
    /// An item of a spread, array or list, written as <c>[2]</c>.
    /// </summary>
    Index,
    /// <summary>
    /// An item of a dictionary with string keys, written as <c>["key"]</c>. A <c>"</c> inside of the key is written twice.
    /// </summary>
    Key
}

/// <summary>
/// One step of a <see cref="ValuePath"/>.
/// </summary>
public readonly record struct ValuePathSegment
{
    private ValuePathSegment(ValuePathSegmentKind kind, string? name, int index)
    {
        Kind = kind;
        Name = name;
        Index = index;
    }

    public ValuePathSegmentKind Kind { get; }

    /// <summary>
    /// The name of the property or the key of the dictionary entry, null for an index.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The index of the item, 0 for the other kinds.
    /// </summary>
    public int Index { get; }

    public static ValuePathSegment ForProperty(string name) => new(ValuePathSegmentKind.Property, name, 0);

    public static ValuePathSegment ForIndex(int index) => new(ValuePathSegmentKind.Index, null, index);

    public static ValuePathSegment ForKey(string key) => new(ValuePathSegmentKind.Key, key, 0);

    /// <summary>
    /// Whether the name survives being written into a path and parsed back, which isn't the case if it's empty or contains <c>.</c> or <c>[</c>.
    /// </summary>
    public static bool CanBeWrittenAsProperty(string name) => name.Length > 0 && name.AsSpan().IndexOfAny('.', '[') < 0;

    /// <summary>
    /// Appends the segment to the given path. A property always gets a leading dot, so a property after the empty path gives ".Name".
    /// </summary>
    public string AppendTo(string path) => Kind == ValuePathSegmentKind.Property ? $"{path}.{this}" : path + this;

    /// <summary>
    /// The segment as written in a path, without the dot in front of a property: <c>Name</c>, <c>[2]</c> or <c>["key"]</c>.
    /// </summary>
    public override string ToString() => Kind switch
    {
        ValuePathSegmentKind.Property => Name!,
        ValuePathSegmentKind.Index => $"[{Index}]",
        _ => $"[\"{Name!.Replace("\"", "\"\"")}\"]"
    };
}

/// <summary>
/// A path to a value inside of an object graph, the way the nodes working "by path" understand it, for example <c>Items[2].Position.X</c> or <c>Lookup["a"].Name</c>:
/// <list type="bullet">
/// <item>Properties are separated by dots. The dot in front of the first one is optional. Properties are matched by their original name, there's no escaping.</item>
/// <item><c>[N]</c> picks an item of a spread, array or list.</item>
/// <item><c>["key"]</c> picks an entry of a dictionary with string keys. A <c>"</c> in the key is written twice.</item>
/// </list>
/// Parsing never fails: it keeps what it understood and tells where it stopped, which is what's needed for a path that is still being typed.
/// </summary>
public sealed class ValuePath : IReadOnlyList<ValuePathSegment>
{
    public static readonly ValuePath Empty = new([], string.Empty);

    private ValuePath(ImmutableArray<ValuePathSegment> segments, string remainder)
    {
        Segments = segments;
        Remainder = remainder;
    }

    /// <summary>
    /// The steps that were understood.
    /// </summary>
    public ImmutableArray<ValuePathSegment> Segments { get; }

    /// <summary>
    /// The text that couldn't be understood, empty if the whole text was. It starts at the segment with the index <see cref="Count"/>.
    /// </summary>
    public string Remainder { get; }

    /// <summary>
    /// Whether the whole text was understood. If not, the segment with the index <see cref="Count"/> is the one that failed.
    /// </summary>
    public bool IsValid => Remainder.Length == 0;

    public int Count => Segments.Length;

    public ValuePathSegment this[int index] => Segments[index];

    public IEnumerator<ValuePathSegment> GetEnumerator() => ((IEnumerable<ValuePathSegment>)Segments).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates a valid path from the given segments.
    /// </summary>
    public static ValuePath Create(IEnumerable<ValuePathSegment> segments)
    {
        var array = segments.ToImmutableArray();
        return array.IsEmpty ? Empty : new(array, string.Empty);
    }

    /// <summary>
    /// Parses the text. If a segment can't be understood the result holds the segments before it and puts the rest of the text into <see cref="Remainder"/>.
    /// </summary>
    public static ValuePath Parse(string? text)
    {
        var rest = text.AsSpan();
        var segments = ImmutableArray.CreateBuilder<ValuePathSegment>();
        while (!rest.IsEmpty && TryParseSegment(rest, out var segment, out var next))
        {
            segments.Add(segment);
            rest = next;
        }

        return segments.Count == 0 && rest.IsEmpty ? Empty : new(segments.ToImmutable(), rest.ToString());
    }

    /// <summary>
    /// Parses the text, returns false if it contains something that can't be understood.
    /// </summary>
    public static bool TryParse(string? text, out ValuePath path)
    {
        path = Parse(text);
        return path.IsValid;
    }

    /// <summary>
    /// Parses the segment at the start of the text.
    /// </summary>
    /// <param name="text">The text to parse. A property may start with a dot, an index or a key must not.</param>
    /// <param name="segment">The segment.</param>
    /// <param name="rest">The text following the segment.</param>
    /// <returns>False if the text doesn't start with a segment.</returns>
    public static bool TryParseSegment(ReadOnlySpan<char> text, out ValuePathSegment segment, out ReadOnlySpan<char> rest)
    {
        segment = default;
        rest = default;

        if (text.IsEmpty)
            return false;

        if (text[0] == '[')
        {
            if (text.Length >= 4 && text[1] == '"')
                return TryParseKey(text, out segment, out rest);

            // At least one digit, as in "[1]"
            var closeIndex = text.IndexOf(']');
            if (text.Length >= 3 && closeIndex > 1 && int.TryParse(text.Slice(1, closeIndex - 1), out var index))
            {
                segment = ValuePathSegment.ForIndex(index);
                rest = text.Slice(closeIndex + 1);
                return true;
            }

            return false;
        }

        var name = text[0] == '.' ? text.Slice(1) : text;
        var length = name.IndexOfAny('.', '[');
        if (length < 0)
            length = name.Length;
        if (length == 0)
            return false;

        segment = ValuePathSegment.ForProperty(name.Slice(0, length).ToString());
        rest = name.Slice(length);
        return true;
    }

    // Expects ["
    private static bool TryParseKey(ReadOnlySpan<char> text, out ValuePathSegment segment, out ReadOnlySpan<char> rest)
    {
        var key = new StringBuilder();
        for (var i = 2; i < text.Length; i++)
        {
            if (text[i] != '"')
            {
                key.Append(text[i]);
            }
            else if (i + 1 < text.Length && text[i + 1] == '"')
            {
                key.Append('"');
                i++;
            }
            else if (i + 1 < text.Length && text[i + 1] == ']')
            {
                segment = ValuePathSegment.ForKey(key.ToString());
                rest = text.Slice(i + 2);
                return true;
            }
            else
                break;
        }

        segment = default;
        rest = default;
        return false;
    }

    /// <summary>
    /// Writes the segments as a path, without a dot in front of the first property, for example <c>Items[2].Position</c>.
    /// </summary>
    public static string Format(IEnumerable<ValuePathSegment> segments)
    {
        var path = new StringBuilder();
        foreach (var segment in segments)
        {
            if (segment.Kind == ValuePathSegmentKind.Property && path.Length > 0)
                path.Append('.');
            path.Append(segment);
        }
        return path.ToString();
    }

    /// <summary>
    /// The path written out. The text that couldn't be understood is not part of it.
    /// </summary>
    public override string ToString() => Format(Segments);
}
