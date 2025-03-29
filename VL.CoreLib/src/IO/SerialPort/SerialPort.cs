using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Reactive.Linq;
using VL.Core.Import;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using SerialPort = VL.Lib.IO.Ports.SerialPort;
using NetSerialPort = System.IO.Ports.SerialPort;

[assembly: ImportType(typeof(SerialPort), Category = "IO.Ports")]

namespace VL.Lib.IO.Ports
{
    #region port enum
    [Serializable]
    public class ComPort : DynamicEnumBase<ComPort, ComPortDefinition>
    {
        public ComPort(string value)
            : base(value)
        { }

        public static ComPort CreateDefault()
            => CreateDefaultBase("No COM port found");
    }

    public class ComPortDefinition : DynamicEnumDefinitionBase<ComPortDefinition>
    {
        protected override IObservable<object> GetEntriesChangedObservable()
        {
            if (OperatingSystem.IsWindows())
                return HardwareChangedEvents.HardwareChanged;
            return Observable.Empty<object>();
        }

        //actual work
        protected override IReadOnlyDictionary<string, object> GetEntries()
        {
            Dictionary<string, object> portNames = new Dictionary<string, object>();

            try
            {
                foreach (var portName in NetSerialPort.GetPortNames()
                .Where(n => n.StartsWith("com", StringComparison.InvariantCultureIgnoreCase)))
                {
                    portNames[portName] = portName;
                }
            }
            catch (Exception)
            {
                //TryCatch is introduced in the hope to fix
                //the issue #5089 as discussed with Joreg
            }
            return portNames;
        }
    }
    #endregion

    /// <summary>
    /// Manages a serialport provider.
    /// </summary>
    [ProcessNode]
    public class SerialPort
    {
        ResourceProviderMonitor<NetSerialPort> FCurrentProvider;
        string FPortName;
        int FBaudRate;
        int FDataBits;
        StopBits FStopBits;
        Parity FParity;
        Handshake FHandshake;
        bool FDtrEnable;
        bool FRtsEnable;
        bool FBreakState;

        /// <summary>
        /// Configures the internally managed serialport provider.
        /// </summary>
        /// <returns>A serialport provider which can be used by multiple threads in parallel.</returns>
        public IResourceProvider<NetSerialPort> Update(ComPort portName, int baudrate, int dataBits, StopBits stopBits, Parity parity, Handshake handshake, bool dtrEnable, bool rtsEnable, bool breakState, bool open)
        {
            if (portName.Value != FPortName || baudrate != FBaudRate || dataBits != FDataBits || stopBits != FStopBits || parity != FParity || handshake != FHandshake || dtrEnable != FDtrEnable || rtsEnable != FRtsEnable || breakState != FBreakState)
            {
                FPortName = portName.Value;
                FBaudRate = baudrate;
                FDataBits = dataBits;
                FStopBits = stopBits;
                FParity = parity;
                FHandshake = handshake;
                FDtrEnable = dtrEnable;
                FRtsEnable = rtsEnable;
                FBreakState = breakState;

                FCurrentProvider = ResourceProvider.New(() =>
                {
                    NetSerialPort port = null;
                    if (portName.IsValid())
                    {
                        port = new NetSerialPort(FPortName, FBaudRate, FParity, FDataBits, FStopBits);
                        port.Handshake = FHandshake;
                        port.DtrEnable = FDtrEnable;
                        port.RtsEnable = FRtsEnable;

                        port.Open();

                        try
                        {
                            if (port.IsOpen)
                                port.BreakState = FBreakState;
                        }
                        catch (Exception)
                        {
                            //had to catch here since it would crash here e.g. with the AxiDraw plotter
                            //shouldn't be an issue though, just didn't set the breakstate which didn't need to be set anyway
                        }
                    }

                    return port;
                }, port => port?.Close()).ShareInParallel().Monitor();
            }

            if (open)
                return FCurrentProvider;
            return null;
        }

        /// <summary>
        /// Whether or not the serialport is connected.
        /// </summary>
        public bool IsOpen => FCurrentProvider?.ResourcesUsedBySinks.FirstOrDefault()?.IsOpen ?? false;
    }
}
