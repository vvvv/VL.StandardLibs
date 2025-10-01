using System;
using System.IO.Ports;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.IO.Ports;
using VL.Lib.Threading;
using NetSerialPort = System.IO.Ports.SerialPort;

[assembly: ImportType(typeof(Receiver), Category = "IO.Ports")]

namespace VL.Lib.IO.Ports
{
    /// <summary>
    /// Receives bytes from a SerialPort.
    /// </summary>
    [ProcessNode]
    public class Receiver : IDisposable
    {
        IResourceProvider<NetSerialPort> FSerialPortProvider;
        Task FCurrentTask;
        CancellationTokenSource FCancellation = new CancellationTokenSource();
        readonly Subject<Spread<byte>> FOutput = new Subject<Spread<byte>>();

        /// <summary>
        /// Configures the receiver.
        /// </summary>
        /// <param name="port">The serialport to receive data from.</param>
        public void Update(IResourceProvider<NetSerialPort> port)
        {
            if (port != FSerialPortProvider)
            {
                FSerialPortProvider = port;
                Stop(0);
                if (port != null)
                    Start(port);
            }
        }

        /// <summary>
        /// The observable sequence of bytes. The bytes will be pushed on the network thread.
        /// </summary>
        public IObservable<Spread<byte>> Data => FOutput;

        void Start(IResourceProvider<NetSerialPort> provider)
        {
            FCancellation = new CancellationTokenSource();
            var token = FCancellation.Token;
            FCurrentTask = Task.Run(async () =>
            {
                var buffer = new byte[2048];
                while (!token.IsCancellationRequested)
                {
                    using (var handle = await provider.GetHandleAsync(token, 100))
                    {
                        var port = handle.Resource;
                        if (port == null)
                            return;

                        // Return the handle on cancellation
                        token.Register(handle.Dispose);

                        try
                        {
                            var stream = port.BaseStream;
                            while (!token.IsCancellationRequested)
                            {
                                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                                if (bytesRead > 0)
                                {
                                    var data = Spread.Create(buffer, 0, bytesRead);
                                    FOutput.OnNext(data);
                                }
                                else
                                    break; //connection lost?

                            }
                        }
                        catch (Exception)
                        {
                            // Try again
                            await Task.Delay(100);
                            continue;
                        }
                    }
                }
            }, token);
        }

        private void Stop(int timeout)
        {
            FCurrentTask?.CancelAndDispose(FCancellation, timeout);
            FCurrentTask = null;
        }

        void IDisposable.Dispose()
        {
            Stop(1);
        }
    }
}
