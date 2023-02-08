using System;
using System.Drawing;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VL.Lang.PublicAPI;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    partial class SkiaGLControl
    {
        private readonly Subject<TouchNotification> touchNotifications = new Subject<TouchNotification>();

        public IObservable<TouchNotification> TouchNotifications => touchNotifications;

        public bool TouchEnabled { get; set; }

        protected override void WndProc(ref Message m)
        {
            try
            {
                bool handled;
                switch (m.Msg)
                {
                    case Utils.WM_TOUCH:
                        handled = DecodeTouch(ref m);
                        break;
                    default:
                        handled = false;
                        break;
                }

                if (handled)
                    m.Result = new IntPtr(1);

                base.WndProc(ref m);
            }
            catch (Exception e)
            {
                // when running standalone as application there is no session, let the hosting environment deal with it
                if (IDevSession.Current != null)
                    IDevSession.Current.ReportException(e);
                else
                    throw;
            }
        }

        static int touchInputByteSize = Marshal.SizeOf(typeof(TOUCHINPUT));

        private bool DecodeTouch(ref Message m)
        {
            var inputCount = (m.WParam.ToInt32() & 0xffff);
            var touchPoints = new TOUCHINPUT[inputCount];

            if (!Utils.GetTouchInputInfo(m.LParam, inputCount, touchPoints, touchInputByteSize))
            {
                return false;
            }

            try
            {
                foreach (var touchPoint in touchPoints)
                {
                    TouchNotificationKind kind;
                    if ((touchPoint.dwFlags & Utils.TOUCHEVENTF_DOWN) != 0)
                        kind = TouchNotificationKind.TouchDown;
                    else if ((touchPoint.dwFlags & Utils.TOUCHEVENTF_UP) != 0)
                        kind = TouchNotificationKind.TouchUp;
                    else
                        kind = TouchNotificationKind.TouchMove;

                    var contactArea = (touchPoint.dwMask & Utils.TOUCHINPUTMASKF_CONTACTAREA) > 0
                        ? new Size(touchPoint.cxContact / 100, touchPoint.cyContact / 100)
                        : Size.Empty;

                    var notification = new TouchNotification(
                        kind,
                        position: PointToClient(new Point(touchPoint.x / 100, touchPoint.y / 100)).ToVector2(),
                        clientArea: ClientSize.ToVector2(),
                        id: touchPoint.dwID,
                        primary: (touchPoint.dwFlags & Utils.TOUCHEVENTF_PRIMARY) > 0,
                        contactArea: contactArea.ToVector2(),
                        touchDeviceID: touchPoint.hSource.ToInt64(),
                        modifierKeys: (VL.Lib.IO.Keys)ModifierKeys,
                        sender: this);

                    touchNotifications.OnNext(notification);
                }

                return true;
            }
            finally
            {
                Utils.CloseTouchInputHandle(m.LParam);
            }
        }
    }

    static class Utils
    {
        public const int WM_TOUCH = 0x0240;

        // Touch event flags ((TOUCHINPUT.dwFlags) [winuser.h]
        public const int TOUCHEVENTF_MOVE = 0x0001;
        public const int TOUCHEVENTF_DOWN = 0x0002;
        public const int TOUCHEVENTF_UP = 0x0004;
        public const int TOUCHEVENTF_INRANGE = 0x0008;
        public const int TOUCHEVENTF_PRIMARY = 0x0010;
        public const int TOUCHEVENTF_NOCOALESCE = 0x0020;
        public const int TOUCHEVENTF_PEN = 0x0040;

        // Touch input mask values (TOUCHINPUT.dwMask) [winuser.h]
        public const int TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001; // the dwTime field contains a system generated value
        public const int TOUCHINPUTMASKF_EXTRAINFO = 0x0002; // the dwExtraInfo field is valid
        public const int TOUCHINPUTMASKF_CONTACTAREA = 0x0004; // the cxContact and cyContact fields are valid


        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow(IntPtr hWnd, ulong ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTouchInputInfo(IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern void CloseTouchInputHandle(IntPtr lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TOUCHINPUT
    {
        public int x;
        public int y;
        public IntPtr hSource;
        public int dwID;
        public int dwFlags;
        public int dwMask;
        public int dwTime;
        public IntPtr dwExtraInfo;
        public int cxContact;
        public int cyContact;
    }
}
