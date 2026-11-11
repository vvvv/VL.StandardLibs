#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VL.Lib.Collections;

namespace VL.Core;

/// <summary>
/// A segment that can be appended to a <see cref="ValuePath"/>.
/// </summary>
/// <param name="Segment">The segment.</param>
/// <param name="Type">The declared type of what the segment leads to.</param>
/// <param name="HasChildren">Whether there is something to pick after the segment.</param>
/// <param name="IsTypeOnly">Whether the segment is only known from types, not from the live value: there was no value to look at (null or an empty collection).</param>
public sealed record ValuePathChild(ValuePathSegment Segment, Type Type, bool HasChildren, bool IsTypeOnly);

/// <summary>
/// The result of <see cref="ValuePathResolver.GetChildren"/>.
/// </summary>
/// <param name="ResolvedSegmentCount">How many segments of the path could be followed. Less than the segments of the path if one of them didn't lead anywhere.</param>
/// <param name="Type">The declared type of the node the resolved part of the path leads to.</param>
/// <param name="Children">The segments that can follow.</param>
/// <param name="OmittedCount">How many items of a collection are not part of <paramref name="Children"/>, because there are too many of them.</param>
public sealed record ValuePathChildren(int ResolvedSegmentCount, Type? Type, IReadOnlyList<ValuePathChild> Children, int OmittedCount);

/// <summary>
/// Finds out what can follow a <see cref="ValuePath"/> by walking the value and, where the value ends (null, an empty collection, a missing item), the type.
/// Resolves like the nodes working by path do, see <see cref="ValuePath"/>: it only looks at what the nodes can reach.
/// The properties of objects that aren't VL objects are only taken from their type, so no getter of foreign code is called to list them.
/// Dictionaries are only listed while there is a value.
/// </summary>
public static class ValuePathResolver
{
    /// <summary>
    /// How many items of a collection are listed by default.
    /// </summary>
    public const int DefaultMaxItems = 50;

    /// <summary>
    /// Gets the segments that can follow the given path. Follows the path as far as possible, if it doesn't lead anywhere the result tells how far it got
    /// and lists what can follow there.
    /// </summary>
    /// <param name="root">The value the path starts at. Must not change while this runs, the caller needs to take care of that.</param>
    /// <param name="rootType">The type of the root, used where the value is null. Defaults to the type of the value.</param>
    /// <param name="path">The path to follow.</param>
    /// <param name="typeRegistry">Used to look at the properties of VL objects. Without one only plain properties of .NET types are found.</param>
    /// <param name="maxItems">How many items of a collection to list.</param>
    public static ValuePathChildren GetChildren(object? root, Type? rootType, ValuePath path, TypeRegistry? typeRegistry, int maxItems = DefaultMaxItems)
    {
        var node = new Node(root, rootType ?? root?.GetType());
        var resolved = 0;
        foreach (var segment in path)
        {
            if (!TryStep(node, segment, typeRegistry, out var next))
                break;
            node = next;
            resolved++;
        }

        var children = new List<ValuePathChild>();
        var omitted = ListChildren(node, typeRegistry, maxItems, children);
        return new ValuePathChildren(resolved, node.Type, children, omitted);
    }

    /// <summary>
    /// Gets the declared type of what a segment leads to from a value, for example the element type of a list for an index, whether or not there is such an item.
    /// </summary>
    /// <param name="value">The value the segment is taken from.</param>
    /// <param name="segment">The segment.</param>
    /// <param name="typeRegistry">Used to look at the properties of VL objects.</param>
    /// <returns>The type, or null if the segment doesn't lead anywhere from the value.</returns>
    internal static Type? GetSegmentType(object? value, ValuePathSegment segment, TypeRegistry? typeRegistry)
        => TryStep(new Node(value, null), segment, typeRegistry, out var next) ? next.Type : null;

    /// <summary>
    /// A place in the graph: the value (null if there is none) and the declared type.
    /// </summary>
    readonly record struct Node(object? Value, Type? Type)
    {
        public Type? EffectiveType => Value?.GetType() ?? Type;
    }

    readonly record struct Property(string Name, Type Type, Func<object, object?> GetValue);

    static bool TryStep(Node node, ValuePathSegment segment, TypeRegistry? typeRegistry, out Node next)
    {
        next = default;
        var value = node.Value;
        var type = node.EffectiveType;
        if (type is null)
            return false;

        switch (segment.Kind)
        {
            case ValuePathSegmentKind.Property:
                if (IsCollection(value, type))
                    return false;
                var property = GetProperties(value, type, typeRegistry).FirstOrDefault(p => p.Name == segment.Name);
                if (property.Name is null)
                    return false;
                next = new Node(value is null ? null : ReadProperty(property, value), property.Type);
                return true;

            case ValuePathSegmentKind.Index:
                if (!TryGetElementType(value, type, out var elementType))
                    return false;
                // Beyond the end continues on the type
                next = new Node(TryGetItem(value, segment.Index, out var item) ? item : null, elementType);
                return true;

            default:
                if (!TryGetValueType(value, type, out var valueType))
                    return false;
                next = new Node(value is IDictionary dict && dict.Contains(segment.Name!) ? dict[segment.Name!] : null, valueType);
                return true;
        }
    }

    // Returns how many items didn't make it into the list
    static int ListChildren(Node node, TypeRegistry? typeRegistry, int maxItems, List<ValuePathChild> children)
    {
        var value = node.Value;
        var type = node.EffectiveType;
        if (type is null)
            return 0;

        var typeOnly = value is null;

        if (TryGetElementType(value, type, out var elementType))
        {
            var count = GetCount(value);
            for (var i = 0; i < Math.Min(count, maxItems); i++)
            {
                TryGetItem(value, i, out var item);
                children.Add(new(ValuePathSegment.ForIndex(i), elementType, HasChildren(new Node(item, elementType), typeRegistry), IsTypeOnly: false));
            }

            // Nothing to look at, but the type says what an item is
            if (count == 0)
                children.Add(new(ValuePathSegment.ForIndex(0), elementType, HasChildren(new Node(null, elementType), typeRegistry), IsTypeOnly: true));

            return Math.Max(0, count - maxItems);
        }

        if (TryGetValueType(value, type, out var valueType))
        {
            if (value is not IDictionary dict)
                return 0;

            var count = 0;
            foreach (DictionaryEntry entry in dict)
            {
                if (count++ < maxItems)
                    children.Add(new(ValuePathSegment.ForKey((string)entry.Key), valueType, HasChildren(new Node(entry.Value, valueType), typeRegistry), IsTypeOnly: false));
            }
            return Math.Max(0, count - maxItems);
        }

        foreach (var property in GetProperties(value, type, typeRegistry))
        {
            // Only VL objects hand out their values for free
            var propertyValue = value is IVLObject ? ReadProperty(property, value!) : null;
            children.Add(new(ValuePathSegment.ForProperty(property.Name), property.Type, HasChildren(new Node(propertyValue, property.Type), typeRegistry), typeOnly));
        }
        return 0;
    }

    static bool HasChildren(Node node, TypeRegistry? typeRegistry)
    {
        var type = node.EffectiveType;
        if (type is null)
            return false;
        if (TryGetElementType(node.Value, type, out _))
            return true;
        // Without a value the keys are not known yet
        if (TryGetValueType(node.Value, type, out _))
            return node.Value is not IDictionary { Count: 0 };
        return GetProperties(node.Value, type, typeRegistry).Any();
    }

    static object? ReadProperty(Property property, object instance)
    {
        try
        {
            return property.GetValue(instance);
        }
        catch (Exception)
        {
            // A getter that fails doesn't lead anywhere, but the path may still be right
            return null;
        }
    }

    static bool IsCollection(object? value, Type type)
        => TryGetElementType(value, type, out _) || TryGetValueType(value, type, out _);

    // The nodes check in this order: VL objects, spreads, dictionaries, lists and only then properties
    static bool TryGetElementType(object? value, Type type, out Type elementType)
    {
        elementType = typeof(object);
        if (value is IVLObject || typeof(IVLObject).IsAssignableFrom(type))
            return false;

        if (value is ISpread spread)
        {
            elementType = spread.ElementType;
            return true;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (TryGetGenericArgument(type, typeof(IList<>), 0, out elementType) || TryGetGenericArgument(type, typeof(IReadOnlyList<>), 0, out elementType))
            return true;

        elementType = typeof(object);
        return typeof(IList).IsAssignableFrom(type) || typeof(ISpread).IsAssignableFrom(type);
    }

    // Only dictionaries with string keys can be indexed
    static bool TryGetValueType(object? value, Type type, out Type valueType)
    {
        valueType = typeof(object);
        if (value is IVLObject || typeof(IVLObject).IsAssignableFrom(type))
            return false;

        if (value is IDictionary dict)
        {
            if (dict.Keys is not IEnumerable<string>)
                return false;
            valueType = GetDictionaryValueType(type) ?? typeof(object);
            return true;
        }

        if (value is not null)
            return false;

        valueType = GetDictionaryValueType(type)!;
        return valueType is not null;
    }

    static Type? GetDictionaryValueType(Type type)
    {
        var interfaces = type.IsInterface ? type.GetInterfaces().Append(type) : type.GetInterfaces();
        return interfaces
            .Where(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>) && i.GenericTypeArguments[0] == typeof(string))
            .Select(i => i.GenericTypeArguments[1])
            .FirstOrDefault();
    }

    static bool TryGetGenericArgument(Type type, Type genericInterface, int position, out Type argument)
    {
        var interfaces = type.IsInterface ? type.GetInterfaces().Append(type) : type.GetInterfaces();
        argument = interfaces
            .Where(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == genericInterface)
            .Select(i => i.GenericTypeArguments[position])
            .FirstOrDefault()!;
        return argument is not null;
    }

    static bool TryGetItem(object? collection, int index, out object? item)
    {
        item = null;
        switch (collection)
        {
            case ISpread spread when index >= 0 && index < spread.Count:
                item = spread.GetItem(index);
                return true;
            case not null when ListAdapter.TryGetList(collection, out var list) && index >= 0 && index < list.Count:
                item = list[index];
                return true;
            default:
                return false;
        }
    }

    static int GetCount(object? collection)
        => collection switch
        {
            ISpread spread => spread.Count,
            _ => ListAdapter.TryGetList(collection, out var list) ? list.Count : 0
        };

    static IEnumerable<Property> GetProperties(object? value, Type type, TypeRegistry? typeRegistry)
    {
        if (typeof(IVLObject).IsAssignableFrom(type) || value is IVLObject)
        {
            var typeInfo = value is IVLObject vlObject ? vlObject.Type : typeRegistry?.GetTypeInfo(type);
            return typeInfo?.Properties
                .Where(p => ValuePathSegment.CanBeWrittenAsProperty(p.OriginalName))
                .Select(p => new Property(p.OriginalName, p.Type.ClrType, p.GetValue))
                ?? [];
        }

        // Types of the framework are values to the nodes, not objects to dive into
        if (type.IsPrimitive || type.IsEnum || type.Namespace?.StartsWith(nameof(System)) == true)
            return [];

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && ValuePathSegment.CanBeWrittenAsProperty(p.Name))
            .Select(p => new Property(p.Name, p.PropertyType, p.GetValue));
    }
}
