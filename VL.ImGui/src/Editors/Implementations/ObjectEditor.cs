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
                if (x == null || y == null)
                    return 0;

                var xDisplay = GetDisplayAttribute(x!);
                var yDisplay = GetDisplayAttribute(y!);

                if (xDisplay != null && yDisplay != null)
                    return xDisplay.Order - yDisplay.Order;

                return x!.NameForTextualCode.CompareTo(y!.NameForTextualCode);
            }
        }

        readonly SortedDictionary<IVLPropertyInfo, IObjectEditor?> editors = new(new PropertyOrderComparer());
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
                        if (IsVisible(property))
                        {
                            // Setup channel
                            var propertyChannel = ChannelHelpers.CreateChannelOfType(property.Type);
                            subscriptions.Add(
                                channel.ChannelOfObject.Merge(
                                    propertyChannel.ChannelOfObject,
                                    (object? v) => property.GetValue((IVLObject)channel.Value),
                                    v => (T)property.WithValue((IVLObject)channel.Value, v),
                                    initialization: ChannelMergeInitialization.UseA,
                                    pushEagerlyTo: ChannelSelection.Both));
                            // this channel is private. So it should be fine to spam it (ChannelSelection.Both).
                            // If we wouldn't spam, the changed properties in a deeply mutating object structure would not get updated.

                            var attributes = property.GetAttributes<Attribute>().ToSpread();
                            propertyChannel.Attributes().Value = attributes;
                            var label = attributes.OfType<LabelAttribute>().FirstOrDefault()?.Label ?? property.OriginalName;
                            var contextForProperty = parentContext.CreateSubContext(label);
                            editor = editors[property] = factory.CreateObjectEditor(propertyChannel, contextForProperty);
                        }
                        else
                        {
                            editor = editors[property] = null;
                        }
                    }
                }

                foreach (var (property, editor) in editors)
                { 
                    if (editor != null)
                    {
                        if (editor.NeedsMoreThanOneLine)
                        {
                            if (ImGui.TreeNode(property.OriginalName))
                            {
                                try
                                {
                                    editor.Draw(context);
                                }
                                finally
                                {
                                    ImGui.TreePop();
                                    DrawTooltip(property); // not working?!
                                }
                            }
                        }
                        else
                        {
                            editor.Draw(context);
                            DrawTooltip(property);
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

        private static void DrawTooltip(IVLPropertyInfo property)
        {
            if (ImGui.IsItemHovered())
            {
                DisplayAttribute? displayAttribute = GetDisplayAttribute(property);
                if (displayAttribute != null)
                {
                    var tooltipText = displayAttribute.Description ?? displayAttribute.Name ?? displayAttribute.ShortName;
                    if (tooltipText != null)
                        ImGui.SetItemTooltip(tooltipText);
                }
            }
        }

        private static DisplayAttribute? GetDisplayAttribute(IVLPropertyInfo property)
        {
            return property.GetAttributes<DisplayAttribute>().FirstOrDefault();
        }

        static bool IsVisible(IVLPropertyInfo property)
        {
            var browseable = property.GetAttributes<BrowsableAttribute>().FirstOrDefault();
            return browseable is null || browseable.Browsable;
        }
    }

}
