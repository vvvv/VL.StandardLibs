using System.Reactive.Disposables;
using System.Reflection.Emit;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    abstract class ListEditorBase<TList, T> : IObjectEditor, IDisposable
                where TList : IReadOnlyList<T?>
    {
        private readonly List<(IObjectEditor? editor, IDisposable ownership)> editors = new List<(IObjectEditor?, IDisposable)>();
        private readonly IChannel<TList> channel;
        private readonly ObjectEditorContext editorContext;
        private readonly string label;
        private IDisposable mainSpreadSync;
        private bool collapsed = true;

        public ListEditorBase(IChannel<TList> channel, ObjectEditorContext editorContext)
        {
            this.channel = channel;
            this.mainSpreadSync = channel.Subscribe(RemoveSuperfluousEntries);
            this.editorContext = editorContext;
            this.label = $"##{GetHashCode()}";
        }

        public void Dispose()
        {
            this.mainSpreadSync.Dispose();
            foreach (var item in editors)
                item.ownership.Dispose();
            editors.Clear();
        }

        public void Draw(Context? context)
        {
            var list = channel.Value;
            if (list is null)
                return;

            RemoveSuperfluousEntries(list);

            if (list.Count == 0)
                return;

            var count = list.Count.ToString();

            ImGui.SetNextItemOpen(!collapsed);

            if (ImGui.TreeNodeEx($"[{count}]{label}", ImGuiNET.ImGuiTreeNodeFlags.CollapsingHeader))
            {
                collapsed = false;

                //ImGui.Indent(-ImGui.GetStyle().IndentSpacing);

                if (ImGui.BeginListBox(label))
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        var (editor, _) = editors.ElementAtOrDefault(i);
                        if (i >= editors.Count)
                        {
                            // Setup channel for item
                            var itemChannel = ChannelHelpers.CreateChannelOfType<T>();
                            var j = i;

                            var ownership = new CompositeDisposable
                            {
                                channel.Merge(itemChannel,
                                    c => c != null ? c[j] : default,
                                    item => channel.Value != null ? SetItem(channel.Value, j, item) : default,
                                    initialization: ChannelMergeInitialization.UseA,
                                    pushEagerlyTo: ChannelSelection.ChannelA)
                            };

                            editor = editorContext.Factory.CreateObjectEditor(itemChannel.ChannelOfObject, editorContext);
                            if (editor is IDisposable disposable)
                                ownership.Add(disposable);

                            editors.Add((editor, ownership));
                        }

                        ImGui.PushID(i);
                        try
                        {
                            editor?.Draw(context);
                        }
                        finally
                        {
                            ImGui.PopID();
                        }
                    }

                    ImGui.EndListBox();
                }
                ImGui.TreePop();
            }
            else
            {
                collapsed = true;
            }
        }

        private void RemoveSuperfluousEntries(TList? list)
        {
            if (list is null)
                return;

            for (int i = editors.Count - 1; i >= list.Count; i--)
            {
                editors[i].ownership.Dispose();
                editors.RemoveAt(i);
            }
        }

        protected abstract TList SetItem(TList list, int i, T? item);
    }
}
