using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.ImGui.Widgets;
using VL.Lib.Collections;
using VL.Lib.Reactive;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Collections;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class ObjectEditor<T> : IObjectEditor, IDisposable
    {
        class PropertyOrderComparer : IComparer<IVLPropertyInfo>
        {
            public int Compare(IVLPropertyInfo? x, IVLPropertyInfo? y)
            {
                if (x == null || y == null || x.Equals(y))
                    return 0;

                var xDisplay = x.GetAttributes<DisplayAttribute>().FirstOrDefault();
                var yDisplay = y.GetAttributes<DisplayAttribute>().FirstOrDefault();

                if (xDisplay == null && yDisplay == null)
                    return x!.NameForTextualCode.CompareTo(y!.NameForTextualCode);

                return (xDisplay?.GetOrder() ?? 0) - (yDisplay?.GetOrder() ?? 0);
            }
        }

        readonly SortedDictionary<IVLPropertyInfo, (IObjectEditor?, string, IChannel)> editors = new(new PropertyOrderComparer());
        readonly CompositeDisposable subscriptions = new CompositeDisposable();
        readonly ObjectEditorContext parentContext;
        readonly IChannel<T> channel;
        readonly IObjectEditorFactory factory;
        readonly IVLTypeInfo typeInfo;

        public ObjectEditor(IChannel<T> channel, ObjectEditorContext editorContext, IVLTypeInfo typeInfo)
        {
            this.channel = channel;
            this.parentContext = editorContext;
            this.factory = editorContext.Factory;
            this.typeInfo = typeInfo;
        }

        public void Dispose()
        {
            subscriptions.Dispose();
        }

        public bool NeedsMoreThanOneLine => true;

        public void Draw(Context? context)
        {
            context = context.Validate();
            if (context is null)
                return;

            var typeInfo = this.typeInfo;
            if (channel.Value is IVLObject instance)
            {
                foreach (var property in typeInfo.Properties)
                {
                    if (!editors.TryGetValue(property, out var editor))
                    {
                        var old = editors.Keys.FirstOrDefault(p => p.OriginalName == property.OriginalName);
                        if (old != default)
                            editors.Remove(old);

                        if (IsVisible(property))
                        {
                            // Setup channel
                            var propertyChannel = ChannelHelpers.CreateChannelOfType(property.Type);
                            var attributes = property.GetAttributes<Attribute>().ToSpread();
                            propertyChannel.Attributes().Value = attributes;
                            
                            subscriptions.Add(
                                channel.ChannelOfObject.Merge(
                                    propertyChannel.ChannelOfObject,
                                    (object? v) => property.GetValue((IVLObject)channel.Value),
                                    v => (T)property.WithValue((IVLObject)channel.Value, v),
                                    initialization: ChannelMergeInitialization.UseA,
                                    pushEagerlyTo: ChannelSelection.Both));
                            // this channel is private. So it should be fine to spam it (ChannelSelection.Both).
                            // If we wouldn't spam, the changed properties in a deeply mutating object structure would not get updated.

                            var label =
                                propertyChannel.GetAttribute<LabelAttribute>()?.Label ??
                                propertyChannel.GetAttribute<DisplayAttribute>()?.Name ??
                                property.OriginalName;

                            var contextForProperty = parentContext.CreateSubContext(label);
                            editor = editors[property] = 
                                (factory.CreateObjectEditor(propertyChannel, contextForProperty),
                                label, propertyChannel);
                        }
                        else
                        {
                            editor = editors[property] = (null, null!, null!);
                        }
                    }
                }

                foreach (var (property, (editor, label, propertyChannel)) in editors)
                { 
                    if (editor != null)
                    {
                        if (editor.NeedsMoreThanOneLine)
                        {
                            if (ImGui.TreeNode(label))
                            {
                                try
                                {
                                    DrawTooltip(propertyChannel);
                                    editor.Draw(context);
                                }
                                finally
                                {
                                    ImGui.TreePop();
                                }
                            }
                            else
                                DrawTooltip(propertyChannel); 
                        }
                        else
                        {
                            editor.Draw(context);
                            DrawTooltip(propertyChannel);
                        }
                    }
                }
            }
            else if (channel.Value is not null)
            {
                // TODO: General case
                ImGui.TextUnformatted(channel.Value.ToString());
            }
            else
            {
                ImGui.TextUnformatted("NULL");
            }
        }

        private static void DrawTooltip(IChannel channel)
        {
            if (ImGui.IsItemHovered())
            {
                var displayAttribute = channel.GetAttribute<DisplayAttribute>();
                if (displayAttribute != null)
                {
                    var tooltipText = displayAttribute.Description;
                    if (tooltipText != null)
                        ImGui.SetItemTooltip(tooltipText);
                }
            }
        }

        static bool IsVisible(IVLPropertyInfo property)
        {
            var browsable = property.GetAttributes<BrowsableAttribute>().FirstOrDefault();
            return browsable is null || browsable.Browsable;
        }
    }

}
