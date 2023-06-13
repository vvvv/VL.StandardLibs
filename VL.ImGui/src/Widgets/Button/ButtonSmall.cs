using System.Reactive;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Button with FramePadding=(0,0) to easily embed within text
    /// </summary>
    [GenerateNode(Name ="Button (Small)", Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    internal partial class ButtonSmall : ChannelWidget<Unit>, IHasLabel
    {
        public string? Label { get; set; }

        internal override void UpdateCore(Context context)
        {
            Update();
            if (ImGuiNET.ImGui.SmallButton(widgetLabel.Update(Label)))
                Value = Unit.Default;
        }
    }
}
