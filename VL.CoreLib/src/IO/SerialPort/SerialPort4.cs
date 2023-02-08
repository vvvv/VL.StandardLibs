using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using VL.Lib.Collections;
using VL.Lib.Threading;
using NetSerialPort = System.IO.Ports.SerialPort;

namespace VL.Lib.IO.Ports
{
    //move com port enum here from SerialPort.cs

    public class SerialPort4: IDisposable
    {
        NetSerialPort FPort;
        Task FCurrentTask;
        CancellationTokenSource FCancellation = new CancellationTokenSource();
        
        readonly Subject<ArraySegment<byte>> FOutput = new Subject<ArraySegment<byte>>();
        public IObservable<ArraySegment<byte>> Output => FOutput;

        /// <summary>
        /// Whether or not the serialport is connected.
        /// </summary>
        public bool IsOpen => FPort?.IsOpen ?? false;

        public void OpenPort(ComPort portName, int baudrate, int dataBits, StopBits stopBits, Parity parity, Handshake handshake, bool dtrEnable, bool rtsEnable, bool breakState)
        {
            if (FPort != null)
                ClosePort(1);

            if (portName.IsValid())
            {
                FPort = new NetSerialPort(portName.Value, baudrate, parity, dataBits, stopBits);
                FPort.Handshake = handshake;
                FPort.DtrEnable = dtrEnable;
                FPort.RtsEnable = rtsEnable;

                FPort.Open();

                try
                {
                    if (FPort.IsOpen)
                        FPort.BreakState = breakState;
                }
                catch (Exception)
                {
                    //had to catch here since it would crash here e.g. with the AxiDraw plotter
                    //shouldn't be an issue though, just didn't set the breakstate which didn't need to be set anyway
                }

                FCancellation = new CancellationTokenSource();
                var token = FCancellation.Token;
                FCurrentTask = Task.Run(async () =>
                {
                    var buffer = new byte[2048];
                    while (!token.IsCancellationRequested)
                    {
                        var port = FPort;
                        if (port == null)
                            return;

                        try
                        {
                            var stream = port.BaseStream;
                            while (!token.IsCancellationRequested)
                            {
                                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                                if (bytesRead > 0)
                                {
                                    var data = new ArraySegment<byte>(buffer, 0, bytesRead);
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
                }, token);
            }
        }

        public void ClosePort(int timeout)
        {
            FCurrentTask?.CancelAndDispose(FCancellation, timeout);
            FCurrentTask = null;

            FPort.Close();
            FPort.Dispose();
            FPort = null;
        }

        public void SendData(IReadOnlyList<byte> data)
        {
            if (IsOpen)
            {
                if (data is byte[])
                {
                    FPort.Write(data as byte[], 0, data.Count);
                }
                else
                {
                    if (data.Count > FSendBuffer.Length)
                        FSendBuffer = new byte[data.Count];

                    for (int i = 0; i < data.Count; i++)
                        FSendBuffer[i] = data[i];

                    FPort.Write(FSendBuffer, 0, data.Count);
                }
            }
        }
        byte[] FSendBuffer = Array.Empty<byte>();

        public void SendData(string text, string terminator = "\r\n")
        {
            if (IsOpen)
                FPort.Write(text + terminator);
        }

        public void Dispose()
        {
            ClosePort(10);
        }
    }
}
