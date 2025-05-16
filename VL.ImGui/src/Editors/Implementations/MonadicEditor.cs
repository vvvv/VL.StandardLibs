using ImGuiNET;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            // In case a viewer is requested let the real viewers take over
            if (context.ViewOnly)
                return null;

            var monadicType = channel.ClrTypeOfValues;
            var valueType = monadicType.GenericTypeArguments[0];
            return s_genericCreateMethod.MakeGenericMethod(monadicType, valueType).Invoke(null, parameters: [channel, context]) as IObjectEditor;
        }

        private static MonadicEditor<TMonad, TValue>? Create_Generic<TMonad, TValue>(IChannel<TMonad> channel, ObjectEditorContext context)
        {
            var innerChannel = ChannelHelpers.CreateChannelOfType<TValue>();
            var innerEditor = context.Factory.CreateObjectEditor(innerChannel, context);
            if (innerEditor is null)
                return null;

            var monadicValueEditor = MonadicUtils.GetMonadicEditor<TValue, TMonad>()!;
            return new MonadicEditor<TMonad, TValue>(channel, context, monadicValueEditor, innerChannel, innerEditor);
        }
    }

    internal class MonadicEditor<TMonad, TValue> : IObjectEditor, IDisposable
    {
        private readonly string textLabel, checkboxLabel;

        private readonly ObjectEditorContext editorContext;
        private readonly IChannel<TMonad> channel;
        private readonly IChannel<TValue> innerChannel;
        private readonly IMonadicValueEditor<TValue, TMonad> monadicValueEditor;
        private readonly IObjectEditor innerEditor;
        private readonly IDisposable connection;

        public MonadicEditor(IChannel<TMonad> channel, ObjectEditorContext context, IMonadicValueEditor<TValue, TMonad> monadicValueEditor, IChannel<TValue> innerChannel, IObjectEditor innerEditor)
        {
            this.editorContext = context;
            this.channel = channel;
            this.monadicValueEditor = monadicValueEditor;

            this.innerChannel = innerChannel;
            if (monadicValueEditor.HasValue(channel.Value))
                this.innerChannel.Value = monadicValueEditor.GetValue(channel.Value);
            this.innerEditor = innerEditor;
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
                        return new Optional<TValue>(monadicValueEditor.GetValue(m)!);
                    else
                        return default;
                },
                toA: v =>
                {
                    return monadicValueEditor.SetValue(channel.Value!, v!);
                },
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

            BeginGroup();
            try
            {
                if (!monadicValueEditor.HasCustomDefault)
                {
                    if (editorContext.ViewOnly)
                        BeginDisabled();

                    if (Checkbox(checkboxLabel, ref hasValue))
                    {
                        if (hasValue)
                        {
                            channel.Value = monadicValueEditor.Create(AppHost.Current.GetDefaultValue<TValue>()!);
                        }
                        else
                        {
                            channel.Value = AppHost.Current.GetDefaultValue<TMonad>();
                        }
                    }

                    if (editorContext.ViewOnly)
                        EndDisabled();

                    SameLine();
                }

                var x = GetCursorPosX() - GetStyle().ItemSpacing.X;
                SetNextItemWidth(CalcItemWidth() - x);

                if (hasValue)
                {
                    innerEditor.Draw(context);
                }
                else
                {
                    var s = "";
                    BeginDisabled();
                    InputTextWithHint(editorContext.LabelForImGUI, "Not set", ref s, 100);
                    EndDisabled();
                }
            }
            finally
            {
                EndGroup();
            }
        }
    }
}
