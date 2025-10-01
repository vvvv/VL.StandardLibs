using System;
using VL.Lib.IO;

namespace VL.Core.Commands
{
    public interface ICommandService
    {
        public IDisposable RegisterCommand(string name, ICommand command, Keys shortCut = default, bool isVisible = true);
    }
}
