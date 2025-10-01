namespace VL.Core.Commands;

/// <summary>
/// Provides various factory methods to create a <see cref="ICommandList"/>
/// </summary>
public static class CommandList
{
    /// <summary>
    /// Creates a command list out of the given command bindings.
    /// </summary>
    public static ICommandList Create([Pin(PinGroupKind = PinGroupKind.Collection, PinGroupDefaultCount = 2)] params CommandBinding[] binding)
    {
        return CreateRange(binding);
    }

    /// <summary>
    /// Creates a command list out of the given command bindings.
    /// </summary>
    public static ICommandList CreateRange(IEnumerable<CommandBinding> bindings)
    {
        return new FixedCommandList(bindings.ToDictionary(c => c.Keys, c => c.Command));
    }

    /// <summary>
    /// Creates a command list out of other command lists. The first command list which provides a command for a given key wins.
    /// </summary>
    public static ICommandList Combine([Pin(PinGroupKind = PinGroupKind.Collection, PinGroupDefaultCount = 2)] params ICommandList?[] list)
    {
        return CombineRange(list);
    }

    /// <summary>
    /// Creates a command list out of other command lists. The first command list which provides a command for a given key wins.
    /// </summary>
    public static ICommandList CombineRange(IEnumerable<ICommandList?> lists)
    {
        return new CombinedCommandList(lists.Where(c => c != null).ToImmutableArray()!);
    }

    /// <summary>
    /// Creates a command list simply to "reserve" certain keys.
    /// </summary>
    public static ICommandList Reserve([Pin(PinGroupKind = PinGroupKind.Collection, PinGroupDefaultCount = 2)] params Keys[] shortcut)
    {
        return ReserveRange(shortcut);
    }

    /// <summary>
    /// Creates a command list simply to "reserve" certain keys.
    /// </summary>
    public static ICommandList ReserveRange(IEnumerable<Keys> shortcuts)
    {
        return CreateRange(shortcuts.Select(k => new CommandBinding(k, Command.Create(() => { }))));
    }

    public static bool TryExecute(this ICommandList commands, INotification notification)
    {
        if (notification is KeyCodeNotification keyCodeNotification && commands.TryGetCommand(keyCodeNotification.KeyData, out var command))
        {
            if (keyCodeNotification.IsKeyDown && command.IsEnabled)
                command.Execute();
            return true;
        }
        return false;
    }

    public static IDisposable SubscribeTo(this ICommandList commandList, IObservable<KeyNotification> notifications)
    {
        if (commandList is null)
            return Disposable.Empty;

        return notifications.Subscribe(n =>
        {
            if (commandList.TryExecute(n))
            {
                n.Handled = true;
            }
        });
    }

    private sealed class FixedCommandList(Dictionary<Keys, ICommand> Commands) : ICommandList
    {
        public bool TryGetCommand(Keys keys, [NotNullWhen(true)] out ICommand? command)
        {
            return Commands.TryGetValue(keys, out command);
        }
    }

    private sealed class CombinedCommandList(ImmutableArray<ICommandList> commandLists) : ICommandList
    {
        public bool TryGetCommand(Keys keys, [NotNullWhen(true)] out ICommand? command)
        {
            foreach (var list in commandLists)
                if (list.TryGetCommand(keys, out command))
                    return true;

            command = null;
            return false;
        }
    }
}
