using System;
using System.Drawing;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;
using VL.Lang.PublicAPI;
using VL.Lib.IO.Notifications;

namespace VL.Skia
{
    partial class SkiaGLControl
    {
        private readonly Subject<TouchNotification> touchNotifications = new Subject<TouchNotification>();

        public IObservable<TouchNotification> TouchNotifications => touchNotifications;

        protected override void WndProc(ref Message m)
        {
            try
            {
                bool handled;
                switch (m.Msg)
                {
                    // https://devblogs.microsoft.com/oldnewthing/20210728-00/?p=105487
                    case Utils.WM_POINTERDOWN:
                    case Utils.WM_POINTERUPDATE:
                    case Utils.WM_POINTERUP:
                        var pointerId = Utils.GET_POINTERID_WPARAM((ulong)m.WParam);
                        if (Utils.GetPointerType(pointerId, out var type) && type == Utils.PointerInputType.PT_TOUCH)
                        {
                            Utils.GetPointerTouchInfo(pointerId, out var touchInfo);
                            var pointerInfo = touchInfo.pointerInfo;

                            TouchNotificationKind kind;
                            if (m.Msg == Utils.WM_POINTERDOWN)
                                kind = TouchNotificationKind.TouchDown;
                            else if (m.Msg == Utils.WM_POINTERUP)
                                kind = TouchNotificationKind.TouchUp;
                            else
                                kind = TouchNotificationKind.TouchMove;


                            var contactArea = new Size(touchInfo.rcContactRight - touchInfo.rcContactLeft, touchInfo.rcContactBottom - touchInfo.rcContactTop);

                            var notification = new TouchNotification(
                                kind,
                                position: PointToClient(new Point(pointerInfo.ptPixelLocationX, pointerInfo.ptPixelLocationY)).ToVector2(),
                                clientArea: ClientSize.ToVector2(),
                                id: (int)pointerInfo.pointerId,
                                primary: pointerInfo.pointerFlags.HasFlag(Utils.PointerFlags.POINTER_FLAG_PRIMARY),
                                contactArea: contactArea.ToVector2(),
                                touchDeviceID: pointerInfo.sourceDevice.ToInt64(),
                                modifierKeys: (VL.Lib.IO.Keys)ModifierKeys,
                                sender: this);

                            touchNotifications.OnNext(notification);

                            handled = notification.Handled;
                        }
                        else
                            handled = false;
                        break;
                    default:
                        handled = false;
                        break;
                }

                if (handled)
                    m.Result = new IntPtr(1);
                else
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
    }

    static class Utils
    {
        public const int WM_POINTERDOWN = 0x0246;
        public const int WM_POINTERUP = 0x0247;
        public const int WM_POINTERUPDATE = 0x0245;
        public const int WM_POINTERWHEEL = 0x024E;
        public const int WM_POINTERHWHEEL = 0x024F;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerType(uint pointerId, out PointerInputType pointerType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetPointerTouchInfo(uint pointerId, out POINTER_TOUCH_INFO touchInfo);

        [Flags]
        public enum TouchFlags
        {
            TOUCH_FLAG_NONE = 0x00000000
        }

        [Flags]
        public enum TouchMask
        {
            TOUCH_MASK_NONE = 0x00000000,
            TOUCH_MASK_CONTACTAREA = 0x00000001,
            TOUCH_MASK_ORIENTATION = 0x00000002,
            TOUCH_MASK_PRESSURE = 0x00000004,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public TouchFlags touchFlags;
            public TouchMask touchMask;
            public int rcContactLeft;
            public int rcContactTop;
            public int rcContactRight;
            public int rcContactBottom;
            public int rcContactRawLeft;
            public int rcContactRawTop;
            public int rcContactRawRight;
            public int rcContactRawBottom;
            public uint orientation;
            public uint pressure;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINTER_INFO
        {
            public PointerInputType pointerType;
            public uint pointerId;
            public uint frameId;
            public PointerFlags pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public int ptPixelLocationX;
            public int ptPixelLocationY;
            public int ptHimetricLocationX;
            public int ptHimetricLocationY;
            public int ptPixelLocationRawX;
            public int ptPixelLocationRawY;
            public int ptHimetricLocationRawX;
            public int ptHimetricLocationRawY;
            public uint dwTime;
            public uint historyCount;
            public int inputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public PointerButtonChangeType ButtonChangeType;
        }

        public enum PointerInputType
        {
            PT_NONE = 0x00000000,
            PT_POINTER = 0x00000001,
            PT_TOUCH = 0x00000002,
            PT_PEN = 0x00000003,
            PT_MOUSE = 0x00000004,
            PT_TOUCHPAD = 0x00000005
        }

        [Flags]
        public enum PointerFlags
        {
            POINTER_FLAG_NONE = 0x00000000,
            POINTER_FLAG_NEW = 0x00000001,
            POINTER_FLAG_INRANGE = 0x00000002,
            POINTER_FLAG_INCONTACT = 0x00000004,
            POINTER_FLAG_FIRSTBUTTON = 0x00000010,
            POINTER_FLAG_SECONDBUTTON = 0x00000020,
            POINTER_FLAG_THIRDBUTTON = 0x00000040,
            POINTER_FLAG_FOURTHBUTTON = 0x00000080,
            POINTER_FLAG_FIFTHBUTTON = 0x00000100,
            POINTER_FLAG_PRIMARY = 0x00002000,
            POINTER_FLAG_CONFIDENCE = 0x00000400,
            POINTER_FLAG_CANCELED = 0x00000800,
            POINTER_FLAG_DOWN = 0x00010000,
            POINTER_FLAG_UPDATE = 0x00020000,
            POINTER_FLAG_UP = 0x00040000,
            POINTER_FLAG_WHEEL = 0x00080000,
            POINTER_FLAG_HWHEEL = 0x00100000,
            POINTER_FLAG_CAPTURECHANGED = 0x00200000,
            POINTER_FLAG_HASTRANSFORM = 0x00400000
        }

        public enum PointerButtonChangeType : ulong
        {
            POINTER_CHANGE_NONE,
            POINTER_CHANGE_FIRSTBUTTON_DOWN,
            POINTER_CHANGE_FIRSTBUTTON_UP,
            POINTER_CHANGE_SECONDBUTTON_DOWN,
            POINTER_CHANGE_SECONDBUTTON_UP,
            POINTER_CHANGE_THIRDBUTTON_DOWN,
            POINTER_CHANGE_THIRDBUTTON_UP,
            POINTER_CHANGE_FOURTHBUTTON_DOWN,
            POINTER_CHANGE_FOURTHBUTTON_UP,
            POINTER_CHANGE_FIFTHBUTTON_DOWN,
            POINTER_CHANGE_FIFTHBUTTON_UP
        }

        public static ushort LOWORD(ulong l) { return (ushort)(l & 0xFFFF); }
        public static ushort HIWORD(ulong l) { return (ushort)((l >> 16) & 0xFFFF); }
        public static ushort GET_POINTERID_WPARAM(ulong wParam) { return LOWORD(wParam); }
        public static ushort GET_X_LPARAM(ulong lp) { return LOWORD(lp); }
        public static ushort GET_Y_LPARAM(ulong lp) { return HIWORD(lp); }
    }
}
