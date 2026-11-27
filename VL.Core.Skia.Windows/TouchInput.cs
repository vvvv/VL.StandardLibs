#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.Versioning;
using System.Windows.Forms;
using VL.Lang.PublicAPI;
using VL.Lib.IO.Notifications;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.POINTER_INPUT_TYPE;
using static Windows.Win32.UI.Input.Pointer.POINTER_FLAGS;

namespace VL.Skia;

/// <summary>
/// Processes the pointer/touch messages of a control and generates TouchNotifications.
/// Every TouchDown is followed by exactly one TouchUp and nothing after it: a contact which is lost instead of lifted ends with a canceled one.
/// </summary>
internal sealed class TouchMessageProcessor
{
    // The last notification of every contact that is down
    private readonly Dictionary<uint, TouchNotification> contacts = new();
    private readonly Subject<TouchNotification> notifications = new();

    public IObservable<TouchNotification> Notifications => notifications;

    [SupportedOSPlatform("Windows8.0")]
    public bool TryHandle(ref Message m, Control target)
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
                            var canceled = pi.pointerFlags.HasFlag(POINTER_FLAG_CANCELED);
                            var kind = canceled ? TouchNotificationKind.TouchUp : (uint)m.Msg switch
                            {
                                WM_POINTERDOWN => TouchNotificationKind.TouchDown,
                                WM_POINTERUP => TouchNotificationKind.TouchUp,
                                _ => TouchNotificationKind.TouchMove
                            };
                            // Only a contact that is down moves and ends
                            if (kind != TouchNotificationKind.TouchDown && !contacts.ContainsKey(pointerId))
                                return false;

                            var contactArea = touchInfo.rcContact.Size.ToVector2();
                            var pos = target.PointToClient(pi.ptPixelLocation).ToVector2();
                            var client = target.ClientSize.ToVector2();
                            var primary = pi.pointerFlags.HasFlag(POINTER_FLAG_PRIMARY);
                            System.Windows.Forms.Keys winFormsModifiers = System.Windows.Forms.Control.ModifierKeys;
                            var vlModifiers = winFormsModifiers.ToOurs();
                            var notification = new TouchNotification(kind, pos, client, (int)pi.pointerId, primary, contactArea, pi.sourceDevice, vlModifiers, target, canceled);
                            if (kind == TouchNotificationKind.TouchUp)
                                contacts.Remove(pointerId);
                            else
                                contacts[pointerId] = notification;
                            notifications.OnNext(notification);
                            if (notification.Handled)
                            {
                                m.Result = new IntPtr(1);
                                return true; // handled and consumed
                            }
                        }
                    }
                }
                catch (Exception e) when (IDevSession.Current is { } session)
                {
                    session.ReportException(e);
                }
                return false; // not handled (or not consumed)
            case WM_POINTERCAPTURECHANGED:
                // The system takes the contact away. No WM_POINTERUP follows.
                if (contacts.Remove(GET_POINTERID_WPARAM((ulong)m.WParam), out var lostContact))
                    Cancel(lostContact);
                return false;
        }
        return false; // not a touch message
    }

    /// <summary>
    /// Ends every contact that is down with a canceled TouchUp. To be called when the notifications of the control no longer reach their receivers.
    /// </summary>
    public void CancelAll()
    {
        var lostContacts = contacts.Values.ToArray();
        contacts.Clear();
        foreach (var lostContact in lostContacts)
            Cancel(lostContact);
    }

    private void Cancel(TouchNotification n)
    {
        try
        {
            notifications.OnNext(new TouchNotification(TouchNotificationKind.TouchUp, n.Position, n.ClientArea, n.Id, n.Primary, n.ContactArea, n.TouchDeviceID, n.ModifierKeys, n.Sender, canceled: true));
        }
        catch (Exception e) when (IDevSession.Current is { } session)
        {
            session.ReportException(e);
        }
    }

    static ushort LOWORD(ulong l) => (ushort)(l & 0xFFFF);
    static ushort GET_POINTERID_WPARAM(ulong wParam) => LOWORD(wParam);
}
