#nullable enable
using VL.Lang.PublicAPI;
using VL.Lib.Reactive;

namespace VL.Core
{
    /// <summary>
    /// Optional interface for nodes that can "learn" from user input. When implemented the node inspector will show a learn button.
    /// Use in combination with <see cref="IDevSession.RegisterNode(IVLObject)"/>.
    /// </summary>
    public interface IHasLearnMode
    {
        /// <summary>
        /// This channel will get hooked up to a button in the node inspector that allows the user to toggle the learn mode.
        /// </summary>
        IChannel<bool> LearnMode { get; }
    }
}
