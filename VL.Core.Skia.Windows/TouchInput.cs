#nullable enable
using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using VL.Lib.IO.Notifications;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.POINTER_INPUT_TYPE;
using static Windows.Win32.UI.Input.Pointer.POINTER_FLAGS;

namespace VL.Skia;

/// <summary>
/// Shared helper that processes pointer/touch messages and generates TouchNotifications.
/// </summary>
internal static class TouchMessageProcessor
{
    [SupportedOSPlatform("Windows8.0")]
    public static bool TryHandle(ref Message m, Control target, IObserver<TouchNotification> sink)
    {
        switch ((uint)m.Msg)
        {
            case WM_POINTERDOWN:
            case WM_POINTERUPDATE:
            case WM_POINTERUP:
                try
                {
                    var pointerId = GET_POINTERID_WPARAM((ulong)m.WParam);
                    if (GetPointerType(pointerId, out var type) && type == PT_TOUCH)
                    {
                        if (GetPointerTouchInfo(pointerId, out var touchInfo))
                        {
                            var pi = touchInfo.pointerInfo;
                            var kind = (uint)m.Msg switch
                            {
                                WM_POINTERDOWN => TouchNotificationKind.TouchDown,
                                WM_POINTERUP => TouchNotificationKind.TouchUp,
                                _ => TouchNotificationKind.TouchMove
                            };
                            var contactArea = touchInfo.rcContact.Size.ToVector2();
                            var pos = target.PointToClient(pi.ptPixelLocation).ToVector2();
                            var client = target.ClientSize.ToVector2();
                            var primary = pi.pointerFlags.HasFlag(POINTER_FLAG_PRIMARY);
                            System.Windows.Forms.Keys winFormsModifiers = System.Windows.Forms.Control.ModifierKeys;
                            var vlModifiers = winFormsModifiers.ToOurs();
                            var notification = new TouchNotification(kind, pos, client, (int)pi.pointerId, primary, contactArea, pi.sourceDevice, vlModifiers, target);
                            sink.OnNext(notification);
                            if (notification.Handled)
                            {
                                m.Result = new IntPtr(1);
                                return true; // handled and consumed
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (VL.Lang.PublicAPI.IDevSession.Current != null)
                        VL.Lang.PublicAPI.IDevSession.Current.ReportException(e);
                    else
                        throw;
                }
                return false; // not handled (or not consumed)
        }
        return false; // not a touch message
    }

    static ushort LOWORD(ulong l) => (ushort)(l & 0xFFFF);
    static ushort GET_POINTERID_WPARAM(ulong wParam) => LOWORD(wParam);
}
