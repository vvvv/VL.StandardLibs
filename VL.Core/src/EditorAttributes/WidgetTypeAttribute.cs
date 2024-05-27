using System;

namespace VL.Core.EditorAttributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class WidgetTypeAttribute : Attribute
    {
        public WidgetTypeAttribute(WidgetType widgetType)
        {
            WidgetType = widgetType;
        }

        public WidgetType WidgetType { get; }

        public override string ToString()
        {
            return $"WidgetType: {WidgetType}";
        }
    }
}
