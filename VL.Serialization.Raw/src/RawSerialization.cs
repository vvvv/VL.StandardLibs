using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using VL.Core.Utils;
using VL.Lib.Collections;


namespace VL.Serialization.Raw
{
    public static class RawSerialization
    {
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
                // Struct, Vector2,Vector3 ...
                if (typeof(T).IsBlitable())
                {
                    var blitableToBytes = typeof(RawSerialization).GetMethod(nameof(BlitableToBytes), BindingFlags.NonPublic | BindingFlags.Static);

                    if (blitableToBytes != null)
                    {
                        blitableToBytes = blitableToBytes.MakeGenericMethod(new[] { typeof(T) });
                        var BlitableToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(blitableToBytes);
                        ToMemoryMap.TryAdd(typeof(T), BlitableToBytesDel);

                        return BlitableToBytesDel(input);
                    }
                }
                // Spread of Struct 
                else if (typeof(ISpread).IsAssignableFrom(typeof(T)))
                {
                    if (typeof(T).IsGenericType)
                    {
                        var genericType = typeof(T).GetGenericArguments()[0];
                        if (genericType != null && genericType.IsBlitable())
                        {
                            var spreadToBytes = typeof(RawSerialization).GetMethod(nameof(SpreadToBytes), BindingFlags.NonPublic | BindingFlags.Static);
                            if (spreadToBytes != null)
                            {
                                spreadToBytes = spreadToBytes.MakeGenericMethod(new[] { genericType });
                                var SpreadToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(spreadToBytes);
                                ToMemoryMap.TryAdd(typeof(T), SpreadToBytesDel);

                                return SpreadToBytesDel(input);
                            }
                        }
                    }
                }
                // ImmutableArray
                else if (typeof(T).Name.StartsWith("ImmutableArray"))
                {
                    if (typeof(T).IsGenericType)
                    {
                        var genericType = typeof(T).GetGenericArguments()[0];
                        if (genericType != null && genericType.IsBlitable())
                        {
                            var immutableArrayToBytes = typeof(RawSerialization).GetMethod(nameof(ImmutableArrayToBytes), BindingFlags.NonPublic | BindingFlags.Static);
                            if (immutableArrayToBytes != null)
                            {
                                immutableArrayToBytes = immutableArrayToBytes.MakeGenericMethod(new[] { genericType });
                                var ImmutableArrayToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(immutableArrayToBytes);
                                ToMemoryMap.TryAdd(typeof(T), ImmutableArrayToBytesDel);

                                return ImmutableArrayToBytesDel(input);
                            }
                        }
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
                            var ArrayToBytesDel = StaticMethodDelegate<object, ReadOnlyMemory<byte>>(arrayToBytes);
                            ToMemoryMap.TryAdd(typeof(T), ArrayToBytesDel);

                            return ArrayToBytesDel(input);
                        }
                    }
                }
                // String
                else if (typeof(T) == typeof(String))
                {
                    Func<object, ReadOnlyMemory<byte>> StringToBytesDel = (obj) => StringToBytes((string)obj);
                    ToMemoryMap.TryAdd(typeof(T), StringToBytesDel);

                    return StringToBytesDel(input);
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
                // Struct, Vector2,Vector3 ...
                if (typeof(T).IsBlitable())
                {
                    var bytesToBlitable = typeof(RawSerialization).GetMethod(nameof(BytesToBlitable), BindingFlags.NonPublic | BindingFlags.Static);

                    if (bytesToBlitable != null)
                    {
                        bytesToBlitable = bytesToBlitable.MakeGenericMethod(new[] { typeof(T) });

                        var BytesToBlitableDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToBlitable);
                        FromMemoryMap.TryAdd(typeof(T), BytesToBlitableDel);

                        return (T)BytesToBlitableDel(input);
                    }
                }
                // Spread of Struct 
                else if (typeof(ISpread).IsAssignableFrom(typeof(T)))
                {
                    if (typeof(T).IsGenericType)
                    {
                        var genericType = typeof(T).GetGenericArguments()[0];
                        if (genericType != null && genericType.IsBlitable())
                        {
                            var bytesToSpread = typeof(RawSerialization).GetMethod(nameof(BytesToSpread), BindingFlags.NonPublic | BindingFlags.Static);
                            if (bytesToSpread != null)
                            {
                                bytesToSpread = bytesToSpread.MakeGenericMethod(new[] { genericType });
                                var BytesToSpreadDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToSpread);
                                FromMemoryMap.TryAdd(typeof(T), BytesToSpreadDel);

                                return (T)BytesToSpreadDel(input);
                            }
                        }
                    }
                }
                // ImmutableArray
                else if (typeof(T).Name.StartsWith("ImmutableArray"))
                {
                    if (typeof(T).IsGenericType)
                    {
                        var genericType = typeof(T).GetGenericArguments()[0];
                        if (genericType != null && genericType.IsBlitable())
                        {
                            var bytesToImmutableArray = typeof(RawSerialization).GetMethod(nameof(BytesToImmutableArray), BindingFlags.NonPublic | BindingFlags.Static);
                            if (bytesToImmutableArray != null)
                            {
                                bytesToImmutableArray = bytesToImmutableArray.MakeGenericMethod(new[] { genericType });
                                var BytesToImmutableArrayDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToImmutableArray);
                                FromMemoryMap.TryAdd(typeof(T), BytesToImmutableArrayDel);

                                return (T)BytesToImmutableArrayDel(input);
                            }
                        }
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
                            var BytesToArrayDel = StaticMethodDelegate<ReadOnlyMemory<byte>, object>(bytesToArray);
                            FromMemoryMap.TryAdd(typeof(T), BytesToArrayDel);

                            return (T)BytesToArrayDel(input);
                        }
                    }
                }
                // String
                else if (typeof(T) == typeof(String))
                {
                    Func<ReadOnlyMemory<byte>, object> BytesToStringDel = (b) => BytesToString(b);
                    FromMemoryMap.TryAdd(typeof(T), BytesToStringDel);

                    return (T)BytesToStringDel(input);
                }
            }

            throw new SerializationException($"Can't deserialize {typeof(T)}");
        }

        private static readonly ConcurrentDictionary<Type, Func<object, ReadOnlyMemory<byte>>> ToMemoryMap = new();
        private static readonly ConcurrentDictionary<Type, Func<ReadOnlyMemory<byte>, object>> FromMemoryMap = new();

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
    }
}
