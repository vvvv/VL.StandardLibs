using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Diagnostics;
using VL.Core.Diagnostics;
using ILogger = Stride.Core.Diagnostics.ILogger;

namespace VL.Stride.Core
{
    public static class LoggerExtensions
    {
        public static ILogger GetLoggerResult(string name = null) 
            => new LoggerResult(name);

        internal static bool TryGetFilePath(this ILogMessage m, out string path)
        {
            var module = m.Module;
            if (module != null && TryGetFilePath(module, out path))
                return true;
            return TryGetFilePath(m.Text, out path);
        }

        private static bool TryGetFilePath(string s, out string path)
        {
            var index = s.IndexOf('(');
            if (index >= 0)
            {
                path = s.Substring(0, index);
                return true;
            }
            else
            {
                path = null;
                return false;
            }
        }
    }
}
