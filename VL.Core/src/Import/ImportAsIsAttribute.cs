#nullable enable
using System;

namespace VL.Core.Import
{
    /// <summary>
    /// Makes all public types and their members directly available in VL.
    /// Process nodes can be defined with the <see cref="ProcessNodeAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ImportAsIsAttribute : Attribute
    {
        /// <summary>
        /// Defines the namespace from which types are getting picked up. The namespace will be stripped on VL side.
        /// For example namespace = "VL" with class "VL.Foo.Bar" will make it show up as "Bar [Foo]".
        /// </summary>
        public string? Namespace { get; set; }

        public bool IsMatch(string? ns)
        {
            if (Namespace is null)
                return true;

            if (ns is null)
                return false;

            return ns.StartsWith(Namespace);
        }

        public string? GetCategory(string? ns)
        {
            if (Namespace is null)
                return ns;

            if (!IsMatch(ns))
                return ns;

            if (ns is null)
                return null;

            if (ns.Length > Namespace.Length)
                return ns.Substring(Namespace.Length + 1);
            else
                return Namespace;
        }
    }
}
