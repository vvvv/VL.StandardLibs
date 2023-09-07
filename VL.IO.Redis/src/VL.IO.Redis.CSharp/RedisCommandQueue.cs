using Microsoft.Win32.SafeHandles;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.IO.Redis
{
    public class RedisCommandQueue : IDisposable
    {
        internal  Guid _id;

        internal  ITransaction Transaction;

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


        public RedisCommandQueue(Guid id)
        {
            _id                 = id;
        }

        public void CreateTransaction(IDatabase database)
        {
            Transaction = database.CreateTransaction();
        }

        public void Clear()
        {
            Cmds.Clear();
            ChangesBuilder.Clear();
            ReceivedChangesBuilder.Clear();
            try
            {
                foreach (var task in Tasks)
                {
                    if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted)
                        task.Dispose();
                }

            }
            catch (Exception e)
            {

            }
            Tasks.Clear();
        }

        #region Dispose 
        

        // To detect redundant calls
        private bool _disposedValue;

        // Instantiate a SafeHandle instance.
        private SafeHandle? _safeHandle = new SafeFileHandle(IntPtr.Zero, true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _safeHandle?.Dispose();
                    _safeHandle = null;
                }

                try
                {
                    foreach (var task in Tasks)
                    {
                        if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted)
                            task.Dispose();
                    }

                }
                catch (Exception e)
                {

                }
                Tasks.Clear();
                Cmds.Clear();
                ChangesBuilder.Clear();
                ReceivedChangesBuilder.Clear();

                _disposedValue = true;
            }
        }
        #endregion
    }
}
