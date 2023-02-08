using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace VL.Lib.IO.Socket
{
    public class WebSocketServer<TState> : IDisposable
    {
        public class Behavior : WebSocketBehavior
        {
            private readonly Timer FTimer;
            private readonly Func<Behavior, TState> FCreate;
            private readonly Func<TState, MessageEventArgs, TState> FUpdate;
            private readonly Action<TState> FDispose;
            private TState FState;
            private DateTime FLastMessage = DateTime.Now;

            public Behavior(int timeout, Func<Behavior, TState> create, Func<TState, MessageEventArgs, TState> update, Action<TState> dispose)
            {
                FCreate = create;
                FUpdate = update;
                FDispose = dispose;
                if (timeout > 0)
                {
                    FTimer = new Timer(_ =>
                    {
                        var elapsedTime = DateTime.Now - FLastMessage;
                        if (elapsedTime > TimeSpan.FromSeconds(timeout))
                            Sessions.CloseSession(ID);
                    }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(timeout));
                }
            }

            public void SendString(string data) => base.Send(data);
            public new void Send(byte[] data) => base.Send(data);
            public new void SendAsync(byte[] data, Action<bool> completed) => base.SendAsync(data, completed);

            protected override void OnOpen()
            {
                FState = FCreate(this);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                FLastMessage = DateTime.Now;
                FState = FUpdate.Invoke(FState, e);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                FTimer?.Dispose();
                FDispose(FState);
                base.OnClose(e);
            }
        }

        readonly WebSocketServer FServer;

        public WebSocketServer(
            string url,
            string path, 
            int timeout,
            Func<Behavior, TState> create, 
            Func<TState, MessageEventArgs, TState> update,
            Action<TState> dispose)
        {
            FServer = new WebSocketServer(url)
            {
                KeepClean = true,
            };
            FServer.AddWebSocketService(path, () => new Behavior(timeout, create, update, dispose));
            FServer.Start();
        }

        public void Dispose()
        {
            FServer.Stop();
        }
    }
}
