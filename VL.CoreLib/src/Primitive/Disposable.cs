using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.Lib.Primitive
{
    public static class IDisposableUtils
    {
        /// <summary>
        /// Tries to cast the input to IEnumerable or IDisposable, and then either tries to dispose all elements or the input itself.
        /// Does not catch exceptions.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="success">Set to <c>true</c> if Dispose was called successfully on all elements or the input instance.</param>
        public static void TryDispose(object input, out bool success)
        {
            success = false;
            var enumerable = input as ICollection;
            if (enumerable != null)
            {
                TryDisposeSequence(enumerable, out success);
            }
            else
            {
                TryDisposeInstance(input, out success);
            }
        }

        /// <summary>
        /// Tries to cast the input to IDisposable and then calls Dispose. 
        /// Does not catch exceptions.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="success">Set to <c>true</c> if input is a IDisposable and Dispose was called successfully.</param>
        public static void TryDisposeInstance(object input, out bool success)
        {
            success = false;
            var disposable = input as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
                success = true;
            }
        }

        /// <summary>
        /// Tries to cast the elements in the input to IDisposable and then calls Dispose on each element. 
        /// Does not catch exceptions.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="success">Set to <c>true</c> if Dispose was called successfully on all elements.</param>
        public static void TryDisposeSequence(ICollection input, out bool success)
        {
            success = false;
            if (input != null)
            {
                success = true;
                foreach (var item in input)
                {
                    bool itemSuccess;
                    TryDisposeInstance(item, out itemSuccess);
                    success &= itemSuccess;
                } 
            }
        }
    }
}
