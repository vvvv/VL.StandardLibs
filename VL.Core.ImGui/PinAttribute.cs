namespace VL.ImGui
{
    public class PinAttribute : Attribute
    {
        public int Priority { get; set; }

        public bool Ignore { get; set; }
    }
}
