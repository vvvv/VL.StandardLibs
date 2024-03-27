using ImGuiNET;

using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using VL.Core;
using Stride.Engine;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using VL.Stride.Games;
using VL.Stride;
using Stride.Games;
using Stride.Rendering;


namespace VL.ImGui.Stride
{
    using ImGui = ImGuiNET.ImGui;
    using Vector2 = System.Numerics.Vector2;

    public partial class ImGuiWindows : IDisposable
    {
        //VL 
        private readonly NodeContext nodeContext;
        private readonly ImGuiWindow mainViewportWindow;

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

            

            IntPtr context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);
            ImGuiIOPtr io = ImGui.GetIO();

            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

            ImGuiPlatformIOPtr platformIO = ImGui.GetPlatformIO();
            ImGuiViewportPtr mainViewport = platformIO.Viewports[0];

            mainViewportWindow = new ImGuiWindow(this.nodeContext, mainViewport);
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
        }

        public void Update(IGraphicsRendererBase IRenderer)
        {
            mainViewportWindow.Update(IRenderer);
        }


        #region Platformfunctions
        
        private void CreateWindow(ImGuiViewportPtr vp)
        {
            ImGuiWindow window = new ImGuiWindow(nodeContext, vp);
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
            mainViewportWindow.Dispose();
        }
    }
}
