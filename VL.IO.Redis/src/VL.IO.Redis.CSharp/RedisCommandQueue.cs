using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.IO.Redis
{
    public class RedisCommandQueue
    {
        private ITransaction _tran;
        private readonly IList<Task<KeyValuePair<Guid, object>>> _tasks = new List<Task<KeyValuePair<Guid, object>>>();
        private readonly IList<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> _cmds = new List<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>();
        private readonly List<RedisKey> _changedkeys = new List<RedisKey>();

        private Task<Task<ImmutableDictionary<Guid, object>>> _result;

        private Task<bool> _transaction;
        private Task<ImmutableDictionary<Guid, object>> _output;

        public int Count => _tasks.Count;
        public void Enqueue(Func<ITransaction, Task<KeyValuePair<Guid, object>>> cmd)
        {
            _cmds.Add(cmd);
        }

        public void ChangedKeys(IEnumerable<RedisKey> keys)
        {
            _changedkeys.AddRange(keys);
        }


        public RedisCommandQueue()
        { }

        public void BeginTransaction(IDatabase database)
        {
            _tasks.Clear();
            _cmds.Clear();
            _changedkeys.Clear();
            _tran = database.CreateTransaction();
        }

        public void ExecuteAsync(out int Count)
        {
            
            if (_tran != null)
                foreach (var cmd in _cmds)
                    _tasks.Add(cmd(_tran));

            Count = _tasks.Count;

            _result = _tran.ExecuteAsync().ContinueWith(
            t =>
                Task.WhenAll(_tasks).ContinueWith(t => t.Result.ToImmutableDictionary())
            );
        }

        public void ExecuteAsync2(out int Count)
        {

            if (_tran != null)
                foreach (var cmd in _cmds)
                    _tasks.Add(cmd(_tran));

            Count = _tasks.Count;

            _transaction = _tran.ExecuteAsync();
        }

        public async Task<ImmutableDictionary<Guid, object>> WaitTransaction()
        {
            if (await _transaction)
                return await Task.WhenAll(_tasks).ContinueWith(t => new Dictionary<Guid, object>(t.Result).ToImmutableDictionary());
            else
                return ImmutableDictionary<Guid, object>.Empty;
        }

        public ImmutableDictionary<Guid, object> Result2()
        {
            return _output.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public ImmutableDictionary<Guid, object> Result()
        {
            return _result.ConfigureAwait(false).GetAwaiter().GetResult().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public ImmutableList<RedisKey> Changed()
        {
            return _changedkeys.ToImmutableList();
        }
    }
}
