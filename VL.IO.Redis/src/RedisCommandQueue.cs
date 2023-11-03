using Collections.Pooled;
using Microsoft.Win32.SafeHandles;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VL.Core;

namespace VL.IO.Redis
{
    public class RedisCommandQueue : IDisposable
    {
        private readonly NodeContext nodeContext;
        private readonly CompositeDisposable warnings;
        private readonly IVLRuntime runtime;

        internal readonly ConnectionMultiplexer Multiplexer;
        internal readonly Guid id;
        

        internal IDatabase Database;
        internal ITransaction Transaction;

        internal ConcurrentQueue<Func<ITransaction, ValueTuple<Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>>>> Cmds = new ConcurrentQueue<Func<ITransaction, (Task<KeyValuePair<Guid, object>>, IEnumerable<RedisKey>)>>();
        internal ConcurrentQueue<Task<KeyValuePair<Guid, object>>> Tasks = new ConcurrentQueue<Task<KeyValuePair<Guid, object>>>();

        internal PooledSet<string> Changes = new PooledSet<string>();
        internal PooledSet<string> ReceivedChanges = new PooledSet<string>();

        public RedisCommandQueue(NodeContext nodeContext, ConnectionMultiplexer Multiplexer, Guid id)
        {
            this.nodeContext = nodeContext;
            this.warnings = new CompositeDisposable();
            this.runtime = IVLRuntime.Current;

            this.Multiplexer = Multiplexer;
            this.id = id;
        }

        public void CreateTransaction(IDatabase Database)
        {
            this.Database = Database;
            Transaction = Database.CreateTransaction();
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

                if (warnings.Count > 0) warnings.Clear();
            }
            catch (Exception ex)
            {
                warnings.AddExeption("RedisCommandQueue failed to dispose Tasks in Clear().", ex, nodeContext, runtime);
            }
            Tasks.Clear();
        }

        #region Dispose 
        

        // To detect redundant calls
        private bool _disposedValue;

        #nullable enable
        // Instantiate a SafeHandle instance.
        private SafeHandle? _safeHandle = new SafeFileHandle(IntPtr.Zero, true);
        #nullable disable

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

                    if (warnings.Count > 0) warnings.Clear();
                }
                catch (Exception ex)
                {
                    warnings.AddExeption("RedisCommandQueue failed to dispose Tasks in Clear().", ex, nodeContext, runtime);
                }


                if (!warnings.IsDisposed)
                    warnings.Dispose();

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
