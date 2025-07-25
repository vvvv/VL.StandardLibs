﻿using ImGuiNET;
using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.EditorAttributes;
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
            foreach (var c in channel.Components)
                innerChannel.AddComponent(c);
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

                    if (Checkbox(checkboxLabel, ref hasValue)) // only executes in the moment when checkbox gets turned on or offf
                    {
                        if (hasValue)
                        {
                            TValue defaultValue = channel.GetDefault<TValue>().TryGetValue(AppHost.Current.GetDefaultValue<TValue>()!);
                            defaultValue = MonadicEditorExtensions.TryConstrain(defaultValue, channel.GetMin<TValue>(), channel.GetMax<TValue>());
                            channel.Value = monadicValueEditor.Create(defaultValue);
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

    static class MonadicEditorExtensions
    {
        public static TValue TryConstrain<TValue>(TValue value, Optional<TValue> min, Optional<TValue> max)
        {
            if (value is null)
                return value;

            // Check if TValue implements INumber<TValue>
            var inumberInterface = typeof(TValue)
                .GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Numerics.INumber<>));

            if (inumberInterface != null)
            {
                // Call the generic method via reflection
                var method = typeof(MonadicEditorExtensions)
                    .GetMethod(nameof(TryConstrainNumber), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TValue));
                return (TValue)method.Invoke(null, new object[] { value, min, max })!;
            }

            // Not a number, just return the value
            return value;
        }

        private static TValue TryConstrainNumber<TValue>(TValue value, Optional<TValue> min, Optional<TValue> max)
            where TValue : System.Numerics.INumber<TValue>
        {
            if (min.HasValue && max.HasValue)
                return TValue.Clamp(value, min.Value, max.Value);
            else if (min.HasValue)
                return TValue.Max(value, min.Value);
            else if (max.HasValue)
                return TValue.Min(value, max.Value);
            return value;
        }
    }
}
