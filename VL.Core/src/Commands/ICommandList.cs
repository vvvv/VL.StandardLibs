#nullable enable

using System.Diagnostics.CodeAnalysis;
using VL.Lib.IO;

namespace VL.Core.Commands
{
    /// <summary>
    /// Allows to organize commands. The VL.Core.Commands package contains some useful factory methods to create command lists.
    /// </summary>
    public interface ICommandList
    {
        /// <summary>
        /// Tries to get a command for the given key combination.
        /// </summary>
        bool TryGetCommand(Keys keys, [NotNullWhen(true)] out ICommand? command);
    }
}
