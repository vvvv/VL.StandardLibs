extern alias sw;

using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using sw::VL.Skia;
using sw::Windows.Win32.Foundation;
using sw::Windows.Win32.Graphics.Gdi;
using sw::Windows.Win32.UI.Controls;
using sw::Windows.Win32.UI.WindowsAndMessaging;
using static sw::Windows.Win32.PInvoke;
using static sw::Windows.Win32.UI.WindowsAndMessaging.MENU_ITEM_FLAGS;
using static sw::Windows.Win32.UI.WindowsAndMessaging.SHOW_WINDOW_CMD;
using static sw::Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX;
using RGBA = Stride.Core.Mathematics.Color4;

namespace VL.Skia
{
    partial class SkiaRenderer
    {

        // Win32 custom title bar related fields
        private Rectangle closeButtonRect;
        private Rectangle maximizeButtonRect;
        private Rectangle minimizeButtonRect;
        private bool isMaximized = false;
        private bool closeButtonHovered = false;
        private bool maximizeButtonHovered = false;
        private bool minimizeButtonHovered = false;
        private bool titleBarPressed = false;
        private Point dragStartPoint;

        // Custom menu id for toggling top-most state (must not clash with SC_* which are 0xF000+)
        internal const uint ID_TOGGLE_TOPMOST = 0x100; // arbitrary custom id
        internal const uint ID_TOGGLE_EXTEND_INTO_TITLEBAR = 0x101; // arbitrary custom id

        // Whether we want to extend the client area into the title bar. Can be changed at runtime.
        private bool extendIntoTitleBar = false;
        /// <summary>
        /// Enables custom drawing and handling of the title bar (extending the client area into the standard non-client area).
        /// Automatically ignored while the renderer is in fullscreen (borderless) mode.
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
                    if (IsHandleCreated)
                    {
                        var handle = (HWND)Handle;
                        SetWindowPos(handle, default, 0, 0, 0, 0,
                            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER);
                        Invalidate();
                    }
                }
            }
        }

        // Convenience effective flag (disabled when fullscreen)
        private bool ShouldExtendIntoTitleBar => extendIntoTitleBar && !FFullScreen; // FFullScreen declared in other partial

        public RGBA TitleBarColor { get; set; }

        // Port of https://github.com/grassator/win32-window-custom-titlebar
        protected override unsafe void WndProc(ref Message m)
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(8) && TouchMessageProcessor.TryHandle(ref m, this, touchNotifications))
                return;

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
                            TopMost = !TopMost;
                            ToggleMenu(ID_TOGGLE_TOPMOST, TopMost);
                        }
                        else if (m.WParam == ID_TOGGLE_EXTEND_INTO_TITLEBAR)
                        {
                            ExtendIntoTitleBar = !ExtendIntoTitleBar;
                            ToggleMenu(ID_TOGGLE_EXTEND_INTO_TITLEBAR, ExtendIntoTitleBar);
                        }
                        break;
                    }
                case WM_SETCURSOR:
                    {
                        // Show an arrow instead of the busy cursor
                        SetCursor(LoadCursor(default, IDC_ARROW));
                        break;
                    }
            }

            // If we don't extend into title bar just fall back to base behavior (except for menu, cursor override & touch above)
            if (!ShouldExtendIntoTitleBar)
            {
                base.WndProc(ref m);
                return;
            }

            switch (message)
            {
                case WM_NCCALCSIZE:
                    {
                        if (w_param == default)
                        {
                            DefWndProc(ref m);
                            return;
                        }

                        var dpi = (uint)DeviceDpi; // GetDpiForWindow

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

                        return;
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
                        return;
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
                                    return;
                                }
                        }
                        // Check if hover button is on maximize to support SnapLayout on Windows 11
                        if (maximizeButtonHovered)
                        {
                            m.Result = (nint)HTMAXBUTTON;
                            return;
                        }

                        // Looks like adjustment happening in NCCALCSIZE is messing with the detection
                        // of the top hit area so manually fixing that.
                        var dpi = (uint)DeviceDpi;
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
                            return;
                        }

                        // Since we are drawing our own caption, this needs to be a custom test
                        if (cursor_point.Y < win32_titlebar_rect(handle).bottom)
                        {
                            m.Result = (nint)HTCAPTION;
                            return;
                        }

                        m.Result = (nint)HTCLIENT;
                        return;
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
                        return;
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
                        return;
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
                            return;
                        }
                        // Default handling allows for dragging and double click to maximize
                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return;
                    }
                case WM_NCLBUTTONUP:
                    {
                        if (closeButtonHovered)
                        {
                            PostMessage(handle, WM_CLOSE, 0, 0);
                            return;
                        }
                        else if (minimizeButtonHovered)
                        {
                            ShowWindow(handle, SW_MINIMIZE);
                            return;
                        }
                        else if (maximizeButtonHovered)
                        {
                            var mode = win32_window_is_maximized(handle) ? SW_NORMAL : SW_MAXIMIZE;
                            ShowWindow(handle, mode);
                            return;
                        }

                        m.Result = DefWindowProc(handle, message, w_param, l_param);
                        return;
                    }
            }
            base.WndProc(ref m);
        }

        private unsafe void InsertCustomMenu()
        {
            var sysMenu = GetSystemMenu((HWND)Handle, false);
            MENUITEMINFOW sep = new();
            sep.cbSize = (uint)sizeof(MENUITEMINFOW);
            sep.fMask = MENU_ITEM_MASK.MIIM_FTYPE;
            sep.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
            InsertMenuItem(sysMenu, 0, true, &sep);

            InsertToggleMenu(ID_TOGGLE_TOPMOST, 0, "Always On Top", TopMost);
            InsertToggleMenu(ID_TOGGLE_EXTEND_INTO_TITLEBAR, 1, "Extend Into Title Bar", ExtendIntoTitleBar);
        }

        private unsafe void InsertToggleMenu(uint id, uint item, string label, bool value)
        {
            var sysMenu = GetSystemMenu((HWND)Handle, false);
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
            var sysMenu = GetSystemMenu((HWND)Handle, false);
            if (value)
            {
                MENUITEMINFOW mii = new();
                mii.cbSize = (uint)sizeof(MENUITEMINFOW);
                mii.fMask = MENU_ITEM_MASK.MIIM_STATE;
                mii.fState = MENU_ITEM_STATE.MFS_CHECKED;
                SetMenuItemInfo(sysMenu, id, false, &mii);
            }
            else
            {
                MENUITEMINFOW mii = new();
                mii.cbSize = (uint)sizeof(MENUITEMINFOW);
                mii.fMask = MENU_ITEM_MASK.MIIM_STATE;
                mii.fState = MENU_ITEM_STATE.MFS_UNCHECKED;
                SetMenuItemInfo(sysMenu, id, false, &mii);
            }

            MenuToggled?.Invoke(this, id);
        }

        internal event EventHandler<uint> MenuToggled;

        // Add this helper method to update button rectangles
        private void UpdateTitleBarButtonRects()
        {
            if (!ShouldExtendIntoTitleBar)
                return;
            var handle = (HWND)Handle;
            if (handle == default)
                return;

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

        private void PaintTitleBarButtons(SKCanvas canvas)
        {
            if (!ShouldExtendIntoTitleBar)
                return;
            // Only render anything if at least one button is hovered
            if (!(closeButtonHovered || maximizeButtonHovered || minimizeButtonHovered))
                return;

            int iconDimension = LogicalToDeviceUnits(10);

            // Icon color
            var iconColor = SKColors.White;
            // Use dark gray for idle and a brighter gray for hover so it is visible on both dark and light backgrounds.
            var idleBgColor = new SKColor(60, 60, 60, 90);    // subtle dark gray
            var hoverBgColor = new SKColor(170, 170, 170, 140); // brighter gray (lightens on hover)

            using var iconStrokePaint = new SKPaint
            {
                Color = iconColor,
                IsStroke = true,
                StrokeWidth = 1f,
                IsAntialias = false,
                StrokeCap = SKStrokeCap.Square
            };
            using var iconFillPaint = new SKPaint
            {
                Color = iconColor,
                IsStroke = false,
                IsAntialias = false
            };

            // Helper local scope (no function) to draw each button without creating delegates/local function frames.
            // Minimize button background
            using (var bgPaint = new SKPaint { Color = minimizeButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(minimizeButtonRect.Left, minimizeButtonRect.Top, minimizeButtonRect.Width, minimizeButtonRect.Height, bgPaint);
            // Minimize icon (single horizontal line)
            var minY = minimizeButtonRect.Y + (minimizeButtonRect.Height - 1) / 2f;
            var minX = minimizeButtonRect.X + (minimizeButtonRect.Width - iconDimension) / 2f;
            canvas.DrawRect(new SKRect(minX, minY, minX + iconDimension, minY + 1), iconFillPaint);

            // Maximize button background
            using (var bgPaint = new SKPaint { Color = maximizeButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(maximizeButtonRect.Left, maximizeButtonRect.Top, maximizeButtonRect.Width, maximizeButtonRect.Height, bgPaint);
            // Maximize icon
            var maxLeft = maximizeButtonRect.X + (maximizeButtonRect.Width - iconDimension) / 2f;
            var maxTop = maximizeButtonRect.Y + (maximizeButtonRect.Height - iconDimension) / 2f;
            var maxRect = new SKRect(maxLeft, maxTop, maxLeft + iconDimension, maxTop + iconDimension);
            if (win32_window_is_maximized((HWND)this.Handle))
            {
                var offset = LogicalToDeviceUnits(2);
                var backRect = new SKRect(maxRect.Left + offset, maxRect.Top - offset, maxRect.Right + offset, maxRect.Bottom - offset);
                canvas.DrawRect(backRect, iconStrokePaint);
            }
            canvas.DrawRect(maxRect, iconStrokePaint);

            // Close button background
            using (var bgPaint = new SKPaint { Color = closeButtonHovered ? hoverBgColor : idleBgColor, IsStroke = false, IsAntialias = false })
                canvas.DrawRect(closeButtonRect.Left, closeButtonRect.Top, closeButtonRect.Width, closeButtonRect.Height, bgPaint);
            // Close icon (X)
            var closeLeft = closeButtonRect.X + (closeButtonRect.Width - iconDimension) / 2f;
            var closeTop = closeButtonRect.Y + (closeButtonRect.Height - iconDimension) / 2f;
            var closeRight = closeLeft + iconDimension;
            var closeBottom = closeTop + iconDimension;
            canvas.DrawLine(closeLeft, closeTop, closeRight, closeBottom, iconStrokePaint);
            canvas.DrawLine(closeLeft, closeBottom, closeRight, closeTop, iconStrokePaint);
        }

        static unsafe bool win32_window_is_maximized(HWND handle)
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
        unsafe RECT win32_titlebar_rect(HWND handle)
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
