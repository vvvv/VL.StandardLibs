using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// If present the object editor will show a type selector on channels with an abstract value type.
    /// The type selection can be controlled via the <see cref="Includes"/> and <see cref="Excludes"/> properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class TypeSelectorAttribute : Attribute
    {
        public static TypeSelectorAttribute Create(IEnumerable<string> includes, IEnumerable<string> excludes)
        {
            return new TypeSelectorAttribute()
            {
                Includes = includes?.ToArray(),
                Excludes = excludes?.ToArray()
            };
        }

        /// <summary>
        /// A list of types to include in the dropdown. Wildcards (*) are supported.
        /// </summary>
        public string[] Includes { get; set; }

        /// <summary>
        /// A list of types to exclude in the dropdown. Wildcards (*) are supported.
        /// </summary>
        public string[] Excludes { get; set; }

        /// <summary>
        /// Whether or not the given type name matches the include and exclude patterns.
        /// </summary>
        public bool IsMatch(string typeName)
        {
            return (IsNullOrEmpty(Includes) || MatchesAny(typeName, Includes)) &&
                   (IsNullOrEmpty(Excludes) || !MatchesAny(typeName, Excludes));

            static bool IsNullOrEmpty(string[] strings) => strings is null || strings.Length == 0;

            static bool MatchesAny(string value, string[] patterns)
            {
                foreach (var pattern in patterns)
                    if (value.Like(pattern))
                        return true;
                return false;
            }
        }
    }
}
