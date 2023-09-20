using System.Collections.Immutable;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reflection;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class ObjectEditor<T> : IObjectEditor, IDisposable
    {
        readonly Dictionary<IVLPropertyInfo, IObjectEditor?> editors = new Dictionary<IVLPropertyInfo, IObjectEditor?>();
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
                                    pushEagerlyTo: ChannelSelection.ChannelA));

                            var attributes = property.GetAttributes<Attribute>().ToSpread();
                            propertyChannel.Attributes().Value = attributes;
                            var label = attributes.OfType<LabelAttribute>().FirstOrDefault()?.Label ?? property.OriginalName;
                            var contextForProperty = new ObjectEditorContext(instance.AppHost, factory, label, ViewOnly: parentContext.ViewOnly, PrimitiveOnly: parentContext.PrimitiveOnly);
                            editor = editors[property] = factory.CreateObjectEditor(propertyChannel, contextForProperty);
                        }
                        else
                        {
                            editor = editors[property] = null;
                        }
                    }

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
                                }
                            }
                        }
                        else
                        {
                            editor.Draw(context);
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

        static bool IsVisible(IVLPropertyInfo property)
        {
            var browseable = property.GetAttributes<BrowsableAttribute>().FirstOrDefault();
            return browseable is null || browseable.Browsable;
        }
    }

}
