namespace VL.Model
{
    /// <summary>
    /// Controls the visibility of pins. 
    /// </summary>
    public enum PinVisibility
    {
        /// <summary>
        /// The pin is always visible.
        /// </summary>
        Visible,
        /// <summary>
        /// The user can configure the node to show this pin on application side.
        /// </summary>
        Optional,
        /// <summary>
        /// The user won't see the pin.
        /// </summary>
        Hidden
    }
}
