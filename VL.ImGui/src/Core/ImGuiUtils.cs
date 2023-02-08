using ImGuiNET;
using Stride.Core.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using VL.Core;
using VL.Lib.Collections;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    internal static class ImGuiUtils
    {
        public static bool InputObject(string label, ref object? value)
        {
            bool isModified = false;

            if (value is string s)
            {
                if (isModified = ImGui.InputText(label, ref s, 255))
                    value = s;
            }
            else if (value is int i1)
            {
                if (isModified = ImGui.InputInt(label, ref i1))
                    value = i1;
            }
            else if (value is Int2 i2)
            {
                if (isModified = ImGui.InputInt2(label, ref i2.X))
                    value = i2;
            }
            else if (value is Int3 i3)
            {
                if (isModified = ImGui.InputInt3(label, ref i3.X))
                    value = i3;
            }
            else if (value is Int4 i4)
            {
                if (isModified = ImGui.InputInt4(label, ref i4.X))
                    value = i4;
            }
            else if (value is float f)
            {
                if (isModified = ImGui.InputFloat(label, ref f))
                    value = f;
            }
            else if (value is Vector2 v2)
            {
                var x = Unsafe.As<Vector2, System.Numerics.Vector2>(ref v2);
                if (isModified = ImGui.InputFloat2(label, ref x))
                    value = Unsafe.As<System.Numerics.Vector2, Vector2>(ref x);
            }
            else if (value is Vector3 v3)
            {
                var x = Unsafe.As<Vector3, System.Numerics.Vector3>(ref v3);
                if (isModified = ImGui.InputFloat3(label, ref x))
                    value = Unsafe.As<System.Numerics.Vector3, Vector3>(ref x);
            }
            else if (value is Vector4 v4)
            {
                var x = Unsafe.As<Vector4, System.Numerics.Vector4>(ref v4);
                if (isModified = ImGui.InputFloat4(label, ref x))
                    value = Unsafe.As<System.Numerics.Vector4, Vector4>(ref x);
            }
            else if (value is double d)
            {
                if (isModified = ImGui.InputDouble(label, ref d))
                    value = d;
            }
            else if (value is bool b)
            {
                if (isModified = ImGui.Checkbox(label, ref b))
                    value = b;
            }
            else if (value is Color4 color4)
            {
                var x = Unsafe.As<Color4, System.Numerics.Vector4>(ref color4);
                if (isModified = ImGui.ColorEdit4(label, ref x))
                    value = Unsafe.As<System.Numerics.Vector4, Color4>(ref x);
            }
            else if (value is Enum @enum)
            {
                //ImGui.BeginCombo(label, value.ToString());

                var currentItem = (int)Convert.ChangeType(@enum, typeof(int));
                var names = Enum.GetNames(@enum.GetType());
                if (isModified = ImGui.Combo(label, ref currentItem, names, names.Length))
                    value = Enum.ToObject(@enum.GetType(), currentItem);

                //ImGui.EndCombo();
            }
            else if (value is IVLObject vObject)
            {
                var properties = vObject.Type.Properties;
                if (properties.Count > 0)
                {
                    ImGui.BeginGroup();
                    foreach (var p in properties)
                    {
                        var v = p.GetValue(vObject);
                        if (InputObject(p.OriginalName, ref v))
                        {
                            isModified = true;
                            vObject = p.WithValue(vObject, v);
                        }
                    }
                    value = vObject;
                    ImGui.EndGroup();
                }
            }
            else if (value is ISpread spread)
            {
                if (ImGui.BeginListBox(label))
                {
                    for (int i = 0; i < spread.Count; i++)
                    {
                        var v = spread.GetItem(i);
                        if (InputObject($"{i}", ref v))
                        {
                            isModified = true;
                            spread = spread.SetItem(i, v);
                        }
                    }
                    value = spread;

                    ImGui.EndListBox();
                }
            }
            else if (value is IDictionary dict)
            {
                if (ImGui.BeginTable(label, 2, ImGuiNET.ImGuiTableFlags.Borders | ImGuiNET.ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Key", ImGuiNET.ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Value", ImGuiNET.ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableHeadersRow();

                    var i = 0;
                    foreach (DictionaryEntry entry in dict)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();

                        var key = entry.Key;
                        ImGui.TextUnformatted(key.ToString());

                        ImGui.TableNextColumn();

                        var v = entry.Value;
                        if (InputObject($"##{key.GetHashCode()}", ref v))
                        {
                            isModified = true;
                            dict = SetItem(dict, key, v);
                        }

                        i++;
                    }
                    value = dict;

                    ImGui.EndTable();
                }
            }
            else
                ImGui.TextUnformatted(value?.ToString() ?? "NULL");

            return isModified;
        }

        static IDictionary SetItem(IDictionary dict, object key, object? value)
        {
            var dictType = dict.GetType();
            var toBuilderMethod = dictType.GetMethod(nameof(ImmutableDictionary<string, object>.ToBuilder));
            if (toBuilderMethod != null)
            {
                if (toBuilderMethod.Invoke(dict, null) is IDictionary builder)
                {
                    builder[key] = value;
                    var toImmutableMethod = builder.GetType().GetMethod(nameof(ImmutableDictionary<string, object>.Builder.ToImmutable));
                    return toImmutableMethod?.Invoke(builder, null) as IDictionary ?? dict;
                }
                return dict;
            }
            else
            {
                dict[key] = value;
                return dict;
            }
        }

        public static unsafe bool InputDouble(string label, ref double value, double step, double stepFast, string? format, ImGuiInputTextFlags flags)
        {
            return ImGui.InputScalar(label, ImGuiDataType.Double, new IntPtr(Unsafe.AsPointer(ref value)), new IntPtr(Unsafe.AsPointer(ref step)), new IntPtr(Unsafe.AsPointer(ref stepFast)), format, flags);
        }

        public static unsafe bool DragDouble(string label, ref double value, float speed, double min, double max, string? format, ImGuiSliderFlags flags)
        {
            return ImGui.DragScalar(label, ImGuiDataType.Double, new IntPtr(Unsafe.AsPointer(ref value)), speed, new IntPtr(Unsafe.AsPointer(ref min)), new IntPtr(Unsafe.AsPointer(ref max)), format, flags);
        }

        public static unsafe bool SliderDouble(string label, ref double value, double min, double max, string? format, ImGuiSliderFlags flags)
        {
            return ImGui.SliderScalar(label, ImGuiDataType.Double, new IntPtr(Unsafe.AsPointer(ref value)), new IntPtr(Unsafe.AsPointer(ref min)), new IntPtr(Unsafe.AsPointer(ref max)), format, flags);
        }

        public static unsafe bool VSliderDouble(string label, System.Numerics.Vector2 size, ref double value, double min, double max, string? format, ImGuiSliderFlags flags)
        {
            return ImGui.VSliderScalar(label, size, ImGuiDataType.Double, new IntPtr(Unsafe.AsPointer(ref value)), new IntPtr(Unsafe.AsPointer(ref min)), new IntPtr(Unsafe.AsPointer(ref max)), format, flags);
        }
    }

    public static class ImGuiConversion
    {
        public static float ToImGuiScaling = VL.UI.Core.DIPHelpers.DIPFactor() * 100f;
        public static float FromImGuiScaling = 1.0f / (VL.UI.Core.DIPHelpers.DIPFactor() * 100f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 ToImGui(this Color4 v)
        {
            return Unsafe.As<Color4, System.Numerics.Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 ToImGui(this Vector2 v)
        {
            return Unsafe.As<Vector2, System.Numerics.Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 FromHectoToImGui(this Vector2 v)
        {
            v = new Vector2(v.X * ToImGuiScaling, v.Y * ToImGuiScaling);
            return Unsafe.As<Vector2, System.Numerics.Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector3 ToImGui(this Vector3 v)
        {
            return Unsafe.As<Vector3, System.Numerics.Vector3>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector4 ToImGui(this Vector4 v)
        {
            return Unsafe.As<Vector4, System.Numerics.Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Numerics.Vector2 FromHectoToImGui(this Size2F v)
        {
            v = new Size2F(v.Width * ToImGuiScaling, v.Height * ToImGuiScaling);
            return Unsafe.As<Size2F, System.Numerics.Vector2>(ref v);
        }

        public static float FromHectoToImGui(this float v) => v * ToImGuiScaling;

        //TO VL

        public static float ToVLHecto(this float v) => v * FromImGuiScaling;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVLHecto(this System.Numerics.Vector2 v)
        {
            v = new System.Numerics.Vector2(v.X * FromImGuiScaling, v.Y * FromImGuiScaling);
            return Unsafe.As<System.Numerics.Vector2, Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVL(this System.Numerics.Vector2 v)
        {
            return Unsafe.As<System.Numerics.Vector2, Vector2>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVL(this System.Numerics.Vector3 v)
        {
            return Unsafe.As<System.Numerics.Vector3, Vector3>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVL(this System.Numerics.Vector4 v)
        {
            return Unsafe.As<System.Numerics.Vector4, Vector4>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToVLColor4(this System.Numerics.Vector4 v)
        {
            return Unsafe.As<System.Numerics.Vector4, Color4>(ref v);
        }

        public static IEnumerable<TOut> Select<T, TOut>(this RangeAccessor<T> range, Func<T, TOut> selector) where T : struct
        {
            for (int i = 0; i < range.Count; i++)
                yield return selector(range[i]);
        }
    }

}
