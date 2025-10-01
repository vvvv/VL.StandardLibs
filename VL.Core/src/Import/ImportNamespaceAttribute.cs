#nullable enable
using System;

namespace VL.Core.Import;

/// <summary>
/// Makes all public types and their members of a given namespace directly available in VL.
/// Process nodes can be defined with the <see cref="ProcessNodeAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ImportNamespaceAttribute : ImportAttribute
{
    /// <summary>
    /// Makes all public types and their members under given namespace directly available in VL.
    /// </summary>
    /// <param name="namespace">The namespace to import.</param>
    public ImportNamespaceAttribute(string @namespace)
    {
        Namespace = @namespace;
    }

    /// <summary>
    /// Defines the namespace from which types are getting picked up. The namespace will be stripped on VL side.
    /// For example namespace = "VL" with class "VL.Foo.Bar" will make it show up as "Bar [Foo]".
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Defines the category where types are placed in VL.
    /// </summary>
    public string? Category { get; set; }

    public bool IsMatch(string? ns)
    {
        if (ns is null)
            return false;

        return ns.StartsWith(Namespace);
    }

    public override bool IsMatch(Type type) => IsMatch(type.Namespace);

    public override string? GetCategory(string? ns)
    {
        if (!IsMatch(ns))
            return null;

        var root = Category ?? string.Empty;
        if (string.IsNullOrEmpty(ns))
            return root;

        string cat;
        if (string.IsNullOrEmpty(Namespace))
            cat = ns;
        else if (ns.Length > Namespace.Length)
            cat = ns.Substring(Namespace.Length + 1);
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
