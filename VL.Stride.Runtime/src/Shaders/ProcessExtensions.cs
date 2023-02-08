using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Stride.Shaders
{
    public static class ProcessExtensions
    {
        public static void StartExe(string exe, string arguments)
        {
            System.Diagnostics.Process.Start(exe, arguments);
        }

        public static Exception InnermostException(this Exception e)
        {
            while (e.InnerException != null)
                e = e.InnerException;
            return e;
        }
    }
}
