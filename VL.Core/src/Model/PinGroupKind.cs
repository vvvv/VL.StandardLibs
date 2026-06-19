namespace VL.Model
{
    public enum PinGroupKind
    {
        None,
        /// <summary>
        /// Supported types are: Array, ImmutableArray and Spread
        /// </summary>
        Collection,
        /// <summary>
        /// Supported types are: Dictionary and ImmutableDictionary
        /// </summary>
        Dictionary
    }
}
