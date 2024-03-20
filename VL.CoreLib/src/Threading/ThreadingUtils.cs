using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Lib.Threading
{
    public static class ThreadingUtils
    {
        public readonly struct MutexLock : IDisposable
        {
            public readonly Mutex M;

            public MutexLock(Mutex m)
            {
                M = m;
            }

            public void Dispose() => M.ReleaseMutex();
        }

        public readonly struct MonitorLock : IDisposable
        {
            public readonly object Key;

            public MonitorLock(object key)
            {
                Key = key;
            }

            public void Dispose() => Monitor.Exit(Key);
        }

        public static bool TryEnter(Mutex mutex, TimeSpan timeout, out MutexLock mutexLock)
        {
            if (mutex.WaitOne(Normalize(timeout)))
            {
                mutexLock = new MutexLock(mutex);
                return true;
            }
            else
            {
                mutexLock = default;
                return false;
            }
        }

        public static bool TryEnter(object key, TimeSpan timeout, out MonitorLock @lock)
        {
            var lockTaken = false;
            Monitor.TryEnter(key, Normalize(timeout), ref lockTaken);
            if (lockTaken)
            {
                @lock = new MonitorLock(key);
                return true;
            }
            else
            {
                @lock = default;
                return false;
            }
        }

        public static MonitorLock Enter(object key)
        {
            Monitor.Enter(key);
            return new MonitorLock(key);
        }

        private static TimeSpan Normalize(TimeSpan timeSpan) => timeSpan.Ticks < 0 ? TimeSpan.FromMilliseconds(-1d) : timeSpan;
    }
}
