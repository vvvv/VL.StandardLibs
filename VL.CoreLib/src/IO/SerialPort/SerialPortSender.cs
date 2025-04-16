using System;
using System.IO.Ports;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using NetSerialPort = System.IO.Ports.SerialPort;
using VL.Lib.Threading;
using VL.Core;
using VL.Core.Import;
using VL.Lib.IO.Ports;

[assembly: ImportType(typeof(Sender), Category = "IO.Ports")]

namespace VL.Lib.IO.Ports
{
    /// <summary>
    /// Sends bytes on a SerialPort.
    /// </summary>
    [ProcessNode]
    public class Sender : IDisposable
    {
        object FData;
        object FSerialPortProvider;
        Task FCurrentTask;
        CancellationTokenSource FCancellation = new CancellationTokenSource();

        public Sender(NodeContext nodeContext)
        {
        }

        /// <summary>
        /// Configures the sender.
        /// </summary>
        /// <param name="port">The serialport to send data to.</param>
        /// <param name="data">The bytes to send.</param>
        public void Update(IResourceProvider<NetSerialPort> port, IObservable<Spread<byte>> data)
        {
            if (port != FSerialPortProvider || data != FData)
            {
                FSerialPortProvider = port;
                FData = data;
                Stop(0);
                if (port != null)
                    Start(port, data);
            }
        }

        void Start(IResourceProvider<NetSerialPort> provider, IObservable<Spread<byte>> input)
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

                        var stream = port.BaseStream;
                        foreach (var data in input.ToEnumerable())
                        {
                            if (token.IsCancellationRequested)
                                break;

                            try
                            {
                                var sentBytes = 0;
                                while (sentBytes < data.Count)
                                {
                                    var bytesToSend = Math.Min(buffer.Length, data.Count - sentBytes);
                                    data.CopyTo(sentBytes, buffer, 0, bytesToSend);
                                    await stream.WriteAsync(buffer, 0, bytesToSend, token);
                                    sentBytes += bytesToSend;
                                }
                            }
                            catch (Exception e)
                            {
                                RuntimeGraph.ReportException(e);
                                break;
                            }
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
