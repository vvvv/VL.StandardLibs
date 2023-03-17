using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using VL.Core;
using VL.Core.Diagnostics;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride
{
    class StrideNodeDesc<TInstance> : IVLNodeDescription, IEnumerable, IInfo
        where TInstance : new()
    {
        private List<PinDescription> inputs;
        private List<StateOutput> outputs;
        internal Type stateOutputType;

        public StrideNodeDesc(IVLNodeDescriptionFactory factory, string name = default, string category = default, Type stateOutputType = default)
        {
            Factory = factory;
            Name = name ?? typeof(TInstance).Name;
            Category = category ?? string.Empty;
            this.stateOutputType = stateOutputType ?? typeof(TInstance);
        }

        public bool CopyOnWrite { get; set; } = true;

        public IVLNodeDescriptionFactory Factory { get; }

        public string Name { get; }

        public string Category { get; }

        public bool Fragmented => true;

        public IReadOnlyList<IVLPinDescription> Inputs
        {
            get
            {
                return inputs ?? (inputs = Compute().ToList());

                IEnumerable<PinDescription> Compute()
                {
                    var categoryOrdering = typeof(TInstance).GetCustomAttributes<CategoryOrderAttribute>()
                        .ToDictionary(a => a.Name, a => a.Order);

                    var properties = typeof(TInstance).GetStrideProperties()
                        .GroupBy(p => p.Category)
                        .OrderBy(g => g.Key != null ? categoryOrdering.ValueOrDefault(g.Key, 0) : 0)
                        .SelectMany(g => g.OrderBy(p => p.Order).ThenBy(p => p.Name));

                    var instance = new TInstance();
                    foreach (var p in properties)
                    {
                        var property = p.Property;
                        var propertyType = property.GetPropertyType();
                        var pinDescType = GetPinDescType(propertyType);

                        object defaultValue = null;
                        if (property is PropertyInfo pi)
                            defaultValue = pi.GetValue(instance);
                        else if (property is FieldInfo fi)
                            defaultValue = fi.GetValue(instance);

                        var defaultValueProperty = property.GetCustomAttribute<DefaultValueAttribute>();
                        if (defaultValueProperty != null && defaultValueProperty.Value != null && defaultValueProperty.Value.GetType() == propertyType)
                            defaultValue = defaultValueProperty.Value;

                        if (pinDescType == typeof(ComputeColorPinDesc))
                            defaultValue = ShaderFXUtils.Constant(GetDefaultColor(defaultValue));
                        else if (pinDescType == typeof(ComputeScalarPinDesc))
                            defaultValue = ShaderFXUtils.Constant(GetDefaultFloat(defaultValue));

                        var name = p.Name;
                        // Prepend the category to the name (if not already done so)
                        var category = p.Category;
                        if (category != null)
                        {
                            if (category == "Metal Flakes")
                                category = "Metal Flake";

                            if (!name.StartsWith(category))
                                name = $"{category} {name}";
                        }
                        yield return (PinDescription)Activator.CreateInstance(pinDescType, property, name, defaultValue);
                    }
                }
            }
        }

        static float GetDefaultFloat(object defaultValue)
        {
            if (defaultValue is ComputeTextureScalar ts)
                return ts.FallbackValue.Value;
            else if (defaultValue is ComputeFloat f)
                return f.Value;

            return 0;
        }

        static Vector4 GetDefaultColor(object defaultValue)
        {
            if (defaultValue is ComputeTextureColor tc)
                return tc.FallbackValue.Value;
            else if (defaultValue is ComputeColor f)
                return f.Value;

            return Vector4.One;
        }

        static Type GetPinDescType(Type propertyType)
        {
            if (propertyType.IsValueType)
                return typeof(StructPinDec<>).MakeGenericType(propertyType);
            if (TryGetElementType(propertyType, out var elementType))
                return typeof(ListPinDesc<,,>).MakeGenericType(propertyType, propertyType, elementType);
            if (typeof(IComputeScalar).IsAssignableFrom(propertyType))
                return typeof(ComputeScalarPinDesc);
            if (typeof(IComputeColor).IsAssignableFrom(propertyType))
                return typeof(ComputeColorPinDesc);
            return typeof(ClassPinDec<>).MakeGenericType(propertyType);
        }

        static bool TryGetElementType(Type type, out Type elementType)
        {
            var typeArgs = type.GenericTypeArguments;
            if (typeArgs.Length == 1)
            {
                elementType = typeArgs[0];
                return typeof(IList<>).MakeGenericType(elementType).IsAssignableFrom(type);
            }
            if (type.BaseType != null)
            {
                return TryGetElementType(type.BaseType, out elementType);
            }
            else
            {
                elementType = default;
                return false;
            }
        }

        public IReadOnlyList<IVLPinDescription> Outputs
        {
            get
            {
                return outputs ?? (outputs = Compute().ToList());

                IEnumerable<StateOutput> Compute()
                {
                    yield return new StateOutput(stateOutputType);
                }
            }
        }

        public IEnumerable<Message> Messages => Enumerable.Empty<Message>();

        public IObservable<object> Invalidated => Observable.Empty<object>();

        public string Summary => typeof(TInstance).GetSummary();

        public string Remarks => typeof(TInstance).GetRemarks();

        public IVLNode CreateInstance(NodeContext context)
        {
            return new StrideNode<TInstance>(context, this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        class StateOutput : IVLPinDescription
        {
            public StateOutput(Type type)
            {
                Type = type;
            }

            public string Name => "Output";

            public Type Type { get; }

            public object DefaultValue => null;
        }
    }
}
