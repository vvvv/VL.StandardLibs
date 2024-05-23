using MessagePack.Formatters;
using MessagePack;
using System;
using System.Collections.Generic;
using SkiaSharp;
using VL.Serialization.MessagePack.Formatters;

namespace VL.Serialization.MessagePack
{
    sealed class SkiaResolver : IFormatterResolver
    {
        public static readonly SkiaResolver Instance = new SkiaResolver();

        private SkiaResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                try
                {
                    Formatter = (IMessagePackFormatter<T>?)SkiaResolverGetFormatterHelper.GetFormatter(typeof(T));
                }
                catch (TypeInitializationException)
                {
                    // SkiaSharp not present
                    Formatter = null;
                }
            }
        }
    }

    internal static class SkiaResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new Dictionary<Type, object>()
        {
            { typeof(SKTypeface), new SKTypefaceFormatter() }
        };

        internal static object? GetFormatter(Type t)
        {
            object? formatter;
            if (FormatterMap.TryGetValue(t, out formatter))
            {
                return formatter;
            }

            return null;
        }
    }
}
