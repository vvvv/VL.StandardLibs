using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// Used by the Publish node to turn collection items of properties into individual public channels.
    /// Items of properties are published unless marked with [PublishIndividualItems(false)].
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)] // TODO: shouldn't be on field
    public class PublishIndividualItemsAttribute : Attribute
    {
        bool publishIndividualItems;

        public PublishIndividualItemsAttribute(bool publishIndividualItems)
        {
            this.publishIndividualItems = publishIndividualItems;
        }

        public override string ToString()
        {
            return $"PublishIndividualItems: {publishIndividualItems}";
        }

        public bool PublishIndividualItems => publishIndividualItems;

        public static PublishIndividualItemsAttribute Yes = new(true);
        public static PublishIndividualItemsAttribute No = new(false);
    }
}
