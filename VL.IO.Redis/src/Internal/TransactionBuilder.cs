using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VL.IO.Redis.Internal
{
    /// <summary>
    /// Used to build a Redis transaction.
    /// </summary>
    sealed class TransactionBuilder
    {
        private readonly List<Func<ITransaction, Task>> _actions = new();
        private readonly List<Task> _tasks = new();

        public bool IsEmpty => _actions.Count == 0;

        public CommandFlags CommandFlags { get; set; }

        public void Add(Func<ITransaction, Task> asyncAction)
        {
            _actions.Add(asyncAction);
        }

        public Task BuildAndExecuteAsync(IDatabase database)
        {
            _tasks.Clear();

            var transaction = database.CreateTransaction();
            foreach (var action in _actions)
                _tasks.Add(action(transaction));
            _tasks.Add(transaction.ExecuteAsync(CommandFlags));

            return Task.WhenAll(_tasks);
        }

        public void Clear()
        {
            _actions.Clear();
            CommandFlags = CommandFlags.None;
        }
    }
}
