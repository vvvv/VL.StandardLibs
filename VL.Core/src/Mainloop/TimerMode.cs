using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Core
{
    /// <summary>
    /// Defines constants for the Timer's event types
    /// </summary>
    public enum TimerMode
    {
        /// <summary>
        /// Timer event occurs once.
        /// </summary>
        OneShot,

        /// <summary>
        /// Timer event occurs periodically.
        /// </summary>
        Periodic
    };
}
