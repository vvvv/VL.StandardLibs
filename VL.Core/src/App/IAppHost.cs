using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace VL.Core
{
    public interface IAppHost
    {
        /// <summary>
        /// Raised when the application exits.
        /// </summary>
        IObservable<Unit> OnExit { get; }
    }
}
