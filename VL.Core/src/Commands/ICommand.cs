#nullable enable

namespace VL.Core.Commands
{
    /// <summary>
    /// Defines a command. The VL.Core.Commands package contains some useful factory methods to create commands.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Whether or not the command can execute.
        /// </summary>
        public bool IsEnabled { get; }

        /// <summary>
        /// Executs the commands.
        /// </summary>
        public void Execute();
    }
}
