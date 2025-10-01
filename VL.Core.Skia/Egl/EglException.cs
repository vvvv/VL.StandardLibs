#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static VL.Skia.Egl.NativeEgl;

namespace VL.Skia.Egl
{
    public class EglException : Exception
    {
        public EglException(string message) : base(message)
        {
        }
    }

    public class EglContextLostException : EglException
    {
        public EglContextLostException(string message) : base(message)
        {
        }
    }
}
