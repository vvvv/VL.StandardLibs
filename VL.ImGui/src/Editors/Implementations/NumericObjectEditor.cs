using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors.Implementations
{
    internal class NumericObjectEditor : IObjectEditor, IDisposable
    {
        IObjectEditor intEditor;
        IChannel<int> channelForInt;
        IDisposable merge;
        private IChannel channel;
        private Type staticType;

        public NumericObjectEditor(IChannel channel, 
            DefaultObjectEditorFactory defaultObjectEditorFactory, 
            ObjectEditorContext context, Type staticType)
        {
            this.channel = channel;
            this.staticType = staticType;

            channelForInt = ChannelHelpers.CreateChannelOfType<int>();
            intEditor = defaultObjectEditorFactory.CreateObjectEditor(channelForInt, context)!;

            merge =
                ChannelHelpers.Merge<object, int>(
                channel.ChannelOfObject,
                channelForInt,
                v => (int)Convert.ChangeType(v, typeof(int))!,
                v => Convert.ChangeType((v), staticType),
                initialization: ChannelMergeInitialization.UseA,
                pushEagerlyTo: ChannelSelection.Both);

            // a lot of attributes get lost via this merge.
            // much better than this wrapper would be to have widgets for all types.

            // SliderByte
        }

        T Clamp<T>(int v) where T: IMinMaxValue<T>, INumber<T>
        {
            v = int.Max(v, Convert.ToInt32(T.MinValue));
            v = int.Min(v, Convert.ToInt32(T.MaxValue));
            return (T)Convert.ChangeType(v, staticType);
        }

        public void Dispose()
        {
            (intEditor as IDisposable)?.Dispose();
            merge.Dispose();
        }

        public void Draw(Context? context)
        {
            intEditor.Draw(context);
        }


        bool NeedsMoreThanOneLine => intEditor.NeedsMoreThanOneLine;
        bool HasContentToDraw => intEditor.HasContentToDraw;
    }
}
