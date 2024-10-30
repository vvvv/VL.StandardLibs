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

                var xO = x.GetOrder();
                var yO = y.GetOrder();

                if (xO.HasNoValue && yO.HasNoValue)
                    return x!.NameForTextualCode.CompareTo(y!.NameForTextualCode);

                return xO.ValueOrDefault() - yO.ValueOrDefault();
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
                            propertyChannel.Attributes().Value = property.Attributes;
                            
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
                                propertyChannel.GetLabel().ValueOrDefault_ForReferenceType(property.OriginalName)!;

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
                        //this is needed as long as we don't have a proper way to react on property changes of a mutable parent object
                        propertyChannel.EnsureObject(property.GetValue((IVLObject)channel.Value));
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
                var d = channel.GetDescription();
                if (d.HasValue)
                {
                    var tooltipText = d.Value;
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
