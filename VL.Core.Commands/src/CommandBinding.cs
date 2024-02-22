namespace VL.Core.Commands;

/// <summary>
/// A command bound to a specific keyboard shortcut.
/// </summary>
/// <param name="Keys">A bitwise combination of keys.</param>
/// <param name="Command">The command to execute when pressing the shortcut.</param>
public record struct CommandBinding(Keys Keys, ICommand Command);
