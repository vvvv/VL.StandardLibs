using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using VL.Core.Utils;
using VL.Lib.Collections;


namespace VL.Serialization.Raw
{
    public static class RawSerialization
    {
        private static Dictionary<Type, (string serializeMethod, string deserializeMethod)> s_methods = new()
        {
            { typeof(Spread<>), (nameof(SpreadToBytes), nameof(BytesToSpread)) },
            { typeof(ImmutableArray<>), (nameof(ImmutableArrayToBytes), nameof(BytesToImmutableArray)) },
            { typeof(ReadOnlyMemory<>), (nameof(ReadOnlyMemoryToBytes), nameof(BytesToReadOnlyMemory)) },
            { typeof(ArraySegment<>), (nameof(ArraySegmentToBytes), nameof(BytesToArraySegment)) },
        };

        /// <summary>
        /// Supports blitable types, strings, and collections thereof.
        /// </summary>
        public static ReadOnlyMemory<byte> Serialize<T>(T input)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            if (ToMemoryMap.TryGetValue(typeof(T), out Func<object, ReadOnlyMemory<byte>>? ToDelegate))
            {
                return ToDelegate(input);
            }
            else
            {
                if (input is ReadOnlyMemory<byte> memory)
                    return memory;

                if (input is ArraySegment<byte> segment)
                    return segment.AsMemory();

                // Struct, Vector2,Vector3 ...
                if (typeof(T).IsBlitable())
                {
                    var blitableToBytes = typeof(RawSerialization).GetMethod(nameof(BlitableToBytes), BindingFlags.NonPublic | BindingFlags.Static);

                    if (blitableToBytes != null)
                    {
                        blitableToBytes = blitableToBytes.MakeGenericMethod(new[] { typeof(T) });
                        var blitableToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(blitableToBytes);
                        ToMemoryMap.TryAdd(typeof(T), blitableToBytesDel);

                        return blitableToBytesDel(input);
                    }
                }
                // Array
                else if (typeof(T).IsArray)
                {
                    var elementType = typeof(T).GetElementType();
                    if (elementType != null && elementType.IsBlitable())
                    {
                        var arrayToBytes = typeof(RawSerialization).GetMethod(nameof(ArrayToBytes), BindingFlags.NonPublic | BindingFlags.Static);
                        if (arrayToBytes != null)
                        {
                            arrayToBytes = arrayToBytes.MakeGenericMethod(new[] { elementType });
                            var arrayToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(arrayToBytes);
                            ToMemoryMap.TryAdd(typeof(T), arrayToBytesDel);

                            return arrayToBytesDel(input);
                        }
                    }
                }
                // String
                else if (typeof(T) == typeof(String))
                {
                    Func<object, ReadOnlyMemory<byte>> stringToBytesDel = (obj) => StringToBytes((string)obj);
                    ToMemoryMap.TryAdd(typeof(T), stringToBytesDel);

                    return stringToBytesDel(input);
                }
                else if (IsGenericType(typeof(T), out var elementType) && elementType.IsBlitable() && s_methods.TryGetValue(typeof(T).GetGenericTypeDefinition(), out var methods))
                {
                    var toBytes = typeof(RawSerialization).GetMethod(methods.serializeMethod, BindingFlags.NonPublic | BindingFlags.Static)!;
                    toBytes = toBytes.MakeGenericMethod([elementType]);
                    var toBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(toBytes);
                    ToMemoryMap.TryAdd(typeof(T), toBytesDel);

                    return toBytesDel(input);
                }
            }

            throw new SerializationException($"Can't serialize {typeof(T)}");
        }

        public static T Deserialize<T>(ReadOnlyMemory<byte> input)
        {
            if (FromMemoryMap.TryGetValue(typeof(T), out Func<ReadOnlyMemory<byte>, object>? FromDelegate))
            {
                return (T)FromDelegate(input);
            }
            else
            {
                if (input is T t)
                    return t;

                if (MemoryMarshal.TryGetArray(input, out var segment) && segment is T tSegment)
                    return tSegment;

                // Struct, Vector2,Vector3 ...
                if (typeof(T).IsBlitable())
                {
                    var bytesToBlitable = typeof(RawSerialization).GetMethod(nameof(BytesToBlitable), BindingFlags.NonPublic | BindingFlags.Static);

                    if (bytesToBlitable != null)
                    {
                        bytesToBlitable = bytesToBlitable.MakeGenericMethod(new[] { typeof(T) });

                        var bytesToBlitableDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToBlitable);
                        FromMemoryMap.TryAdd(typeof(T), bytesToBlitableDel);

                        return (T)bytesToBlitableDel(input);
                    }
                }
                // Array
                else if (typeof(T).IsArray && typeof(T).HasElementType)
                {
                    var genericType = typeof(T).GetElementType();
                    if (genericType != null && genericType.IsBlitable())
                    {
                        var bytesToArray = typeof(RawSerialization).GetMethod(nameof(BytesToArray), BindingFlags.NonPublic | BindingFlags.Static);
                        if (bytesToArray != null)
                        {
                            bytesToArray = bytesToArray.MakeGenericMethod(new[] { genericType });
                            var bytesToArrayDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToArray);
                            FromMemoryMap.TryAdd(typeof(T), bytesToArrayDel);

                            return (T)bytesToArrayDel(input);
                        }
                    }
                }
                // String
                else if (typeof(T) == typeof(String))
                {
                    Func<ReadOnlyMemory<byte>, object> bytesToStringDel = (b) => BytesToString(b);
                    FromMemoryMap.TryAdd(typeof(T), bytesToStringDel);

                    return (T)bytesToStringDel(input);
                }
                else if (IsGenericType(typeof(T), out var elementType) && s_methods.TryGetValue(typeof(T).GetGenericTypeDefinition(), out var methods))
                {
                    var fromBytes = typeof(RawSerialization).GetMethod(methods.deserializeMethod, BindingFlags.NonPublic | BindingFlags.Static)!;
                    fromBytes = fromBytes.MakeGenericMethod(new[] { elementType });
                    var fromBytesDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(fromBytes);
                    FromMemoryMap.TryAdd(typeof(T), fromBytesDel);

                    return (T)fromBytesDel(input);
                }
            }

            throw new SerializationException($"Can't deserialize {typeof(T)}");
        }

        private static readonly ConcurrentDictionary<Type, Func<object, ReadOnlyMemory<byte>>> ToMemoryMap = new();
        private static readonly ConcurrentDictionary<Type, Func<ReadOnlyMemory<byte>, object>> FromMemoryMap = new();

        private static bool IsGenericType(Type type, [NotNullWhen(true)] out Type? elementType)
        {
            if (type.IsGenericType && type.GenericTypeArguments.Length == 1)
            {
                elementType = type.GenericTypeArguments[0];
                return true;
            }

            elementType = default;
            return false;
        }

        private static Func<Tinput, TOutput> StaticMethodDelegate<Tinput, TOutput>(MethodInfo method)
        {
            var parameter = method.GetParameters().Single();
            var argument = Expression.Parameter(typeof(Tinput), "argument");
            var methodCall = Expression.Call(
                null,
                method,
                Expression.Convert(argument, parameter.ParameterType)
                );
            return Expression.Lambda<Func<Tinput, TOutput>>(
                Expression.Convert(methodCall, typeof(TOutput)),
                argument
                ).Compile();
        }

        private static ReadOnlyMemory<byte> BlitableToBytes<T>(T input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.AsBytes(new ReadOnlyMemory<T>(new T[] { input }));
        }

        private unsafe static T BytesToBlitable<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            T result;
            input.Span.CopyTo(new Span<byte>(&result, sizeof(T)));
            return result;
        }

        private static ReadOnlyMemory<byte> SpreadToBytes<T>(Spread<T> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input.AsMemory());
        }
        private static Spread<T> BytesToSpread<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            var span = MemoryMarshal.Cast<byte, T>(input.Span);
            var builder = Pooled.GetArrayBuilder<T>();
            builder.Value.Count = span.Length;
            span.CopyTo(builder.Value.AsSpan());
            return Spread.Create<T>(builder.ToImmutableAndFree());
        }

        private static ReadOnlyMemory<byte> ImmutableArrayToBytes<T>(ImmutableArray<T> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input.AsMemory());
        }
        private static ImmutableArray<T> BytesToImmutableArray<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            var span = MemoryMarshal.Cast<byte, T>(input.Span);
            var builder = Pooled.GetArrayBuilder<T>();
            builder.Value.Count = span.Length;
            span.CopyTo(builder.Value.AsSpan());
            return builder.ToImmutableAndFree();
        }
        private static ReadOnlyMemory<byte> ArrayToBytes<T>(T[] input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input.AsMemory());
        }
        private static T[] BytesToArray<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            var span = MemoryMarshal.Cast<byte, T>(input.Span);
            T[] values = new T[span.Length];
            span.CopyTo(values.AsSpan());
            return values;
        }

        private static ReadOnlyMemory<byte> StringToBytes(string input)
        {
            return new ReadOnlyMemory<byte>(UTF8Encoding.UTF8.GetBytes(input));
        }

        private static string BytesToString(ReadOnlyMemory<byte> input)
        {
            return UTF8Encoding.UTF8.GetString(input.Span);
        }

        private static ReadOnlyMemory<T> BytesToReadOnlyMemory<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<byte, T>(input);
        }

        private static ReadOnlyMemory<byte> ReadOnlyMemoryToBytes<T>(ReadOnlyMemory<T> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input);
        }

        private static ArraySegment<T> BytesToArraySegment<T>(ReadOnlyMemory<byte> input) where T : unmanaged
        {
            if (MemoryMarshal.TryGetArray(ReadOnlyMemoryExtensions.Cast<byte, T>(input), out var segment))
                return segment;

            return new ArraySegment<T>(BytesToArray<T>(input));
        }

        private static ReadOnlyMemory<byte> ArraySegmentToBytes<T>(ArraySegment<T> input) where T : unmanaged
        {
            return ReadOnlyMemoryExtensions.Cast<T, byte>(input);
        }
    }
}
