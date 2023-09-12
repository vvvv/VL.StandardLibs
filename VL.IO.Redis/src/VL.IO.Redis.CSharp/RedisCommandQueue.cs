using Collections.Pooled;
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

        internal ConcurrentQueue<Func<ITransaction, ValueTuple<Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>>>> Cmds = new ConcurrentQueue<Func<ITransaction, (Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>)>>();
        internal ConcurrentQueue<Task<KeyValuePair<Guid, object>>> Tasks = new ConcurrentQueue<Task<KeyValuePair<Guid, object>>>();

        internal PooledSet<string> Changes = new PooledSet<string>();
        internal PooledSet<string> ReceivedChanges = new PooledSet<string>();

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
            Changes.Clear();
            ReceivedChanges.Clear();
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
                Changes.Dispose();
                ReceivedChanges.Dispose();

                _disposedValue = true;
            }
        }
        #endregion
    }
}
