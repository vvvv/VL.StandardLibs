﻿using Stride.Core.Mathematics;
using System.Reactive;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "rgba, hsv, hsl")]
    internal partial class ColorButton : ChannelWidget<Bang>
    {
        public Color4 Color { private get; set; }

        public Vector2 Size { private get; set; }

        public ImGuiNET.ImGuiColorEditFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.ColorButton(widgetLabel.Update(label.Value), Color.ToImGui(), Flags, Size.FromHectoToImGui()))
                Value = Lib.Reactive.Bang.Trigger;
        }
    }
}
