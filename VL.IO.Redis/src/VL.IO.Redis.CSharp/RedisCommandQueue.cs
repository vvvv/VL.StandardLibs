using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public class RedisCommandQueue : IDisposable
    {
        internal Guid _id;
        internal ITransaction _tran;



        private Pooled<List<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>> pooledCmds = Pooled.GetList<Func<ITransaction, Task<KeyValuePair<Guid, object>>>>();
        internal List<Func<ITransaction, Task<KeyValuePair<Guid, object>>>> Cmds => pooledCmds.Value;


        private Pooled<ImmutableHashSet<string>.Builder> pooledChanges = Pooled.GetHashSetBuilder<string>();
        internal ImmutableHashSet<string>.Builder ChangesBuilder => pooledChanges.Value;
        internal ImmutableHashSet<string> Changes => pooledChanges.Value.ToImmutable();


        private Pooled<ImmutableHashSet<string>.Builder> pooledReceivedChanges = Pooled.GetHashSetBuilder<string>();
        internal ImmutableHashSet<string>.Builder ReceivedChangesBuilder => pooledReceivedChanges.Value;
        internal ImmutableHashSet<string> ReceivedChanges => pooledReceivedChanges.Value.ToImmutable();


        private Pooled<List<Task<KeyValuePair<Guid, object>>>> pooledtasks = Pooled.GetList<Task<KeyValuePair<Guid, object>>>();

        internal List<Task<KeyValuePair<Guid, object>>> Tasks => pooledtasks.Value;


        public RedisCommandQueue(Guid id, IDatabase database)
        {
            _id                 = id;
            _tran               = database.CreateTransaction();
        }

        public void Dispose()
        {
            try
            {
                foreach (var task in Tasks)
                {
                    if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted)
                        task.Dispose();
                }
                pooledtasks.Dispose();
            }
            catch (Exception e) 
            {
                
            }
            pooledCmds.Dispose();
            pooledChanges.Dispose();
            pooledReceivedChanges.Dispose();
        }
    }
}
