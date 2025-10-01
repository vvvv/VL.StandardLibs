using System;
using MessagePack;
using MessagePack.Formatters;
using SkiaSharp;

namespace VL.Serialization.MessagePack.Formatters
{
    sealed class SKTypefaceFormatter : IMessagePackFormatter<SKTypeface?>
    {
        public void Serialize(ref MessagePackWriter writer, SKTypeface? value, MessagePackSerializerOptions options)
        {
            if (value is null)
                writer.WriteNil();
            else
            {
                writer.WriteArrayHeader(4);
                writer.Write(value.FamilyName);
                writer.Write(value.FontWeight);
                writer.Write(value.FontWidth);
                writer.Write((int)value.FontSlant);
            }
        }

        public SKTypeface? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
                return null;

            options.Security.DepthStep(ref reader);

            var count = reader.ReadArrayHeader();

            var familyName = default(string);
            var weight = default(int);
            var width = default(int);
            var slant = default(SKFontStyleSlant);
            for (int i = 0; i < count; i++)
            {
                switch (i)
                {
                    case 0:
                        familyName = reader.ReadString();
                        break;
                    case 1:
                        weight = reader.ReadInt32();
                        break;
                    case 2:
                        width = reader.ReadInt32();
                        break;
                    case 3:
                        slant = (SKFontStyleSlant)reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            reader.Depth--;

            return SKTypeface.FromFamilyName(familyName, weight, width, slant);
        }
    }
}
