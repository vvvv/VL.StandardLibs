using Stride.Core.Mathematics;
using System;
using VL.Core;

namespace VL.Lib.Mathematics
{
    static partial class Serialization
    {
        public static void RegisterSerializers(IVLFactory factory)
        {
            factory.RegisterSerializer<Circle, CircleSerializer>();
            factory.RegisterSerializer<Range<object>, RangeSerializer<object>>();
        }

        sealed class CircleSerializer : ISerializer<Circle>
        {
            public object Serialize(SerializationContext context, Circle value)
            {
                var @array = new float[] { value.Center.X, value.Center.Y, value.Radius };
                return context.Serialize(null, @array);
            }

            public Circle Deserialize(SerializationContext context, object content, Type type)
            {
                var @array = context.Deserialize<float[]>(content, null);
                if (@array.Length == 3)
                    return new Circle(new Vector2(@array[0], @array[1]), @array[2]);
                return Circle.Empty;
            }
        }

        sealed class RangeSerializer<T> : ISerializer<Range<T>>
        {
            public object Serialize(SerializationContext context, Range<T> value)
            {
                return new object[] {
                    context.Serialize("From", value.From),
                    context.Serialize("To", value.To)
                };
            }

            public Range<T> Deserialize(SerializationContext context, object content, Type type)
            {
                var from = context.Deserialize<T>(content, "From");
                var to = context.Deserialize<T>(content, "To");
                return new Range<T>(from, to);
            }
        }
    }
}
