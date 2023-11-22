namespace VL.Model
{
    /// <summary>
    /// Aka AutoConnect. Makes the pin show up in the parent(s).
    /// </summary>
    public enum PinExpositionMode
    {
        /// <summary>
        /// Off
        /// </summary>
        Local,
        /// <summary>
        /// Makes the pin show up in the surrounding patch
        /// </summary>
        InfectPatch,
        /// <summary>
        /// Infect all surrounding patches (the call stack)
        /// </summary>
        Expose
    }
}
