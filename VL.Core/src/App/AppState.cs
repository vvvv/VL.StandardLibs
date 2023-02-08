using System;
using System.Collections.Generic;
using System.Text;

namespace VL.App
{
    public static class AppState
    {
        public static bool IsExported { get; internal set; }
        public static int RefCount { get; private set; }

        public static void IncreaseRefCount() => RefCount++;
        public static void DecreaseRefCount() => RefCount--;
    }
}
