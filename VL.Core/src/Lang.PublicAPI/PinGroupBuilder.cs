namespace VL.Lang.PublicAPI
{
    /// <summary>
    /// Modifies a pin group by simply adding pins to it and once finished calling commit on it. The pins will then get synchronized.
    /// </summary>
    public abstract class PinGroupBuilder
    {
        public abstract void Add(string name, string type);

        public abstract ISolution Commit();
    }
#nullable restore
}