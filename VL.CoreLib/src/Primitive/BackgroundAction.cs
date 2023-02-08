using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;

namespace VL.Lib.Primitive.BackgroundAction
{
    public static class BackgroundActionHelpers
    {
        public static Task StartNew<T>(Func<T> action)
        {
            //shouldn't use Task.Factory.StartNew, see:
            //http://blog.stephencleary.com/2013/08/startnew-is-dangerous.html
            return Task.Run(action);
        }

        public static void WaitAll(Task taskA, Task taskB)
        {
            Task.WaitAll(taskA, taskB);
        }
    }
}
