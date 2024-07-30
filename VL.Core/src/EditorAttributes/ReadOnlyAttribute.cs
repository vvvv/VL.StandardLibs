using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.EditorAttributes
{
    /// <summary>
    /// Read-only. No write access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)] 
    public class ReadOnlyAttribute : Attribute
    {
        public ReadOnlyAttribute()
        {
        }

        public override string ToString()
        {
            return $"ReadOnly";
        }
    }
}
