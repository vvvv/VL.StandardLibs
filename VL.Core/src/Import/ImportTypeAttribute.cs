#nullable enable
using System;

namespace VL.Core.Import;

/// <summary>
/// Makes the specified type directly available in VL.
/// Process nodes can be defined with the <see cref="ProcessNodeAttribute"/>.
/// </summary>
/// <remarks>
/// Nested types are not supported / not tested at all.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ImportTypeAttribute : ImportAttribute
{
    internal object? OriginalTypeSymbol;

    /// <summary>
    /// Makes the type and all its public members directly available in VL.
    /// </summary>
    /// <param name="type">The type to import.</param>
    public ImportTypeAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// The type to import.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The name to use in VL. Leave empty to use the type's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Defines the category where the type is placed in VL.
    /// If not set the category is the namespace of the type with optional <see cref="NamespacePrefixToStrip"/> removed.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Defines the namespace prefix (excluding the .) to strip from the type's namespace.
    /// For example "VL" with class "VL.Foo.Bar" will make it show up as "Bar [Foo]".
    /// </summary>
    public string? NamespacePrefixToStrip { get; set; }

    public override bool IsMatch(Type type) => type == Type;

    public override string? GetCategory(string? ns)
    {
        var root = Category ?? string.Empty;
        if (string.IsNullOrEmpty(ns))
            return root;

        if (NamespacePrefixToStrip is null)
            return Category ?? ns;

        string cat;
        if (ns.StartsWith(NamespacePrefixToStrip) && ns.Length > NamespacePrefixToStrip.Length)
            cat = ns.Substring(NamespacePrefixToStrip.Length + 1);
        else
            cat = string.Empty;

        if (string.IsNullOrEmpty(cat))
            return root;
        else if (string.IsNullOrEmpty(root))
            return cat;
        else
            return $"{root}.{cat}";
    }
}
