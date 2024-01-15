using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib
{
    [SupportedOSPlatform("windows")]
    public static class HardwareChangedEvents
    {

        /// <summary>
        /// Checks for device additions, buffers multiple events in 2 second windows. Can fire multiple times
        /// </summary>
        public static IObservable<IReadOnlyList<ManagementBaseObject>> HardwareAdded { get; private set; }

        /// <summary>
        /// Checks for device removals, buffers multiple events in 2 second windows. Can fire multiple times
        /// </summary>
        public static IObservable<IReadOnlyList<ManagementBaseObject>> HardwareRemoved { get; private set; }

        /// <summary>
        /// Checks for device additions and removals, buffers multiple events in 2 second windows. Can fire multiple times
        /// </summary>
        public static IObservable<IReadOnlyList<ManagementBaseObject>> HardwareChanged { get; private set; }

        static HardwareChangedEvents()
        {
            //var queryStringAdded = "SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'";
            var queryStringAdded = "SELECT * FROM Win32_DeviceChangeEvent where EventType = 2";
            HardwareAdded = Observable.Using(() => CreateWatcher(queryStringAdded),
                watcherWrapper => Observable.FromEventPattern<EventArrivedEventHandler, EventArrivedEventArgs>(
                h => watcherWrapper.Watcher.EventArrived += h,
                h => watcherWrapper.Watcher.EventArrived -= h)
                .Delay(TimeSpan.FromSeconds(1.5)) //wait a bit for the usb device installer
                .Select(GetNewDeviceCreatedEvent))
                // Catch any exception, we don't want this class to bring down the whole app (https://discourse.vvvv.org/t/patch-throws-exception-error-on-loading-5-3-0234/22014)
                .Catch(Observable.Empty<ManagementBaseObject>())
                .Buffer(TimeSpan.FromSeconds(2))
                .Where(buffer => buffer.Count > 0)
                .Cast<IReadOnlyList<ManagementBaseObject>>()
                .Publish().RefCount();

            //var queryStringDeleted = "SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'";
            var queryStringDeleted = "SELECT * FROM Win32_DeviceChangeEvent where EventType = 3";
            HardwareRemoved = Observable.Using(() => CreateWatcher(queryStringDeleted),
                watcherWrapper => Observable.FromEventPattern<EventArrivedEventHandler, EventArrivedEventArgs>(
                h => watcherWrapper.Watcher.EventArrived += h,
                h => watcherWrapper.Watcher.EventArrived -= h)
                .Select(GetNewDeviceDeletedEvent))
                .Catch(Observable.Empty<ManagementBaseObject>())
                .Buffer(TimeSpan.FromSeconds(2))
                .Where(buffer => buffer.Count > 0)
                .Cast<IReadOnlyList<ManagementBaseObject>>()
                .Publish().RefCount();

            HardwareChanged = Observable.Merge(HardwareAdded, HardwareRemoved)
                .Publish().RefCount();

        }

        class WatcherWrapper : IDisposable
        { 
            public readonly ManagementEventWatcher Watcher;
            private bool disposed;

            public WatcherWrapper(ManagementEventWatcher watcher)
            {
                Watcher = watcher;
            }

            // Protected implementation of Dispose pattern.
            protected virtual void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                }

                // Free any unmanaged objects here.
                try
                {
                    //Watcher?.Stop();
                    Watcher?.Dispose();
                }
                catch (Exception)
                {
                }

                disposed = true;
            }

            public void Dispose()
            {               
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~WatcherWrapper()
            {
                Dispose(false);
            }
        }

        private static WatcherWrapper CreateWatcher(string queryString)
        {
            var query = new WqlEventQuery(queryString);
            var watcher = new ManagementEventWatcher(query);
            watcher.Start();        
            return new WatcherWrapper(watcher);
        }

        private static ManagementBaseObject GetNewDeviceCreatedEvent(EventPattern<EventArrivedEventArgs> e)
        {
            //wait for pending device installtions
            WaitNoPendingInstallEvents(WaitForInstallationTimeoutInMilliseconds);
            return e.EventArgs.NewEvent;
        }

        private static ManagementBaseObject GetNewDeviceDeletedEvent(EventPattern<EventArrivedEventArgs> e)
        {
            //just for setting breakpoints
            return e.EventArgs.NewEvent;
        }

        /// <summary>
        /// The wait for installation timeout in milliseconds, defaults to 1 minute
        /// </summary>
        public static uint WaitForInstallationTimeoutInMilliseconds = 60 * 1000;

        [DllImport("cfgmgr32.dll", SetLastError = true, EntryPoint = "CMP_WaitNoPendingInstallEvents", CharSet = CharSet.Auto)]
        public static extern uint WaitNoPendingInstallEvents(uint timeOutInMilliseconds);

        private const uint WAIT_OBJECT_0 = 0x0;
        private const uint WAIT_TIMEOUT = 0x102;
        private const uint WAIT_FAILED = 0xFFFFFFFF;

    }

    /// <summary>
    ///  Defines GUIDs for device classes used in Plug &amp; Play.
    /// </summary>
    public class GUID_DEVCLASS
    {
        public static readonly Guid GUID_DEVCLASS_1394 = new Guid("{0x6bdd1fc1, 0x810f, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_1394DEBUG = new Guid("{0x66f250d6, 0x7801, 0x4a64, {0xb1, 0x39, 0xee, 0xa8, 0x0a, 0x45, 0x0b, 0x24}}");
        public static readonly Guid GUID_DEVCLASS_61883 = new Guid("{0x7ebefbc0, 0x3200, 0x11d2, {0xb4, 0xc2, 0x00, 0xa0, 0xc9, 0x69, 0x7d, 0x07}}");
        public static readonly Guid GUID_DEVCLASS_ADAPTER = new Guid("{0x4d36e964, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_APMSUPPORT = new Guid("{0xd45b1c18, 0xc8fa, 0x11d1, {0x9f, 0x77, 0x00, 0x00, 0xf8, 0x05, 0xf5, 0x30}}");
        public static readonly Guid GUID_DEVCLASS_AVC = new Guid("{0xc06ff265, 0xae09, 0x48f0, {0x81, 0x2c, 0x16, 0x75, 0x3d, 0x7c, 0xba, 0x83}}");
        public static readonly Guid GUID_DEVCLASS_BATTERY = new Guid("{0x72631e54, 0x78a4, 0x11d0, {0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a}}");
        public static readonly Guid GUID_DEVCLASS_BIOMETRIC = new Guid("{0x53d29ef7, 0x377c, 0x4d14, {0x86, 0x4b, 0xeb, 0x3a, 0x85, 0x76, 0x93, 0x59}}");
        public static readonly Guid GUID_DEVCLASS_BLUETOOTH = new Guid("{0xe0cbf06c, 0xcd8b, 0x4647, {0xbb, 0x8a, 0x26, 0x3b, 0x43, 0xf0, 0xf9, 0x74}}");
        public static readonly Guid GUID_DEVCLASS_CDROM = new Guid("{0x4d36e965, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_COMPUTER = new Guid("{0x4d36e966, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_DECODER = new Guid("{0x6bdd1fc2, 0x810f, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_DISKDRIVE = new Guid("{0x4d36e967, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_DISPLAY = new Guid("{0x4d36e968, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_DOT4 = new Guid("{0x48721b56, 0x6795, 0x11d2, {0xb1, 0xa8, 0x00, 0x80, 0xc7, 0x2e, 0x74, 0xa2}}");
        public static readonly Guid GUID_DEVCLASS_DOT4PRINT = new Guid("{0x49ce6ac8, 0x6f86, 0x11d2, {0xb1, 0xe5, 0x00, 0x80, 0xc7, 0x2e, 0x74, 0xa2}}");
        public static readonly Guid GUID_DEVCLASS_ENUM1394 = new Guid("{0xc459df55, 0xdb08, 0x11d1, {0xb0, 0x09, 0x00, 0xa0, 0xc9, 0x08, 0x1f, 0xf6}}");
        public static readonly Guid GUID_DEVCLASS_FDC = new Guid("{0x4d36e969, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_FLOPPYDISK = new Guid("{0x4d36e980, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_GPS = new Guid("{0x6bdd1fc3, 0x810f, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_HDC = new Guid("{0x4d36e96a, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_HIDCLASS = new Guid("{0x745a17a0, 0x74d3, 0x11d0, {0xb6, 0xfe, 0x00, 0xa0, 0xc9, 0x0f, 0x57, 0xda}}");
        public static readonly Guid GUID_DEVCLASS_IMAGE = new Guid("{0x6bdd1fc6, 0x810f, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_INFINIBAND = new Guid("{0x30ef7132, 0xd858, 0x4a0c, {0xac, 0x24, 0xb9, 0x02, 0x8a, 0x5c, 0xca, 0x3f}}");
        public static readonly Guid GUID_DEVCLASS_INFRARED = new Guid("{0x6bdd1fc5, 0x810f, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_KEYBOARD = new Guid("{0x4d36e96b, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_LEGACYDRIVER = new Guid("{0x8ecc055d, 0x047f, 0x11d1, {0xa5, 0x37, 0x00, 0x00, 0xf8, 0x75, 0x3e, 0xd1}}");
        public static readonly Guid GUID_DEVCLASS_MEDIA = new Guid("{0x4d36e96c, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MEDIUM_CHANGER = new Guid("{0xce5939ae, 0xebde, 0x11d0, {0xb1, 0x81, 0x00, 0x00, 0xf8, 0x75, 0x3e, 0xc4}}");
        public static readonly Guid GUID_DEVCLASS_MODEM = new Guid("{0x4d36e96d, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MONITOR = new Guid("{0x4d36e96e, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MOUSE = new Guid("{0x4d36e96f, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MTD = new Guid("{0x4d36e970, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MULTIFUNCTION = new Guid("{0x4d36e971, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_MULTIPORTSERIAL = new Guid("{0x50906cb8, 0xba12, 0x11d1, {0xbf, 0x5d, 0x00, 0x00, 0xf8, 0x05, 0xf5, 0x30}}");
        public static readonly Guid GUID_DEVCLASS_NET = new Guid("{0x4d36e972, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_NETCLIENT = new Guid("{0x4d36e973, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_NETSERVICE = new Guid("{0x4d36e974, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_NETTRANS = new Guid("{0x4d36e975, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_NODRIVER = new Guid("{0x4d36e976, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_PCMCIA = new Guid("{0x4d36e977, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_PNPPRINTERS = new Guid("{0x4658ee7e, 0xf050, 0x11d1, {0xb6, 0xbd, 0x00, 0xc0, 0x4f, 0xa3, 0x72, 0xa7}}");
        public static readonly Guid GUID_DEVCLASS_PORTS = new Guid("{0x4d36e978, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_PRINTER = new Guid("{0x4d36e979, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_PRINTERUPGRADE = new Guid("{0x4d36e97a, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_PROCESSOR = new Guid("{0x50127dc3, 0x0f36, 0x415e, {0xa6, 0xcc, 0x4c, 0xb3, 0xbe, 0x91, 0x0B, 0x65}}");
        public static readonly Guid GUID_DEVCLASS_SBP2 = new Guid("{0xd48179be, 0xec20, 0x11d1, {0xb6, 0xb8, 0x00, 0xc0, 0x4f, 0xa3, 0x72, 0xa7}}");
        public static readonly Guid GUID_DEVCLASS_SCSIADAPTER = new Guid("{0x4d36e97b, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_SECURITYACCELERATOR = new Guid("{0x268c95a1, 0xedfe, 0x11d3, {0x95, 0xc3, 0x00, 0x10, 0xdc, 0x40, 0x50, 0xa5}}");
        public static readonly Guid GUID_DEVCLASS_SMARTCARDREADER = new Guid("{0x50dd5230, 0xba8a, 0x11d1, {0xbf, 0x5d, 0x00, 0x00, 0xf8, 0x05, 0xf5, 0x30}}");
        public static readonly Guid GUID_DEVCLASS_SOUND = new Guid("{0x4d36e97c, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_SYSTEM = new Guid("{0x4d36e97d, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_TAPEDRIVE = new Guid("{0x6d807884, 0x7d21, 0x11cf, {0x80, 0x1c, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_UNKNOWN = new Guid("{0x4d36e97e, 0xe325, 0x11ce, {0xbf, 0xc1, 0x08, 0x00, 0x2b, 0xe1, 0x03, 0x18}}");
        public static readonly Guid GUID_DEVCLASS_USB = new Guid("{0x36fc9e60, 0xc465, 0x11cf, {0x80, 0x56, 0x44, 0x45, 0x53, 0x54, 0x00, 0x00}}");
        public static readonly Guid GUID_DEVCLASS_VOLUME = new Guid("{0x71a27cdd, 0x812a, 0x11d0, {0xbe, 0xc7, 0x08, 0x00, 0x2b, 0xe2, 0x09, 0x2f}}");
        public static readonly Guid GUID_DEVCLASS_VOLUMESNAPSHOT = new Guid("{0x533c5b84, 0xec70, 0x11d2, {0x95, 0x05, 0x00, 0xc0, 0x4f, 0x79, 0xde, 0xaf}}");
        public static readonly Guid GUID_DEVCLASS_WCEUSBS = new Guid("{0x25dbce51, 0x6c8f, 0x4a72, {0x8a, 0x6d, 0xb5, 0x4c, 0x2b, 0x4f, 0xc8, 0x35}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_ACTIVITYMONITOR = new Guid("{0xb86dff51, 0xa31e, 0x4bac, {0xb3, 0xcf, 0xe8, 0xcf, 0xe7, 0x5c, 0x9f, 0xc2}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_UNDELETE = new Guid("{0xfe8f1572, 0xc67a, 0x48c0, {0xbb, 0xac, 0x0b, 0x5c, 0x6d, 0x66, 0xca, 0xfb}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_ANTIVIRUS = new Guid("{0xb1d1a169, 0xc54f, 0x4379, {0x81, 0xdb, 0xbe, 0xe7, 0xd8, 0x8d, 0x74, 0x54}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_REPLICATION = new Guid("{0x48d3ebc4, 0x4cf8, 0x48ff, {0xb8, 0x69, 0x9c, 0x68, 0xad, 0x42, 0xeb, 0x9f}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_CONTINUOUSBACKUP = new Guid("{0x71aa14f8, 0x6fad, 0x4622, {0xad, 0x77, 0x92, 0xbb, 0x9d, 0x7e, 0x69, 0x47}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_CONTENTSCREENER = new Guid("{0x3e3f0674, 0xc83c, 0x4558, {0xbb, 0x26, 0x98, 0x20, 0xe1, 0xeb, 0xa5, 0xc5}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_QUOTAMANAGEMENT = new Guid("{0x8503c911, 0xa6c7, 0x4919, {0x8f, 0x79, 0x50, 0x28, 0xf5, 0x86, 0x6b, 0x0c}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_SYSTEMRECOVERY = new Guid("{0x2db15374, 0x706e, 0x4131, {0xa0, 0xc7, 0xd7, 0xc7, 0x8e, 0xb0, 0x28, 0x9a}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_CFSMETADATASERVER = new Guid("{0xcdcf0939, 0xb75b, 0x4630, {0xbf, 0x76, 0x80, 0xf7, 0xba, 0x65, 0x58, 0x84}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_HSM = new Guid("{0xd546500a, 0x2aeb, 0x45f6, {0x94, 0x82, 0xf4, 0xb1, 0x79, 0x9c, 0x31, 0x77}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_COMPRESSION = new Guid("{0xf3586baf, 0xb5aa, 0x49b5, {0x8d, 0x6c, 0x05, 0x69, 0x28, 0x4c, 0x63, 0x9f}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_ENCRYPTION = new Guid("{0xa0a701c0, 0xa511, 0x42ff, {0xaa, 0x6c, 0x06, 0xdc, 0x03, 0x95, 0x57, 0x6f}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_PHYSICALQUOTAMANAGEMENT = new Guid("{0x6a0a8e78, 0xbba6, 0x4fc4, {0xa7, 0x09, 0x1e, 0x33, 0xcd, 0x09, 0xd6, 0x7e}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_OPENFILEBACKUP = new Guid("{0xf8ecafa6, 0x66d1, 0x41a5, {0x89, 0x9b, 0x66, 0x58, 0x5d, 0x72, 0x16, 0xb7}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_SECURITYENHANCER = new Guid("{0xd02bc3da, 0x0c8e, 0x4945, {0x9b, 0xd5, 0xf1, 0x88, 0x3c, 0x22, 0x6c, 0x8c}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_COPYPROTECTION = new Guid("{0x89786ff1, 0x9c12, 0x402f, {0x9c, 0x9e, 0x17, 0x75, 0x3c, 0x7f, 0x43, 0x75}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_SYSTEM = new Guid("{0x5d1b9aaa, 0x01e2, 0x46af, {0x84, 0x9f, 0x27, 0x2b, 0x3f, 0x32, 0x4c, 0x46}}");
        public static readonly Guid GUID_DEVCLASS_FSFILTER_INFRASTRUCTURE = new Guid("{0xe55fa6f9, 0x128c, 0x4d04, {0xab, 0xab, 0x63, 0x0c, 0x74, 0xb1, 0x45, 0x3a}}");
    }
}
