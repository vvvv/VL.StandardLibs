using System.Reactive.Disposables;
using VL.Core.EditorAttributes;
using VL.Core;
using VL.Lib.Reactive;
using System.Collections.Immutable;
using System.Reflection;

namespace VL.ImGui.Editors
{
    using ImGui = ImGuiNET.ImGui;

    sealed class AbstractObjectEditor : IObjectEditor, IDisposable
    {
        readonly IChannel publicChannel;
        readonly IDisposable publicChannelSubscription;
        readonly IObjectEditorFactory factory;

        readonly SerialDisposable editorOwnership = new SerialDisposable();
        readonly SerialDisposable privateChannelSubscription = new SerialDisposable();
        readonly ObjectEditorContext editorContext;

        readonly ImmutableArray<IVLTypeInfo> possibleTypes;
        readonly string[]? possibleTypeEntries;

        IChannel<object>? privateChannel;
        Type? currentEditorType;
        IObjectEditor? currentEditor;
        bool isBusy;
        WidgetLabel widgetLabel = new();

        public AbstractObjectEditor(IChannel channel, ObjectEditorContext editorContext, IVLTypeInfo typeInfo)
        {
            this.publicChannel = channel;
            this.editorContext = editorContext;
            this.factory = editorContext.Factory;

            if (!editorContext.ViewOnly && 
                channel.TryGetAttribute<TypeSelectorAttribute>(out var typeSelectorAttribute) && 
                (typeInfo.IsInterface || (!typeInfo.IsPatched && typeInfo.ClrType.IsAbstract)))
            {
                this.possibleTypes = GetImplementingTypes(typeInfo, typeSelectorAttribute).ToImmutableArray();
                this.possibleTypeEntries = possibleTypes.Select(GetLabel).ToArray();
            }

            OnNext(channel.Object);
            publicChannelSubscription = channel.ChannelOfObject.Subscribe(OnNext);
        }

        public void Dispose()
        {
            publicChannelSubscription.Dispose();
            privateChannelSubscription.Dispose();
            editorOwnership.Dispose();
        }

        public bool NeedsMoreThanOneLine => currentEditor != null ? currentEditor.NeedsMoreThanOneLine : false;

        void OnNext(object? value)
        {
            if (isBusy)
                return;

            var type = value?.GetType();
            if (type is null || type == typeof(object) || type.IsNotPublic /* We can only wrap public types, otherwise we might run into type load exception when building adaptive nodes*/)
            {
                privateChannelSubscription.Disposable = null;
                privateChannel?.Dispose();
                privateChannel = null;
                return;
            }

            if (privateChannel is null || type != privateChannel.ClrTypeOfValues)
            {
                // Build new channel using the runtime type
                privateChannel?.Dispose();
                privateChannel = ChannelHelpers.CreateChannelOfType(type);
                privateChannel.Object = value;
                privateChannelSubscription.Disposable = privateChannel.Subscribe(v =>
                {
                    // Push to upstream channel
                    PushValue(publicChannel, v);
                });
            }

            // Push to private channel
            PushValue(privateChannel, value!);
        }

        void PushValue(IChannel dst, object? value)
        {
            if (isBusy)
                return;

            isBusy = true;
            try
            {
                dst.Object = value;
            }
            finally
            {
                isBusy = false;
            }
        }

        public void Draw(Context? context)
        {
            context = context.Validate();
            if (context is null)
                return;

            // Type selector?
            if (possibleTypeEntries != null)
            {
                var currentItem = GetIndex(publicChannel.Object);
                if (ImGui.Combo($"Type##{GetHashCode()}", ref currentItem, possibleTypeEntries, possibleTypeEntries.Length))
                {
                    // Create a new value of the specified type
                    // This will implicitly rebuild the privat channel
                    publicChannel.Object = CreateNewValue(possibleTypes.ElementAtOrDefault(currentItem))!;
                }
            }

            if (privateChannel is null)
                return;

            if (currentEditorType != privateChannel.ClrTypeOfValues)
            {
                currentEditorType = privateChannel.ClrTypeOfValues;
                currentEditor = factory.CreateObjectEditor(privateChannel, editorContext);
                editorOwnership.Disposable = currentEditor as IDisposable;
            }

            if (currentEditor != null)
            {
                currentEditor.Draw(context);
            }
            else if (publicChannel.Object is not null)
            {
                var s = publicChannel.Object.ToString();
                if (!string.IsNullOrEmpty(editorContext.Label))
                    ImGui.LabelText(widgetLabel.Update(editorContext.Label), s);
                else
                    ImGui.TextUnformatted(s);
            }
            else
            {
                var s = "NULL";
                if (!string.IsNullOrEmpty(editorContext.Label))
                    ImGui.LabelText(widgetLabel.Update(editorContext.Label), s);
                else
                    ImGui.TextUnformatted(s);
            }
        }

        IEnumerable<IVLTypeInfo> GetImplementingTypes(IVLTypeInfo typeInfo, TypeSelectorAttribute typeSelectorAttribute)
        {
            var clrType = typeInfo.ClrType;
            var typeRegistry = editorContext.AppHost.TypeRegistry;
            foreach (var vlType in typeRegistry.RegisteredTypes)
            {
                if (!typeSelectorAttribute.IsMatch(vlType.Name))
                    continue;

                var type = vlType.ClrType;
                if (type.IsAbstract || type.IsGenericTypeDefinition)
                    continue;

                if (!clrType.IsAssignableFrom(type))
                    continue;

                if (!vlType.IsPatched && !HasDefaultConstructor(vlType))
                    continue;

                yield return vlType;
            }
        }

        static bool HasDefaultConstructor(IVLTypeInfo type) => type.ClrType.GetConstructor(Array.Empty<Type>()) != null;

        static string GetLabel(IVLTypeInfo typeInfo)
        {
            var labelAttribute = typeInfo.ClrType.GetCustomAttribute<LabelAttribute>();
            if (labelAttribute != null)
                return labelAttribute.Label;
            var displayAttribute = typeInfo.ClrType.GetCustomAttribute<Stride.Core.DisplayAttribute>();
            if (displayAttribute != null)
                return displayAttribute.Name;
            var displayAttribute2 = typeInfo.ClrType.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
            if (displayAttribute2?.Name != null)
                return displayAttribute2.Name;
            return typeInfo.ToString()!;
        }

        int GetIndex(object? value)
        {
            if (value is null)
                return -1;

            var typeInfo = editorContext.AppHost.TypeRegistry.GetTypeInfo(value.GetType());
            return possibleTypes.IndexOf(typeInfo);
        }

        object? CreateNewValue(IVLTypeInfo? typeInfo)
        {
            if (typeInfo is null)
                return default;
            if (typeInfo.IsPatched)
                return AppHost.Current.CreateInstance(typeInfo);
            else
                return Activator.CreateInstance(typeInfo.ClrType);
        }
    }

}
