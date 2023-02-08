using System;
using System.Collections.Generic;
using System.Text;

namespace VL.Core
{
    /// <summary>
    /// Thrown in case initialization code fails.
    /// </summary>
    public class InitializationException : Exception
    {
        public InitializationException(Exception innerException)
            : base($"Initialization failed", innerException)
        {

        }
    }
}
