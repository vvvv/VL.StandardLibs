using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public class RedisCommandQueue : IDisposable
    {
        internal  Guid _id;


        internal  ITransaction _tran;

        //private Pooled<ConcurrentQueue<Func<ITransaction, ValueTuple<Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>>>>> pooledCmds = CustomPooled.GetConcurrentQueue<Func<ITransaction, ValueTuple<Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>>>>();
        internal ConcurrentQueue<Func<ITransaction, ValueTuple<Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>>>> Cmds = new ConcurrentQueue<Func<ITransaction, (Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>)>>(); // => pooledCmds.Value;

        //private Pooled<ImmutableHashSet<string>.Builder> pooledChanges = Pooled.GetHashSetBuilder<string>();
        internal ImmutableHashSet<string>.Builder ChangesBuilder = ImmutableHashSet.CreateBuilder<string>(); // => pooledChanges.Value;
        //internal ImmutableHashSet<string> Changes = ChangesBuilder.ToImmutable();// => pooledChanges.Value.ToImmutable();


        //private Pooled<ImmutableHashSet<string>.Builder> pooledReceivedChanges = Pooled.GetHashSetBuilder<string>();
        internal ImmutableHashSet<string>.Builder ReceivedChangesBuilder = ImmutableHashSet.CreateBuilder<string>(); //=> pooledReceivedChanges.Value;
        //internal ImmutableHashSet<string> ReceivedChanges = ReceivedChangesBuilder.ToImmutable(); //=> pooledReceivedChanges.Value.ToImmutable();


        //private Pooled<ConcurrentQueue<Task<KeyValuePair<Guid, object>>>> pooledtasks = CustomPooled.GetConcurrentQueue<Task<KeyValuePair<Guid, object>>>();

        internal  ConcurrentQueue<Task<KeyValuePair<Guid, object>>> Tasks = new ConcurrentQueue<Task<KeyValuePair<Guid, object>>>(); // => pooledtasks.Value;


        public RedisCommandQueue(Guid id, IDatabase database)
        {
            _id                 = id;
            _tran               = database.CreateTransaction();
        }

        public void Dispose()
        {
            //try
            //{
            //    foreach (var task in Tasks)
            //    {
            //        if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted)
            //            task.Dispose();
            //    }
            //    pooledtasks.Dispose();
            //}
            //catch (Exception e) 
            //{
                
            //}
            //pooledCmds.Dispose();
            //pooledChanges.Dispose();
            //pooledReceivedChanges.Dispose();
        }
    }
}
