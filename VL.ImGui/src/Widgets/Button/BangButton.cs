using System.Reactive;
using System.Reactive.Disposables;
using VL.Core.EditorAttributes;
using VL.Lib.Reactive;

namespace VL.ImGui.Widgets
{
    [GenerateNode(Category = "ImGui.Widgets", Button = true, Tags = "bang")]
    [WidgetType(WidgetType.Bang)]
    internal partial class BangButton : ChannelWidget<Unit>
    {
        public ImGuiNET.ImGuiDir Direction { private get; set; } = ImGuiNET.ImGuiDir.None;

        private readonly SerialDisposable channelSubscription = new();

        bool bang;
        bool selfbang;

        internal override void UpdateCore(Context context)
        {
            Update();
            selfbang = ImGuiNET.ImGui.ArrowButton(widgetLabel.Update(label.Value), selfbang | bang ? ImGuiNET.ImGuiDir.Down : Direction);
            if (selfbang)
                Value = Unit.Default;
            bang = false;

            widgetLabel.DrawLabelInSameLine();
        }

        protected override void OnChannelChanged(IChannel<Unit>? channel, IChannel<Unit>? old)
        {
            if (channel != null)
                channelSubscription.Disposable = channel.Subscribe(v => bang = true);
            else
                channelSubscription.Disposable = null;
        }
    }
}
