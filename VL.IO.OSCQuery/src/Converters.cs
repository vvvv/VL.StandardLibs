using System;
using Newtonsoft.Json;
using Stride.Core.Mathematics;

namespace IO.OSCQuery;

public static class Converters
{
    public class RGBAConverter : JsonConverter
    {
        public RGBAConverter()
        {
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color4)value;
            color.ToBgra(out var r, out var g, out var b, out var a); 
            writer.WriteRawValue($"\"#{Convert.ToHexString(new byte[] {r, g, b, a})}\"");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color4);
        }
    }
}