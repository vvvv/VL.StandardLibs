using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO;

namespace VL.Lang.PublicAPI
{
    public interface ICommandService
    {
        public IDisposable RegisterCommand(string name, ICommand command);
    }

    public interface ICommand
    {
        public Keys Shortcut { get; }

        public bool IsVisible { get; }

        public bool IsEnabled { get; }

        public void Execute();
    }
}
