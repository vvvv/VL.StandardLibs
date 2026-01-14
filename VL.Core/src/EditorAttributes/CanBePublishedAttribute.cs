using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// Used by the Publish node to turn properties into public channels.
    /// VLObjects are published by default
    /// For .Net classes and structs, the default is not to publish anything. unless marked with [CanBePublished(true)].
    /// Properties are published unless marked with [CanBePublished(false)].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)] // TODO: shouldn't be on field
    public class CanBePublishedAttribute : Attribute
    {
        bool canBePublished;

        public CanBePublishedAttribute(bool canBePublished)
        {
            this.canBePublished = canBePublished;
        }

        public override string ToString()
        {
            return $"CanBePublished: {canBePublished}";
        }

        public bool CanBePublished => canBePublished;

        public static CanBePublishedAttribute Yes = new(true);
        public static CanBePublishedAttribute No = new(false);
    }
}
