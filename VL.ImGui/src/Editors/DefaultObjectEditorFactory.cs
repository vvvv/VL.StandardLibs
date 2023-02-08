using Stride.Core.Mathematics;
using System.Reflection;
using VL.Core;
using VL.Core.EditorAttributes;
using VL.ImGui.Widgets;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.ImGui.Editors
{
    public sealed partial class DefaultObjectEditorFactory : IObjectEditorFactory
    {
        public IObjectEditor? CreateObjectEditor(Channel channel, ObjectEditorContext context)
        {
            var staticType = channel.ClrTypeOfValues;

            // Is there a widget for exactly that type?
            var widgetType = channel.Attributes.Value.OfType<WidgetTypeAttribute>().FirstOrDefault()?.WidgetType ?? GetDefaultWidgetType(staticType);
            var channelWidgetType = typeof(ChannelWidget<>).MakeGenericType(staticType);
            var widgetClass = channelWidgetType.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && channelWidgetType.IsAssignableFrom(t) && t.GetConstructor(Array.Empty<Type>()) != null)
                .OrderBy(t => widgetType == t.GetCustomAttribute<WidgetTypeAttribute>()?.WidgetType ? 0 : 1)
                .FirstOrDefault();
            if (widgetClass != null)
            {
                var editorType = typeof(ObjectEditorBasedOnChannelWidget<>).MakeGenericType(staticType);
                return (IObjectEditor?)Activator.CreateInstance(editorType, new object[] { channel, context, widgetClass });
            }

            var typeInfo = TypeRegistry.Default.GetTypeInfo(staticType);
            if (staticType.IsAbstract || staticType == typeof(object))
                return new AbstractObjectEditor(channel, context, typeInfo);

            if (staticType.IsEnum)
            {
                var editorType = typeof(EnumEditor<>).MakeGenericType(staticType);
                return (IObjectEditor?)Activator.CreateInstance(editorType, new object[] { channel, context });
            }

            if (staticType.IsArray)
                return Activator.CreateInstance(typeof(ArrayEditor<>).MakeGenericType(staticType.GetElementType()!), new object[] { channel, context }) as IObjectEditor;

            if (staticType.IsConstructedGenericType)
            {
                if (staticType.GetGenericTypeDefinition() == typeof(Spread<>))
                    return Activator.CreateInstance(typeof(SpreadEditor<>).MakeGenericType(staticType.GenericTypeArguments), new object[] { channel, context }) as IObjectEditor;
                // More collections
            }

            {
                var editorType = typeof(ObjectEditor<>).MakeGenericType(staticType);
                return (IObjectEditor?)Activator.CreateInstance(editorType, new object[] { channel, context, typeInfo });
            }
        }

        private static WidgetType GetDefaultWidgetType(Type type)
        {
            if (IsNumericType(type) || IsVectorType(type))
                return WidgetType.Drag;

            if (type == typeof(string))
                return WidgetType.Input;

            return WidgetType.Default;
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsVectorType(Type type)
        {
            return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4);
        }
    }
}
