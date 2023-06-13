using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace VL.Core
{
    internal interface IHotSwappableEntryPoint
    {
        /// <summary>
        /// Fires before a frame gets executed.
        /// </summary>
        IObservable<Unit> OnSwap { get; }

        /// <summary>
        /// Allows to swap an object
        /// </summary>
        object Swap(object obj, Type compiletimeType);

        /// <summary>
        /// Allows to swap a type (e.g. for a List of Foo1 you get List of Foo2, where Foo2 is the hot-swapped version of Foo)
        /// </summary>
        Type SwapType(Type clrTypeOfValues);
    }
}
