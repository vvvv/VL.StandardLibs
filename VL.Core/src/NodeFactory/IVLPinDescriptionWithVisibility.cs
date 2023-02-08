#nullable enable

namespace VL.Core
{
    public interface IVLPinDescriptionWithVisibility
    {
        // Moving this to the main interface would break all implementations. Therefor keep it seperated for now.
        bool IsVisible { get; }
    }
}
#nullable restore