using System;

namespace VL.Core
{
    [Flags]
    public enum SymbolSmell
    {
        Default = 0,
        Obsolete = 1 << 0,
        Experimental = 1 << 1,
        Advanced = 1 << 2,

        Hidden = 1 << 3,
        Internal = 1 << 4,
        HiddenOrInternal = Hidden | Internal,

        External = 1 << 8,
        Adaptive = 1 << 9,
    }
}
