using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NetSocket = System.Net.Sockets.Socket;

namespace VL.Lib.IO.Socket
{
    // From https://blogs.msdn.microsoft.com/pfxteam/2011/12/15/awaiting-socket-operations/
    sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };
        private TaskScheduler FScheduler;
        internal bool m_wasCompleted;
        internal Action m_continuation;
        internal SocketAsyncEventArgs m_eventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException(nameof(eventArgs));
            m_eventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                var prev = m_continuation ?? Interlocked.CompareExchange(
                    ref m_continuation, SENTINEL, null);
                if (prev != null)
                {
                    if (FScheduler != null)
                        Task.Factory.StartNew(prev, CancellationToken.None, TaskCreationOptions.None, FScheduler);
                    else
                        prev();
                }
            };
        }

        internal void Reset()
        {
            m_wasCompleted = false;
            m_continuation = null;
            FScheduler = SynchronizationContext.Current != null ? TaskScheduler.FromCurrentSynchronizationContext() : null;
        }

        public SocketAwaitable GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted { get { return m_wasCompleted; } }
        public void OnCompleted(Action continuation)
        {
            if (m_continuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref m_continuation, continuation, null) == SENTINEL)
            {
                if (FScheduler != null)
                    Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, FScheduler);
                else
                    Task.Run(continuation);
            }
        }
        public void GetResult()
        {
            if (m_eventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)m_eventArgs.SocketError);
        }
    }

    static class SocketExtensions
    {
        public static SocketAwaitable AcceptAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.AcceptAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable ConnectAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ConnectAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable DisconnectAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.DisconnectAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable ReceiveAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable ReceiveFromAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveFromAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable SendAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable SendToAsync(this NetSocket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendToAsync(awaitable.m_eventArgs))
                awaitable.m_wasCompleted = true;
            return awaitable;
        }
    }
}
