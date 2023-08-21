using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public record RedisCommandQueue
    {
        internal ITransaction _tran;
        internal IList<Task<KeyValuePair<Guid, object>>> _tasks;
        internal ConcurrentQueue<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> _cmds;
        internal SpreadBuilder<string> _changes;

        internal SpreadBuilder<string> _receivedChanges;

        public void AddReceivedChanges(Spread<string> receivedChanges)
        {
            this._receivedChanges.AddRange(receivedChanges);
        }

        public Spread<string> GetReceivedChanges()
        {
            return _receivedChanges.ToSpread();
        }

        public RedisCommandQueue(IDatabase database)
        {
            _tran           = database.CreateTransaction();
            _tasks          = new List<Task<KeyValuePair<Guid, object>>>();
            _cmds           = new ConcurrentQueue<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>();
            _changes        = new SpreadBuilder<string>();
            _receivedChanges= new SpreadBuilder<string>();
        }

        

    }
}
