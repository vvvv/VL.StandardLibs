using ImGuiNET;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Stride.Games;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Core.Mathematics;

using VL.Core;
using VL.Lib.Collections;
using VL.Lib.Basics.Resources;
using VL.Stride;

namespace VL.ImGui.Stride
{
    using ImGui = ImGuiNET.ImGui;
    using Vector2 = System.Numerics.Vector2;
    
    
    public delegate void ImGuiWindowsCreateHandler(out object stateOutput);
    public delegate void ImGuiWindowsDrawHandler(object stateInput, IGraphicsRendererBase value, GameWindow gameWindow, GraphicsPresenter presenter, out object stateOutput, out IGraphicsRendererBase result);
    public partial class ImGuiWindows : IDisposable
    {
        // VL
        private readonly NodeContext _nodeContext;
        private readonly RectangleF _bounds;
        private readonly ImGuiWindow _mainViewportWindow;
        private readonly StrideDeviceContext _strideDeviceContext;

        private readonly IResourceHandle<GraphicsDevice> _deviceHandle;
        private bool _disposed = false;

        private readonly Platform_CreateWindow _createWindow;
        private readonly Platform_DestroyWindow _destroyWindow;
        private readonly Platform_GetWindowPos _getWindowPos;
        private readonly Platform_ShowWindow _showWindow;
        private readonly Platform_SetWindowPos _setWindowPos;
        private readonly Platform_SetWindowSize _setWindowSize;
        private readonly Platform_GetWindowSize _getWindowSize;
        private readonly Platform_SetWindowFocus _setWindowFocus;
        private readonly Platform_GetWindowFocus _getWindowFocus;
        private readonly Platform_GetWindowMinimized _getWindowMinimized;
        private readonly Platform_SetWindowTitle _setWindowTitle;

        public event EventHandler<EventArgs> Closing
        {
            add => _mainViewportWindow.Closing += value;
            remove => _mainViewportWindow.Closing -= value;
        }

        public GameWindow GameWindow => _mainViewportWindow.GameWindow;

        public unsafe ImGuiWindows(NodeContext nodeContext, RectangleF bounds)
        {
            _nodeContext = nodeContext;
            _bounds = bounds;
            _strideDeviceContext = new StrideDeviceContext(nodeContext);

            _deviceHandle = nodeContext.AppHost.Services.GetDeviceHandle();

            using (_strideDeviceContext.MakeCurrent())
            {
                _strideDeviceContext.IO.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

                ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
                ImGuiViewportPtr mainViewport = platformIO.Viewports[0];

                _mainViewportWindow = new ImGuiWindow(_nodeContext, _strideDeviceContext, mainViewport, _bounds);
                mainViewport.PlatformHandle = _mainViewportWindow.Handle;

                _mainViewportWindow.Closing += (o, i) =>
                {
                    using (_strideDeviceContext.MakeCurrent())
                    {
                        // Closing all other Windows
                        ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
                        if ((nint)platformIO.NativePtr != IntPtr.Zero)
                        {
                            var viewPorts = platformIO.Viewports;
                            if (viewPorts.Data != IntPtr.Zero)
                            {
                                for (int index = 1; index < platformIO.Viewports.Size; index++)
                                {
                                    platformIO.Viewports[index].PlatformRequestClose = true;
                                }
                            }
                        }
                    }
                };

                _createWindow = CreateWindow;
                _destroyWindow = DestroyWindow;
                _getWindowPos = GetWindowPos;
                _showWindow = ShowWindow;
                _setWindowPos = SetWindowPos;
                _setWindowSize = SetWindowSize;
                _getWindowSize = GetWindowSize;
                _setWindowFocus = SetWindowFocus;
                _getWindowFocus = GetWindowFocus;
                _getWindowMinimized = GetWindowMinimized;
                _setWindowTitle = SetWindowTitle;

                platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindow);
                platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindow);
                platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindow);
                platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPos);
                platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSize);
                platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocus);
                platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocus);
                platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimized);
                platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitle);

                ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowPos(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowPos));
                ImGuiNative.ImGuiPlatformIO_Set_Platform_GetWindowSize(platformIO.NativePtr, Marshal.GetFunctionPointerForDelegate(_getWindowSize));

                unsafe
                {
                    _strideDeviceContext.IO.NativePtr->BackendPlatformName = (byte*)new FixedAsciiString("VL.ImGui.Stride Backend").DataPtr;
                }

                _strideDeviceContext.IO.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;
                _strideDeviceContext.IO.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;
                _strideDeviceContext.IO.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
                _strideDeviceContext.IO.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
                _strideDeviceContext.IO.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            }
        }

        public void Update(ImGuiWindowsCreateHandler create, ImGuiWindowsDrawHandler draw, Widget? widget, bool dockingEnabled, Spread<FontConfig?> fonts, IStyle style)
        {
            SetPerFrameImGuiData();
            UpdateMonitors();

            using (_strideDeviceContext.MakeCurrent())
            {
                // should be always
                if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                {
                    // Enable Docking
                    if (dockingEnabled)
                        _strideDeviceContext.IO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                    else
                        _strideDeviceContext.IO.ConfigFlags &= ~ImGuiConfigFlags.DockingEnable;

                    _strideDeviceContext.NewFrame();

                    try
                    {
                        using var _ = _strideDeviceContext.ApplyStyle(style);

                        // Enable Docking
                        if (dockingEnabled)
                        {
                            ImGui.DockSpaceOverViewport();

                            var viewPort = ImGui.GetMainViewport();
                            ImGui.SetNextWindowPos(viewPort.WorkPos);
                            ImGui.SetNextWindowSize(viewPort.WorkSize);
                            ImGui.Begin("DockingRoot", ImGuiWindowFlags.ChildWindow);
                        }
                        else
                        {
                            var viewPort = ImGui.GetMainViewport();
                            ImGui.SetNextWindowPos(viewPort.WorkPos);
                            ImGui.SetNextWindowSize(viewPort.WorkSize);
                            ImGui.Begin("FullscreenRoot",
                                ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus |
                                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoDecoration |
                                ImGuiWindowFlags.NoBackground);
                        }

                        _strideDeviceContext.SetDrawList(DrawList.AtCursor);
                        _strideDeviceContext.Update(widget);
                    }
                    finally
                    {
                        ImGui.End();
                        if (dockingEnabled)
                        {
                            ImGui.End();
                        }

                        // Render (builds mesh with texture coordinates)
                        ImGui.Render();

                        _mainViewportWindow.Update(create, draw, ImGui.GetDrawData(), fonts, style);

                        // Update and Render additional Platform Windows
                        if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
                        {
                            ImGui.UpdatePlatformWindows();
                            // ImGui.RenderPlatformWindowsDefault();

                            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();

                            for (int i = 1; i < platformIO.Viewports.Size; i++)
                            {
                                ImGuiViewportPtr vp = platformIO.Viewports[i];
                                GetWindow(vp, (window) =>
                                {
                                    window.Update(create, draw, vp.DrawData, fonts, style);
                                });
                            }
                        }
                    }
                }
            }
        }

        private void SetPerFrameImGuiData()
        {
            using (_strideDeviceContext.MakeCurrent())
            {
                unsafe
                {
                    int x, y;
                    uint buttons = global::Stride.Graphics.SDL.Window.SDL.GetGlobalMouseState(&x, &y);
                    _strideDeviceContext.IO.MouseDown[0] = (buttons & 0b0001) != 0;
                    _strideDeviceContext.IO.MouseDown[1] = (buttons & 0b0010) != 0;
                    _strideDeviceContext.IO.MouseDown[2] = (buttons & 0b0100) != 0;
                    _strideDeviceContext.IO.MousePos = new Vector2(x, y);
                }

                _strideDeviceContext.IO.DisplaySize = new Vector2(_mainViewportWindow.Size.X, _mainViewportWindow.Size.Y);
                _strideDeviceContext.IO.DisplayFramebufferScale = new Vector2(1.0f, 1.0f);

                unsafe
                {
                    var platformIO = ImGui.GetPlatformIO();
                    if ((nint)platformIO.NativePtr != IntPtr.Zero)
                    {
                        var viewPorts = platformIO.Viewports;
                        if (viewPorts.Data != IntPtr.Zero)
                        {
                            if (_mainViewportWindow != null && platformIO.Viewports.Size > 0 && (nint)platformIO.Viewports[0].NativePtr != IntPtr.Zero)
                            {
                                platformIO.Viewports[0].Pos = new Vector2(_mainViewportWindow.Position.X, _mainViewportWindow.Position.Y);
                                platformIO.Viewports[0].Size = new Vector2(_mainViewportWindow.Size.X, _mainViewportWindow.Size.Y);
                            }
                        }
                    }
                }
            }
        }

        private unsafe void UpdateMonitors()
        {
            using (_strideDeviceContext.MakeCurrent())
            {
                var outputs = _deviceHandle.Resource.Adapter.Outputs;

                ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
                if (platformIO.NativePtr->Monitors.Data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
                }

                int numMonitors = outputs.Length;
                IntPtr data = IntPtr.Zero;

                try
                {
                    data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
                    platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);

                    for (int i = 0; i < numMonitors; i++)
                    {
                        Vector2 pos = new Vector2(outputs[i].DesktopBounds.X, outputs[i].DesktopBounds.Y);
                        Vector2 size = new Vector2(outputs[i].CurrentDisplayMode.Width, outputs[i].CurrentDisplayMode.Height);

                        ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];
                        monitor.DpiScale = 1f; // TODO GET SCALE PER MONITOR
                        monitor.MainPos = pos;
                        monitor.MainSize = size;
                        monitor.WorkPos = pos;
                        monitor.WorkSize = size;
                    }
                }
                catch
                {
                    if (data != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(data);
                    }
                    throw;
                }
                finally
                {
                    if (data != IntPtr.Zero && platformIO.NativePtr->Monitors.Data == IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(data);
                    }
                }
            }
        }

        #region Platformfunctions
        private void CreateWindow(ImGuiViewportPtr vp)
        {
            ImGuiWindow window = new ImGuiWindow(_nodeContext, _strideDeviceContext, vp, _bounds);
            vp.PlatformUserData = GCHandle.ToIntPtr(GCHandle.Alloc(window));
        }

        private void DestroyWindow(ImGuiViewportPtr vp)
        {
            GetWindow(vp, (window) => 
            { 
                window.Dispose();
                GCHandle.FromIntPtr(vp.PlatformUserData).Free();
                vp.PlatformUserData = IntPtr.Zero;
            });
        }

        private void ShowWindow(ImGuiViewportPtr vp)
        {
            GetWindow(vp, (window) => { window.Activate(); });
        }

        private unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
        {
            GetWindow(vp, (window) => { *outPos = new Vector2(window.Position.X, window.Position.Y); });
        }

        private void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
        {
            GetWindow(vp, (window) => { window.Position = new Int2((int)pos.X, (int)pos.Y); });
        }

        private unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
        {
            GetWindow(vp, (window) => { *outSize = new Vector2(window.Size.X, window.Size.Y); });
        }

        private void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
        {
            GetWindow(vp, (window) => { window.Size = new Int2((int)size.X, (int)size.Y); });
        }

        private void SetWindowFocus(ImGuiViewportPtr vp)
        {
            GetWindow(vp, (window) => { window.Activate();});
        }

        private byte GetWindowFocus(ImGuiViewportPtr vp)
        {
            return GetWindow(vp, () => { return (byte)0; }, (window) => { return window.IsFocused ? (byte)1 : (byte)0; });
        }

        private byte GetWindowMinimized(ImGuiViewportPtr vp)
        {
            return GetWindow(vp, () => { return (byte)0; }, (window) => { return window.IsMinimized ? (byte)1 : (byte)0; } );
        }

        private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
        {
            GetWindow(vp, (window) => 
            {
                byte* titlePtr = (byte*)title;
                int count = 0;
                while (titlePtr[count] != 0)
                {
                    count += 1;
                }
                window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
            });      
        }

        private void GetWindow(ImGuiViewportPtr vp, Action<ImGuiWindow> IsTrue)
        {
            GetWindow<bool>(vp, () => {return false;}, (window) => { IsTrue.Invoke(window); return true; });
        }

        private TResult GetWindow<TResult>(ImGuiViewportPtr vp, Func<TResult> IsFalse, Func<ImGuiWindow, TResult> IsTrue)
        {
            if (vp.PlatformUserData == IntPtr.Zero)
            {
                return IsFalse.Invoke();
            }

            var handle = GCHandle.FromIntPtr(vp.PlatformUserData);
            if (!handle.IsAllocated)
            {
                return IsFalse.Invoke();
            }

            var target = handle.Target;
            if (target != null && target is ImGuiWindow window)
            {
                return IsTrue.Invoke(window);
            }
            else
            {
                return IsFalse.Invoke();
            }
        }

        
        #endregion Platformfunctions

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _deviceHandle.Dispose();
                    _mainViewportWindow.Dispose();
                    _strideDeviceContext.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
