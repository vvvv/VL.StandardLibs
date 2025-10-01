using System.Collections.Generic;

namespace VL.Core.Commands;

public interface IHasCommands
{
    public IEnumerable<(string Name, ICommand Command)> Commands { get; }
}
