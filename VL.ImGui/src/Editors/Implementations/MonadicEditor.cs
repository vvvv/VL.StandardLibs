using ImGuiNET;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Reactive;
using static ImGuiNET.ImGui;

namespace VL.ImGui.Editors.Implementations
{
    internal class MonadicEditor
    {
        private static readonly MethodInfo s_genericCreateMethod = typeof(MonadicEditor).GetMethod(nameof(Create_Generic), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static IObjectEditor? Create(IChannel channel, ObjectEditorContext context)
        {
            var monadicType = channel.ClrTypeOfValues;
            var valueType = monadicType.GenericTypeArguments[0];
            return s_genericCreateMethod.MakeGenericMethod(monadicType, valueType).Invoke(null, parameters: [channel, context]) as IObjectEditor;
        }

        private static MonadicEditor<TMonad, TValue>? Create_Generic<TMonad, TValue>(IChannel<TMonad> channel, ObjectEditorContext context)
        {
            var factory = typeof(TMonad).GetMonadicFactory<TValue, TMonad>()!;
            var monadicValueEditor = factory.GetEditor();
            return new MonadicEditor<TMonad, TValue>(channel, context, factory.GetMonadBuilder(false), monadicValueEditor);
        }
    }

    internal class MonadicEditor<TMonad, TValue> : IObjectEditor, IDisposable
    {
        private readonly string textLabel, checkboxLabel;

        private readonly IChannel<TMonad> channel;
        private readonly IChannel<TValue> innerChannel;
        private readonly IMonadBuilder<TValue, TMonad> monadBuilder;
        private readonly IMonadicValueEditor<TValue, TMonad> monadicValueEditor;
        private readonly IObjectEditor innerEditor;
        private readonly IDisposable connection;

        public MonadicEditor(IChannel<TMonad> channel, ObjectEditorContext context, IMonadBuilder<TValue, TMonad> monadBuilder, IMonadicValueEditor<TValue, TMonad> monadicValueEditor)
        {
            this.channel = channel;
            this.monadicValueEditor = monadicValueEditor;
            this.monadBuilder = monadBuilder;
            this.innerChannel = Channel.Create(monadicValueEditor.GetValue(channel.Value!));
            this.innerEditor = context.Factory.CreateObjectEditor(innerChannel, context)!;
            this.connection = ConnectChannels();

            textLabel = $"##checkBox_{GetHashCode()}";
            checkboxLabel = $"##text_{GetHashCode()}";
        }

        private IDisposable ConnectChannels()
        {
            return channel.Merge(innerChannel,
                toB: m =>
                {
                    if (monadicValueEditor.HasValue(m))
                        return new Optional<TValue>(monadicValueEditor.GetValue(m));
                    else
                        return default;
                },
                toA: v => monadicValueEditor.SetValue(channel.Value!, v!),
                initialization: ChannelMergeInitialization.UseA,
                pushEagerlyTo: ChannelSelection.ChannelA);
        }

        public void Dispose()
        {
            connection.Dispose();
            if (innerEditor is IDisposable disposable)
                disposable.Dispose();
        }

        public void Draw(Context? context)
        {
            var hasValue = monadicValueEditor.HasValue(channel.Value);

            //BeginDisabled(disabled: !hasValue);

            if (hasValue)
            {
                innerEditor.Draw(context);
            }
            else
            {
                LabelText(textLabel, "Not set");
            }

            //EndDisabled();

            SameLine();

            if (Checkbox(checkboxLabel, ref hasValue))
            {
                if (hasValue)
                {
                    channel.Value = monadBuilder.Return(AppHost.Current.GetDefaultValue<TValue>()!);
                }
                else
                {
                    channel.Value = monadBuilder.Default();
                }
            }
        }
    }
}
