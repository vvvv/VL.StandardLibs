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
    /// The Publish node interprets the absence of the attribute means that the property is published.
    /// Use the attribute to mark properties that should not be published.
    /// Might later on also be used by an auto-publish system, which might interpret the absence of the attribute as "don't publish".
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)] // TODO: shouldn't be on field
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
