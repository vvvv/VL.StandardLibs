#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using VL.Lib.IO;

namespace VL.Core.Commands
{
    public interface ICommandService
    {
        public IDisposable RegisterCommand(string name, ICommand command, Keys shortCut = default, bool isVisible = true);
        public bool TryGetCommand(string name, [NotNullWhen(true)] out ICommand? command);
    }
}
