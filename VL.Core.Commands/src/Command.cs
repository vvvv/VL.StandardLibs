namespace VL.Core.Commands;

/// <summary>
/// Provides various factory methods to create a <see cref="ICommand"/>.
/// </summary>
public static class Command
{
    /// <summary>
    /// Creates a command which delegates its <see cref="ICommand.Execute"/> call to the given delegate. The command is always enabled.
    /// </summary>
    public static ICommand Create(Action execute)
    {
        return new ActionCommand(execute);
    }

    /// <summary>
    /// Creates a command which delegates its <see cref="ICommand.Execute"/> and <see cref="ICommand.IsEnabled"/> calls to the given delegates.
    /// </summary>
    public static ICommand Create(Action execute, Func<bool> isEnabled)
    {
        return new ActionCommand(execute, isEnabled);
    }

    internal static ICommand WithExceptionManagement(this ICommand command)
    {
        if (command is CommandWithExceptionManagement)
            return command;
        return new CommandWithExceptionManagement(command);
    }

    /// <summary>
    /// Executes the command on the given <see cref="SynchronizationContext"/>.
    /// </summary>
    public static ICommand ExecuteOn(this ICommand command, SynchronizationContext context)
    {
        if (command is SynchronizationContextAwareCommand)
            return command;

        return new SynchronizationContextAwareCommand(command, context);
    }

    private sealed class ActionCommand(Action execute, Func<bool>? isEnabled = null) : ICommand
    {
        public bool IsEnabled => isEnabled != null ? isEnabled() : true;

        public void Execute()
        {
            execute();
        }
    }

    private sealed class CommandWithExceptionManagement(ICommand command) : ICommand
    {
        public bool IsEnabled => Wrap(() => command.IsEnabled);

        public void Execute() => Wrap(() => command.Execute());

        public T? Wrap<T>(Func<T> func)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                RuntimeGraph.ReportException(e);
                return default;
            }
        }

        public void Wrap(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                RuntimeGraph.ReportException(e);
            }
        }
    }

    private sealed class SynchronizationContextAwareCommand(ICommand command, SynchronizationContext context) : ICommand
    {
        public bool IsEnabled => command.IsEnabled;

        public void Execute()
        {
            if (context != SynchronizationContext.Current)
                context.Post(_ => command.Execute(), null);
            else
                command.Execute();
        }
    }
}
