using ImGuiNET;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;
using VL.Lib.IO;
using Path = VL.Lib.IO.Path;

namespace VL.ImGui.Widgets
{
    using ImGui = ImGuiNET.ImGui;

    [GenerateNode(Name = "Input (Path)", Category = "ImGui.Widgets", Tags = "edit")]
    [WidgetType(WidgetType.Input)]
    internal partial class InputPath : ChannelWidget<Path>
    {
        private readonly IsDirectoryValueSelector isDirectoryValueSelector = new ();
        private readonly FilterValueSelector filterValueSelector = new();
        private readonly WidgetLabel textLabel = new();

        public InputPath()
        {
            AddValueSelector(isDirectoryValueSelector);
            AddValueSelector(filterValueSelector);
        }

        public Optional<bool> IsDirectory { get => default; set => isDirectoryValueSelector.SetPinValue(value); }

        public Optional<string> Filter { get => default; set => filterValueSelector.SetPinValue(value); }

        Path? lastframeValue;

        internal override void UpdateCore(Context context)
        {
            ImGui.BeginGroup();

            try
            {

                var l = label.HasValue ? label.Value : "...";
                var padding = ImGui.GetStyle().FramePadding;
                var width = ImGui.CalcItemWidth() - (padding.Y * 2 + ImGui.GetStyle().ItemInnerSpacing.X + ImGui.CalcTextSize(l).X);
                ImGui.SetNextItemWidth(width);

                var value = Update() ?? Path.Default;
                var s = value.ToString();
                if (ImGui.InputText(textLabel.Update(null), ref s, ushort.MaxValue))
                    SetValueIfChanged(lastframeValue, value = new Path(s), default);

                ImGui.SameLine(0, ImGui.GetStyle().ItemInnerSpacing.X);

                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(padding.Y, padding.Y));

                if (ImGui.Button(widgetLabel.Update(l)))
                {
                    var filter = filterValueSelector.Value;
                    var isDirectory = isDirectoryValueSelector.Value;
                    var initialDirectory = value.IsDirectory ? value : value.Parent;
                    var result = isDirectory ? 
                        PlatformServices.Default.ShowDirectoryDialog(initialDirectory, value) : 
                        PlatformServices.Default.ShowFileDialog(initialDirectory, filter);
                    if (result.HasValue)
                        SetValueIfChanged(lastframeValue, value = new Path(result.Value), default);
                }

                ImGui.PopStyleVar();

                lastframeValue = value;
            } 
            finally
            { 
                ImGui.EndGroup(); 
            }
        }

        sealed class IsDirectoryValueSelector : ValueSelector<bool>
        {
            public IsDirectoryValueSelector() : base(false)
            {
            }

            public override void Update(Spread<Attribute> attributes)
            {
                SetAttributeValue(default);

                foreach (var a in attributes)
                    if (a is PathAttribute p)
                        SetAttributeValue(p.IsDirectory);
            }
        }

        sealed class FilterValueSelector : ValueSelector<string>
        {
            public FilterValueSelector() : base(PathAttribute.DefaultFilter)
            {
            }

            public override void Update(Spread<Attribute> attributes)
            {
                SetAttributeValue(default);

                foreach (var a in attributes)
                    if (a is PathAttribute p)
                        SetAttributeValue(p.Filter);
            }
        }
    }
}
