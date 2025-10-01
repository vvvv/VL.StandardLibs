using ImGuiNET;
using Stride.Core.Mathematics;
using VL.Core.EditorAttributes;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Name = "Drag (Quaternion)", Category = "ImGui.Widgets", Tags = "number")]
    [WidgetType(WidgetType.Drag)]
    internal partial class DragQuaternion : DragWidget<Quaternion, float>
    {
        public DragQuaternion()
            : base(float.MinValue, float.MaxValue)
        {
        }

        protected override bool Drag(string label, ref Quaternion value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        {
            var yawPitchRoll = value.YawPitchRoll;
            yawPitchRoll = new Vector3(
                MathUtil.RadiansToRevolutions(yawPitchRoll.X),
                MathUtil.RadiansToRevolutions(yawPitchRoll.Y),
                MathUtil.RadiansToRevolutions(yawPitchRoll.Z));

            var x = yawPitchRoll.ToImGui();
            if (ImGuiNET.ImGui.DragFloat3(label, ref x, speed, min, max, format, flags))
            {
                x = new Vector3(
                    MathUtil.RevolutionsToRadians(x.X),
                    MathUtil.RevolutionsToRadians(x.Y),
                    MathUtil.RevolutionsToRadians(x.Z));

                value = Quaternion.RotationYawPitchRoll(x.X, x.Y, x.Z);

                return true;
            }
            return false;
        }

        //protected override bool Drag(string label, ref Quaternion value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        //{
        //    var x = new System.Numerics.Vector4(value.Axis, value.Angle);
        //    if (ImGuiNET.ImGui.DragFloat4(label, ref x, speed, min, max, format, flags))
        //    {
        //        value = Quaternion.RotationAxis(new Vector3(x.X, x.Y, x.Z), x.W);
        //        return true;
        //    }
        //    return false;
        //}

        //protected override bool Drag(string label, ref Quaternion value, float speed, float min, float max, string? format, ImGuiSliderFlags flags)
        //{
        //    var x = new System.Numerics.Vector4(value.ToArray());
        //    if (ImGuiNET.ImGui.DragFloat4(label, ref x, speed, min, max, format, flags))
        //    {
        //        value = new Quaternion(x.X, x.Y, x.Z, x.W);
        //        return true;
        //    }
        //    return false;
        //}
    }
}
