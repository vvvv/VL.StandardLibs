#nullable enable

using System;
using System.Drawing;
using System.Reactive.Disposables;
using System.Windows.Forms;
using VL.Lang.PublicAPI;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using static Windows.Win32.UI.WindowsAndMessaging.MENU_ITEM_FLAGS;
using static Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;
using static Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;

namespace VL.Core.Windows
{
    /// <summary>
    /// Provides Win32 custom title bar functionality for Windows Forms controls.
    /// This class handles the non-client area extension into the title bar and button hit testing,
    /// but delegates the actual drawing to a separate renderer interface.
    /// Port of https://github.com/grassator/win32-window-custom-titlebar
    /// </summary>
    public class Win32CustomTitleBar
    {
        public readonly record struct Options(bool AlwaysOnTop, bool ExtendIntoTitleBar, Func<bool>? IsFullscreen = default);

        public static Win32CustomTitleBar Install(Form form, NodeContext nodeContext, Options options)
        {
            var customTitleBar = new Win32CustomTitleBar(form)
            {
                ExtendIntoTitleBar = options.ExtendIntoTitleBar,
                IsFullScreen = options.IsFullscreen ?? (() => false)
            };
            var session = IDevSession.Current;
            if (session != null)
            {
                customTitleBar.MenuToggled += (s, e) =>
                {
                    if (e == ID_TOGGLE_TOPMOST)
                        session.CurrentSolution
                            .SetPinValue(nodeContext.Path.Stack, "Always On Top", form.TopMost)
                            .Confirm(VL.Model.SolutionUpdateKind.DontCompile);
                    if (e == ID_TOGGLE_EXTEND_INTO_TITLEBAR)
                        session.CurrentSolution
                            .SetPinValue(nodeContext.Path.Stack, "Extend Into Title Bar", customTitleBar.ExtendIntoTitleBar)
                            .Confirm(VL.Model.SolutionUpdateKind.DontCompile);
                };
            }
            form.TopMost = options.AlwaysOnTop;
            return customTitleBar;
        }

        private readonly Form form;
        private bool disposed;

        // Win32 custom title bar related fields
        private Rectangle closeButtonRect;
        private Rectangle maximizeButtonRect;
        private Rectangle minimizeButtonRect;
        private bool closeButtonHovered = false;
        private bool maximizeButtonHovered = false;
        private bool minimizeButtonHovered = false;

        // Custom menu id for toggling top-most state (must not clash with SC_* which are 0xF000+)
        internal const uint ID_TOGGLE_TOPMOST = 0x100;
        internal const uint ID_TOGGLE_EXTEND_INTO_TITLEBAR = 0x101;

        /// <summary>
        /// Whether we want to extend the client area into the title bar. Can be changed at runtime.
        /// </summary>
        private bool extendIntoTitleBar = false;

        public Win32CustomTitleBar(Form form)
        {
            this.form = form ?? throw new ArgumentNullException(nameof(form));
            form.HandleCreated += (s, e) =>
            {
                InitializeCustomMenu();
                UpdateTitleBarButtonRects();
            };
            form.Resize += (s, e) => UpdateTitleBarButtonRects();
        }

        /// <summary>
        /// Enables custom drawing and handling of the title bar (extending the client area into the standard non-client area).
        /// Automatically ignored while the form is in fullscreen (borderless) mode.
        /// </summary>
        public bool ExtendIntoTitleBar
        {
            get => extendIntoTitleBar;
            set
            {
                if (extendIntoTitleBar != value)
                {
                    extendIntoTitleBar = value;
                    // Force a non-client area recalculation if handle exists
                    if (form.IsHandleCreated)
                    {
                        var handle = (HWND)form.Handle;
                        SetWindowPos(handle, default, 0, 0, 0, 0,
                            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
                        form.Invalidate();
                    }
                }
            }
        }

        /// <summary>
        /// Event fired when custom menu items are toggled
        /// </summary>
        public event EventHandler<uint>? MenuToggled;

        /// <summary>
        /// Gets whether the form is currently in fullscreen mode.
        /// This should be provided by the form implementation.
        /// </summary>
        public Func<bool> IsFullScreen { get; set; } = () => false;

        // Convenience effective flag (disabled when fullscreen)
        private bool ShouldExtendIntoTitleBar => extendIntoTitleBar && !IsFullScreen();

        /// <summary>
        /// Process Windows messages. Call this from the form's WndProc override.
        /// </summary>
        public unsafe bool ProcessMessage(ref Message m)
        {
            if (disposed)
                return false;

            var handle = (HWND)m.HWnd;
            var w_param = (WPARAM)(nuint)m.WParam;
            var l_param = m.LParam;
            var message = (uint)m.Msg;

            switch (message)
            {
                case WM_SYSCOMMAND:
                    {
                        if (m.WParam == ID_TOGGLE_TOPMOST)
                        {
                            form.TopMost = !form.TopMost;
                            ToggleMenu(ID_TOGGLE_TOPMOST, form.TopMost);
                            return true;
                        }
                        else if (m.WParam == ID_TOGGLE_EXTEND_INTO_TITLEBAR)
                        {
                            ExtendIntoTitleBar = !ExtendIntoTitleBar;
                            ToggleMenu(ID_TOGGLE_EXTEND_INTO_TITLEBAR, ExtendIntoTitleBar);
                            return true;
                        }
                        break;
                    }
            }

            // If we don't extend into title bar just fall back to default behavior
            if (!ShouldExtendIntoTitleBar)
                return false;

            switch (message)
            {
                case WM_NCCALCSIZE:
                    {
                        if (w_param == default)
                        {
                            m.Result = DefWindowProc(handle, message, w_param, l_param);
                            return true;
                        }

                        var dpi = (uint)form.DeviceDpi;

                        int frame_x = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) ? GetSystemMetricsForDpi(SM_CXFRAME, dpi) : LogicalToDeviceUnits(1);
                        int frame_y = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) ? GetSystemMetricsForDpi(SM_CYFRAME, dpi) : LogicalToDeviceUnits(31);
                        int padding = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) ? GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi) : LogicalToDeviceUnits(1);

                        NCCALCSIZE_PARAMS* @params = (NCCALCSIZE_PARAMS*)l_param;
                        RECT* requested_client_rect = (RECT*)&@params->rgrc;

                        requested_client_rect->right -= frame_x + padding;
                        requested_client_rect->left += frame_x + padding;
                        requested_client_rect->bottom -= frame_y + padding;

                        if (win32_window_is_maximized(handle))
                        {
                            requested_client_rect->top += padding;
                        }

                        m.Result = IntPtr.Zero;
                        return true;
                    }
                case WM_CREATE:
                    {
                        // Inform the application of the frame change to force redrawing with the new
                        // client area that is extended into the title bar
                        SetWindowPos(
                            handle,
                            default,
                            0, 0, 0, 0,
                            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER
                        );
                        break;
                    }
                case WM_ACTIVATE:
                    {
                        var title_bar_rect = win32_titlebar_rect(handle);
                        InvalidateRect(handle, &title_bar_rect, false);
                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return true;
                    }
                case WM_NCHITTEST:
                    {
                        // Let the default procedure handle resizing areas
                        LRESULT hit = DefWindowProc(handle, message, w_param, l_param);
                        switch ((uint)hit.Value)
                        {
                            case HTNOWHERE:
                            case HTRIGHT:
                            case HTLEFT:
                            case HTTOPLEFT:
                            case HTTOP:
                            case HTTOPRIGHT:
                            case HTBOTTOMRIGHT:
                            case HTBOTTOM:
                            case HTBOTTOMLEFT:
                                {
                                    m.Result = hit;
                                    return true;
                                }
                        }
                        // Check if hover button is on maximize to support SnapLayout on Windows 11
                        if (maximizeButtonHovered)
                        {
                            m.Result = (nint)HTMAXBUTTON;
                            return true;
                        }

                        // Looks like adjustment happening in NCCALCSIZE is messing with the detection
                        // of the top hit area so manually fixing that.
                        var dpi = (uint)form.DeviceDpi;
                        int frame_y = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) ? GetSystemMetricsForDpi(SM_CYFRAME, dpi) : LogicalToDeviceUnits(31);
                        int padding = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393) ? GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi) : LogicalToDeviceUnits(1);
                        Point cursor_point = default;
                        cursor_point.X = GET_X_PARAM(l_param);
                        cursor_point.Y = GET_Y_PARAM(l_param);
                        ScreenToClient(handle, &cursor_point);
                        // We should not return HTTOP when hit-testing a maximized window 
                        if (!win32_window_is_maximized(handle) && cursor_point.Y > 0 && cursor_point.Y < frame_y + padding)
                        {
                            m.Result = (nint)HTTOP;
                            return true;
                        }

                        // Since we are drawing our own caption, this needs to be a custom test
                        if (cursor_point.Y < win32_titlebar_rect(handle).bottom)
                        {
                            m.Result = (nint)HTCAPTION;
                            return true;
                        }

                        m.Result = (nint)HTCLIENT;
                        return true;
                    }
                case WM_NCRBUTTONUP:
                    {
                        if (w_param == HTCAPTION)
                        {
                            bool isMaximized = IsZoomed(handle);
                            var sysMenu = GetSystemMenu(handle, false);

                            EnableMenuItem(sysMenu, SC_RESTORE, isMaximized ? MF_ENABLED : MF_DISABLED);
                            EnableMenuItem(sysMenu, SC_MOVE, !isMaximized ? MF_ENABLED : MF_DISABLED);
                            EnableMenuItem(sysMenu, SC_SIZE, !isMaximized ? MF_ENABLED : MF_DISABLED);
                            EnableMenuItem(sysMenu, SC_MINIMIZE, MF_ENABLED);
                            EnableMenuItem(sysMenu, SC_MAXIMIZE, !isMaximized ? MF_ENABLED : MF_DISABLED);
                            EnableMenuItem(sysMenu, SC_CLOSE, MF_ENABLED);

                            var result = TrackPopupMenu(
                                sysMenu,
                                TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD,
                                GET_X_PARAM(l_param),
                                GET_Y_PARAM(l_param),
                                0,
                                handle,
                                null
                            );

                            if (result != 0)
                            {
                                PostMessage(handle, WM_SYSCOMMAND, (nuint)result.Value, 0);
                            }
                        }

                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return true;
                    }
                case WM_NCMOUSEMOVE:
                    {
                        GetCursorPos(out var cursor_point);
                        ScreenToClient(handle, &cursor_point);

                        bool oldCloseHovered = closeButtonHovered;
                        bool oldMaximizeHovered = maximizeButtonHovered;
                        bool oldMinimizeHovered = minimizeButtonHovered;

                        closeButtonHovered = closeButtonRect.Contains(cursor_point.X, cursor_point.Y);
                        maximizeButtonHovered = maximizeButtonRect.Contains(cursor_point.X, cursor_point.Y);
                        minimizeButtonHovered = minimizeButtonRect.Contains(cursor_point.X, cursor_point.Y);

                        // Only invalidate specific button areas that changed
                        if (oldCloseHovered != closeButtonHovered)
                        {
                            var closeRect = new RECT(closeButtonRect.Left, closeButtonRect.Top,
                                closeButtonRect.Right, closeButtonRect.Bottom);
                            InvalidateRect(handle, &closeRect, false);
                        }
                        if (oldMaximizeHovered != maximizeButtonHovered)
                        {
                            var maxRect = new RECT(maximizeButtonRect.Left, maximizeButtonRect.Top,
                                maximizeButtonRect.Right, maximizeButtonRect.Bottom);
                            InvalidateRect(handle, &maxRect, false);
                        }
                        if (oldMinimizeHovered != minimizeButtonHovered)
                        {
                            var minRect = new RECT(minimizeButtonRect.Left, minimizeButtonRect.Top,
                                minimizeButtonRect.Right, minimizeButtonRect.Bottom);
                            InvalidateRect(handle, &minRect, false);
                        }

                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return true;
                    }
                case WM_NCMOUSELEAVE:
                    {
                        bool needsInvalidation = closeButtonHovered || maximizeButtonHovered || minimizeButtonHovered;

                        if (needsInvalidation)
                        {
                            closeButtonHovered = false;
                            maximizeButtonHovered = false;
                            minimizeButtonHovered = false;

                            var title_bar_rect = win32_titlebar_rect(handle);
                            InvalidateRect(handle, &title_bar_rect, false);
                        }
                        break;
                    }
                case WM_NCLBUTTONDOWN:
                    {
                        // Clicks on buttons will be handled in WM_NCLBUTTONUP, but we still need
                        // to remove default handling of the click to avoid it counting as drag.
                        if (closeButtonHovered || maximizeButtonHovered || minimizeButtonHovered)
                        {
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                        // Default handling allows for dragging and double click to maximize
                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return true;
                    }
                case WM_NCLBUTTONUP:
                    {
                        if (closeButtonHovered)
                        {
                            PostMessage(handle, WM_CLOSE, 0, 0);
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                        else if (minimizeButtonHovered)
                        {
                            ShowWindow(handle, SW_MINIMIZE);
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                        else if (maximizeButtonHovered)
                        {
                            var mode = win32_window_is_maximized(handle) ? SW_NORMAL : SW_MAXIMIZE;
                            ShowWindow(handle, mode);
                            m.Result = IntPtr.Zero;
                            return true;
                        }

                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return true;
                    }
            }
            return false;
        }

        /// <summary>
        /// Call this when the form handle is created to insert custom menu items.
        /// </summary>
        private unsafe void InitializeCustomMenu()
        {
            var sysMenu = GetSystemMenu((HWND)form.Handle, false);
            MENUITEMINFOW sep = new();
            sep.cbSize = (uint)sizeof(MENUITEMINFOW);
            sep.fMask = MENU_ITEM_MASK.MIIM_FTYPE;
            sep.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
            InsertMenuItem(sysMenu, 0, true, &sep);

            InsertToggleMenu(ID_TOGGLE_TOPMOST, 0, "Always On Top", form.TopMost);
            InsertToggleMenu(ID_TOGGLE_EXTEND_INTO_TITLEBAR, 1, "Extend Into Title Bar", ExtendIntoTitleBar);
        }

        private unsafe void InsertToggleMenu(uint id, uint item, string label, bool value)
        {
            var sysMenu = GetSystemMenu((HWND)form.Handle, false);
            fixed (char* text = label)
            {
                MENUITEMINFOW mii = new();
                mii.cbSize = (uint)sizeof(MENUITEMINFOW);
                mii.fMask = MENU_ITEM_MASK.MIIM_ID | MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_STATE;
                mii.fState = value ? MENU_ITEM_STATE.MFS_CHECKED : MENU_ITEM_STATE.MFS_UNCHECKED;
                mii.wID = id;
                mii.dwTypeData = text;
                InsertMenuItem(sysMenu, item, true, &mii);
            }
        }

        private unsafe void ToggleMenu(uint id, bool value)
        {
            var sysMenu = GetSystemMenu((HWND)form.Handle, false);
            MENUITEMINFOW mii = new();
            mii.cbSize = (uint)sizeof(MENUITEMINFOW);
            mii.fMask = MENU_ITEM_MASK.MIIM_STATE;
            mii.fState = value ? MENU_ITEM_STATE.MFS_CHECKED : MENU_ITEM_STATE.MFS_UNCHECKED;
            SetMenuItemInfo(sysMenu, id, false, &mii);

            MenuToggled?.Invoke(this, id);
        }

        /// <summary>
        /// Update the title bar button rectangles. Call this on form resize.
        /// </summary>
        private void UpdateTitleBarButtonRects()
        {
            if (!ShouldExtendIntoTitleBar)
                return;

            var handle = (HWND)form.Handle;
            var titleBarRect = win32_titlebar_rect(handle);
            int buttonWidth = LogicalToDeviceUnits(47);

            closeButtonRect = new Rectangle(
                titleBarRect.right - buttonWidth,
                titleBarRect.top + 1, // Account for fake shadow
                buttonWidth,
                titleBarRect.bottom - titleBarRect.top - 1
            );

            maximizeButtonRect = new Rectangle(
                closeButtonRect.Left - buttonWidth,
                closeButtonRect.Top,
                buttonWidth,
                closeButtonRect.Height
            );

            minimizeButtonRect = new Rectangle(
                maximizeButtonRect.Left - buttonWidth,
                maximizeButtonRect.Top,
                buttonWidth,
                maximizeButtonRect.Height
            );
        }

        /// <summary>
        /// Gets a value indicating whether title bar buttons should be drawn.
        /// </summary>
        /// <remarks>Title bar buttons are drawn when the application extends into the title bar and any
        /// of the title bar buttons (close, maximize, or minimize) are hovered.</remarks>
        public bool ShouldDrawTitleBarButtons => ShouldExtendIntoTitleBar && (closeButtonHovered || maximizeButtonHovered || minimizeButtonHovered);

        public Rectangle CloseButtonRect => closeButtonRect;
        public Rectangle MaximizeButtonRect => maximizeButtonRect;
        public Rectangle MinimizeButtonRect => minimizeButtonRect;
        public bool CloseButtonHovered => closeButtonHovered;
        public bool MaximizeButtonHovered => maximizeButtonHovered;
        public bool MinimizeButtonHovered => minimizeButtonHovered;
        public bool IsMaximized => form.WindowState == FormWindowState.Maximized;

        public int LogicalToDeviceUnits(int value) => form.LogicalToDeviceUnits(value);

        private static unsafe bool win32_window_is_maximized(HWND handle)
        {
            WINDOWPLACEMENT placement = default;
            placement.length = (uint)sizeof(WINDOWPLACEMENT);
            if (GetWindowPlacement(handle, &placement))
            {
                return placement.showCmd == SW_SHOWMAXIMIZED;
            }
            return false;
        }

        private static int GET_X_PARAM(nint lParam)
        {
            return (short)(lParam.ToInt32() & 0xFFFF);
        }

        private static int GET_Y_PARAM(nint lParam)
        {
            return (short)((lParam.ToInt32() >> 16) & 0xFFFF);
        }

        // Adopted from:
        // https://github.com/oberth/custom-chrome/blob/master/source/gui/window_helper.hpp#L52-L64
        private unsafe RECT win32_titlebar_rect(HWND handle)
        {
            const int top_and_bottom_borders = 2;
            using var theme = OpenThemeData(handle, "WINDOW");
            GetThemePartSize(theme, default, (int)WINDOWPARTS.WP_CAPTION, (int)CAPTIONSTATES.CS_ACTIVE, default, THEMESIZE.TS_TRUE, out var title_bar_size);

            int height = LogicalToDeviceUnits(title_bar_size.cy) + top_and_bottom_borders;

            RECT rect;
            GetClientRect(handle, &rect);
            rect.bottom = rect.top + height;
            return rect;
        }
    }
}
