﻿using ImGuiNET;
using System.Runtime.InteropServices;

using VL.Core;

using Stride.Games;
using Stride.Rendering;
using Stride.Input;
using Stride.Graphics;

using Stride.Core.Mathematics;
using VL.Lib.Collections;
using System.Runtime.CompilerServices;
using VL.Lib.Basics.Resources;
using VL.Stride;

using StrideVector2 = Stride.Core.Mathematics.Vector2;
using StrideApp = Stride.Graphics.SDL.Application;

namespace VL.ImGui.Stride
{
    using ImGui = ImGuiNET.ImGui;
    using Vector2 = System.Numerics.Vector2;
    
    
    public delegate void ImGuiWindowsCreateHandler(out object stateOutput);
    public delegate void ImGuiWindowsDrawHandler(object stateInput, IGraphicsRendererBase value, IInputSource inputSource, GameWindow gameWindow, GraphicsPresenter presenter, out object stateOutput, out IGraphicsRendererBase result);
    public partial class ImGuiWindows : IDisposable
    {
        //VL 
        private readonly NodeContext nodeContext;
        private readonly ImGuiWindow mainViewportWindow;
        private readonly StrideDeviceContext _strideDeviceContext;

        private readonly IResourceHandle<GraphicsDevice> deviceHandle;

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

        public unsafe ImGuiWindows(NodeContext nodeContext)
        {
            this.nodeContext = nodeContext;
            _strideDeviceContext = new StrideDeviceContext(nodeContext);

            deviceHandle = AppHost.Current.Services.GetDeviceHandle();

            using (_strideDeviceContext.MakeCurrent()) 
            {
                _strideDeviceContext.IO.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                _strideDeviceContext.IO.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

                ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
                ImGuiViewportPtr mainViewport = platformIO.Viewports[0];

                mainViewportWindow = new ImGuiWindow(this.nodeContext, _strideDeviceContext, mainViewport);
                mainViewport.PlatformHandle = mainViewportWindow.Handle;
            

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

        public void Update(ImGuiWindowsCreateHandler create, ImGuiWindowsDrawHandler draw, Widget? widget, bool dockingEnabled, Spread<FontConfig?> fonts, bool fullscreenWindow, IStyle style)
        {


            var pos = StrideApp.MousePosition;
            _strideDeviceContext.IO.MousePos = new Vector2(pos.X, pos.Y);

            SetPerFrameImGuiData();
            UpdateMonitors();
            mainViewportWindow.Update(create, draw, widget, dockingEnabled, fonts, fullscreenWindow, style);
        }

        private void SetPerFrameImGuiData()
        {
            _strideDeviceContext.IO.DisplaySize = new Vector2(mainViewportWindow.Size.X, mainViewportWindow.Size.Y);
            _strideDeviceContext.IO.DisplayFramebufferScale = new Vector2(1.0f, 1.0f);
            
            
            ImGui.GetPlatformIO().Viewports[0].Pos = new Vector2(mainViewportWindow.Position.X, mainViewportWindow.Position.Y);    
            ImGui.GetPlatformIO().Viewports[0].Size = new Vector2(mainViewportWindow.Size.X, mainViewportWindow.Size.Y);
        }

        private unsafe void UpdateMonitors()
        {
            var outputs = deviceHandle.Resource.Adapter.Outputs;

            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
            Marshal.FreeHGlobal(platformIO.NativePtr->Monitors.Data);
            int numMonitors = outputs.Length;
            IntPtr data = Marshal.AllocHGlobal(Unsafe.SizeOf<ImGuiPlatformMonitor>() * numMonitors);
            platformIO.NativePtr->Monitors = new ImVector(numMonitors, numMonitors, data);
            for (int i = 0; i < numMonitors; i++)
            {
                Rectangle r = outputs[i].DesktopBounds;
                ImGuiPlatformMonitorPtr monitor = platformIO.Monitors[i];
                monitor.DpiScale = 1f;
                monitor.MainPos = new Vector2(r.X, r.Y);
                monitor.MainSize = new Vector2(r.Width, r.Height);
                monitor.WorkPos = new Vector2(r.X, r.Y);
                monitor.WorkSize = new Vector2(r.Width, r.Height);
            }
        }

        #region Platformfunctions

        private void CreateWindow(ImGuiViewportPtr vp)
        {
            ImGuiWindow window = new ImGuiWindow(nodeContext, _strideDeviceContext, vp);
        }

        private void DestroyWindow(ImGuiViewportPtr vp)
        {
            if (vp.PlatformUserData != IntPtr.Zero)
            {
                var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;

                if (target != null)
                {
                    ImGuiWindow window = (ImGuiWindow)target;
                    window.Dispose();
                }

                vp.PlatformUserData = IntPtr.Zero;
            }
        }

        private void ShowWindow(ImGuiViewportPtr vp)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                window.Activate();
            }
        }

        private unsafe void GetWindowPos(ImGuiViewportPtr vp, Vector2* outPos)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                *outPos = new Vector2(window.Position.X, window.Position.Y);
            }
        }

        private void SetWindowPos(ImGuiViewportPtr vp, Vector2 pos)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                window.Position = new Int2((int)pos.X, (int)pos.Y);
            }
        }
        private unsafe void GetWindowSize(ImGuiViewportPtr vp, Vector2* outSize)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                *outSize = new Vector2(window.Size.X, window.Size.Y);
            }
        }

        private void SetWindowSize(ImGuiViewportPtr vp, Vector2 size)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                window.Size = new Int2((int)size.X, (int)size.Y);
            }
        }

        private void SetWindowFocus(ImGuiViewportPtr vp)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                window.Activate();
            }
        }

        private byte GetWindowFocus(ImGuiViewportPtr vp)
        {
            bool isActivated = false;
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                isActivated = window.IsActivated;
            }

            return isActivated ? (byte)1 : (byte)0;
        }

        private byte GetWindowMinimized(ImGuiViewportPtr vp)
        {
            bool isMinimized = false;
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;
                isMinimized = window.IsMinimized;
            }

            return isMinimized ? (byte)1 : (byte)0;
        }

        private unsafe void SetWindowTitle(ImGuiViewportPtr vp, IntPtr title)
        {
            var target = GCHandle.FromIntPtr(vp.PlatformUserData).Target;
            if (target != null)
            {
                ImGuiWindow window = (ImGuiWindow)target;

                byte* titlePtr = (byte*)title;
                int count = 0;
                while (titlePtr[count] != 0)
                {
                    count += 1;
                }
                window.Title = System.Text.Encoding.ASCII.GetString(titlePtr, count);
            }
        }
        #endregion Platformfunctions

        public void Dispose()
        {
            deviceHandle.Dispose();
            mainViewportWindow.Dispose();
        }
    }
}