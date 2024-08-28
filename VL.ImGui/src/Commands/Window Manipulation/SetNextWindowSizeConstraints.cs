using ImGuiNET;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Commands", GenerateRetained = false, IsStylable = false)]
    internal unsafe partial class SetNextWindowSizeConstraints : Widget
    {
        private static readonly ImGuiSizeCallback s_nativeCallback = NativeCallback;
        private nint userCallback;

        public Vector2 Min { private get; set; }

        public Vector2 Max { private get; set; }

        public WindowSizeCallback? Callback { private get; set; }

        public bool Enabled { private get; set; } = true;

        internal override unsafe void UpdateCore(Context context)
        {
            if (Enabled)
            {
                if (Callback is not null)
                {
                    userCallback = Marshal.GetFunctionPointerForDelegate(Callback);
                    ImGuiNET.ImGui.SetNextWindowSizeConstraints(Min.FromHectoToImGui(), Max.FromHectoToImGui(), s_nativeCallback, userCallback);
                }
                else
                {
                    ImGuiNET.ImGui.SetNextWindowSizeConstraints(Min.FromHectoToImGui(), Max.FromHectoToImGui());
                }
            }
        }

        private static unsafe void NativeCallback(ImGuiSizeCallbackData* data)
        {
            var userCallback = Marshal.GetDelegateForFunctionPointer<WindowSizeCallback>(new nint(data->UserData));
            if (userCallback is null)
                return;

            var x = new WindowSizeCallbackData() 
            { 
                Position = data->Pos.ToVLHecto(),
                CurrentSize = data->CurrentSize.ToVLHecto(),
                DesiredSize = data->DesiredSize.ToVLHecto()
            };

            userCallback(ref x);

            data->Pos = x.Position.FromHectoToImGui();
            data->CurrentSize = x.CurrentSize.FromHectoToImGui();
            data->DesiredSize = x.DesiredSize.FromHectoToImGui();
        }
    }

    public delegate void WindowSizeCallback(ref WindowSizeCallbackData data);

    public struct WindowSizeCallbackData
    {
        public Vector2 Position { get; set; }
        public Vector2 CurrentSize { get; set; }
        public Vector2 DesiredSize { get; set; }
    }
}
