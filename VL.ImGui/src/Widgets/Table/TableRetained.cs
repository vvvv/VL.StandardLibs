using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using VL.Core;
using Stride.Core.Mathematics;

namespace VL.ImGui.Widgets
{
    //[GenerateNode(Category = "ImGui.Widgets", Name = "Table", GenerateImmediate = false)]
    internal sealed partial class TableRetained : Widget
    {
        public IEnumerable<Widget> ColumnDescriptions { get; set; } = Enumerable.Empty<Widget>();

        public IEnumerable<Widget> Columns { get; set; } = Enumerable.Empty<Widget>();

        public string? Label { get; set; }

        public Vector2 Size { private get; set; }

        public float InnerWidth { private get; set; }

        public bool ShowHeader { get; set; }

        public ImGuiNET.ImGuiTableFlags Flags { private get; set; }

        internal override void UpdateCore(Context context)
        {
            var count = ColumnDescriptions.Count(x => x != null);

            if (count > 0)
            {
                if (ImGuiNET.ImGui.BeginTable(Context.GetLabel(this, Label), count, Flags, Size.FromHectoToImGui(), InnerWidth))
                {
                    try
                    {
                        foreach (var desc in ColumnDescriptions)
                        {
                            if (desc is null)
                                continue;
                            else
                                context.Update(desc);
                        }

                        if (ShowHeader)
                            ImGuiNET.ImGui.TableHeadersRow();

                        foreach (var col in Columns)
                        {
                            if (col is null)
                                continue;
                            else
                            {
                                ImGuiNET.ImGui.TableNextColumn();
                                context.Update(col);
                            }
                        }
                    }
                    finally
                    {
                        ImGuiNET.ImGui.EndTable();
                    }
                }
            }
        }
    }
}
