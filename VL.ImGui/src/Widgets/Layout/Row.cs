using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Core;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", GenerateImmediate = false)]
    internal sealed partial class Row : Widget
    {
        public IEnumerable<Widget> Children { get; set; } = Enumerable.Empty<Widget>();

        internal override void UpdateCore(Context context)
        {
            var count = Children.Count(x => x != null);
            if (count > 0)
            {
                var width = ImGuiNET.ImGui.GetContentRegionAvail().X / count;

                ImGuiNET.ImGui.BeginGroup();
                ImGuiNET.ImGui.PushItemWidth(width);
                try
                {
                    var i = 0;
                    foreach (var child in Children)
                    {
                        if (i++ > 0)
                            ImGuiNET.ImGui.SameLine();
                        if (child is null)
                            continue;
                        else
                            context.Update(child);
                    }
                }
                finally
                {
                    ImGuiNET.ImGui.PopItemWidth();
                    ImGuiNET.ImGui.EndGroup();
                }
            }
        }
    }
}
