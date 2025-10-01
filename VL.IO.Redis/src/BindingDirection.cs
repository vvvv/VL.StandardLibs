using System;

namespace VL.IO.Redis
{
    [Flags]
    public enum BindingDirection 
    {
        In = 1,
        Out = 2,
        InOut = In | Out
    }
}
