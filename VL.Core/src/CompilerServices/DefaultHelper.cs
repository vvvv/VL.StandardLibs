using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core.CompilerServices
{
    public static class DefaultHelper
    {
        // Used by CreateDefault (Generic) which acts as fallback for all CreateDefault [Adaptive] implementations
        public static T DEFAULT<T>() => default;
    }
}
