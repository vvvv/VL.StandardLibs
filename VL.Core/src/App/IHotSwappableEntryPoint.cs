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
    }
}
