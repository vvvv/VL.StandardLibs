#nullable enable

namespace VL.Core
{
    /// <summary>
    /// Implement this interface to provide a summary and remarks.
    /// </summary>
    public interface IInfo
    {
        /// <summary>
        /// A short description.
        /// </summary>
        string? Summary { get; }

        /// <summary>
        /// Remarks are about details.
        /// </summary>
        string? Remarks { get; }
    }
}
#nullable restore