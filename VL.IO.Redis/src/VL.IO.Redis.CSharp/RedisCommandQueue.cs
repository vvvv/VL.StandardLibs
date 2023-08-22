using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public record RedisCommandQueue
    {
        internal Guid _id;
        internal ITransaction _tran;
        internal ConcurrentQueue<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> _cmds;
        internal SpreadBuilder<string> _changes;
        internal IList<Task<KeyValuePair<Guid, object>>> _tasks;

        internal SpreadBuilder<string> _receivedChanges;

        public void AddReceivedChanges(Spread<string> receivedChanges)
        {
            this._receivedChanges.AddRange(receivedChanges);
        }

        public Spread<string> GetReceivedChanges()
        {
            return _receivedChanges.ToSpread();
        }

        public RedisCommandQueue(IDatabase database, Guid id)
        {
            _tran               = database.CreateTransaction();
            _id                 = id;
            _cmds               = new ConcurrentQueue<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>();
            _changes            = new SpreadBuilder<string>();
            _receivedChanges    = new SpreadBuilder<string>();
            _tasks              = new List<Task<KeyValuePair<Guid, object>>>();
        }

        

    }
}
