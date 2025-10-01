using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// Tags can be used to classify properties. 
    /// This is useful for the user and can also be used by a host to guide the binding to the outside world. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class TagAttribute : Attribute
    {
        public TagAttribute(string tagLabel)
        {
            TagLabel = tagLabel;
        }

        public string TagLabel { get; }

        public override string ToString()
        {
            return $"Tag: {TagLabel}";
        }
    }
}
