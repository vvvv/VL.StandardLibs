using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Lib.Collections;
using VL.Lib.Text;


namespace VL.Lib.IO
{
    public static class StreamUtils
    {
        public static readonly IResourceProvider<Stream> DefaultProvider = ResourceProvider.Return(Stream.Null);

        public const int SmallBufferSize = 0x1000;
        public const int LargeBufferSize = 0x10000; // Let's keep below the 85K so it will not get allocated on LOH
        public const int SmallCharBufferSize = 0x1000 / sizeof(char);
        public const int LargeCharBufferSize = 0x10000 / sizeof(char);

        [ThreadStatic]
        static byte[] _smallBuffer, _largeBuffer;

        public static byte[] SmallBuffer => _smallBuffer ?? (_smallBuffer = new byte[SmallBufferSize]);
        public static byte[] LargeBuffer => _largeBuffer ?? (_largeBuffer = new byte[LargeBufferSize]);

        public static IResourceProvider<Stream> ToStream(IEnumerable<byte> input, bool writeable)
        {
            var array = input as byte[] ?? input.ToArray();
            var provider = ResourceProvider.New(() => new MemoryStream(array, writeable));
            if (writeable)
                provider = provider.ShareSerially();
            return provider;
        }

        /// <summary>
        /// Gets the byte length of the stream
        /// </summary>
        /// <param name="streamProvider"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> Length(IResourceProvider<Stream> streamProvider, out long length)
        {
            using (var handle = streamProvider.GetHandle())
            {
                var stream = handle.Resource;
                length = stream.Length;
            }
            return streamProvider;
        }

        /// <summary>
        /// Gets the current position in the stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> GetPosition(IResourceProvider<Stream> input, out long position)
        {
            using (var handle = input.GetHandle())
            {
                var stream = handle.Resource;
                position = stream.CanSeek ? stream.Position : 0L;
            }
            return input;
        }

        //public static IResourceProvider<Stream> SetPosition(IResourceProvider<Stream> streamProvider, long position)
        //{
        //    using (var handle = streamProvider.GetHandle())
        //    {
        //        var stream = handle.Resource;
        //        if (stream.CanSeek)
        //            stream.Position = position;
        //    }
        //    return streamProvider;
        //}

        private static IResourceProvider<Stream> Read<T>(IResourceProvider<Stream> input, long offset, long count, Func<Stream, long,T> readFunc, T empty, out T data)
        {
            using (var handle = input.GetHandle())
            {
                var stream = handle.Resource;
                
                if (stream.CanSeek)
                {
                    stream.Position += offset;
                    if (count == long.MaxValue)
                        count = stream.Length;  
                }

                data = readFunc(stream, count);
            }
            return input;
        }
   
        private static Spread<byte> ReadBytesFunc(Stream stream, long count)
        {
            if (count == 0)
                return Spread<byte>.Empty;

            long bytesToRead = count;
            int bytesRead = 0;
            var buffer = StreamUtils.SmallBuffer;
            var builder = new SpreadBuilder<byte>((int)Math.Min(count, buffer.Length));
            do
            {
                bytesRead = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, bytesToRead));
                builder.AddRange(buffer, bytesRead);
                bytesToRead -= bytesRead;
            } while (bytesRead > 0 && bytesToRead > 0);
            return builder.ToSpread();
        }

        /// <summary>
        /// Reads bytes from a stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> ReadBytes(IResourceProvider<Stream> input, long offset, long count, out Spread<byte> data)
        {
            return Read(input, offset, count, ReadBytesFunc, Spread<byte>.Empty, out data);
        }

        /// <summary>
        /// Reads all bytes from a stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> ReadAllBytes(IResourceProvider<Stream> input, out Spread<byte> data)
        {
            return Read(input, 0, long.MaxValue, ReadBytesFunc, Spread<byte>.Empty, out data);
        }

        // Not really useful as long as issue #2709 is not closed
        //Reads all bytes from a stream in chunks
        public static IEnumerable<IReadOnlyList<byte>> ReadAllBytesInChunks(IResourceProvider<Stream> input, int chunkSize = SmallBufferSize)
        {
            using (var handle = input.GetHandle())
            {
                var stream = handle.Resource;
                var buffer = chunkSize == SmallBufferSize ? SmallBuffer : new byte[chunkSize];
                while (true)
                {
                    var readCount = stream.Read(buffer, 0, buffer.Length);
                    if (readCount == buffer.Length)
                        yield return buffer;
                    else if (readCount > 0)
                        yield return new ArraySegment<byte>(buffer, 0, readCount);
                    else
                        break;
                }
            }
        }

        /// <summary>
        /// Reads strings from a stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> ReadString(IResourceProvider<Stream> input, Encodings encoding, long offset, long count, out string data)
        {
            Func<Stream, long, string> readString = ((stream, c) =>
            {
                return encoding.ToEncoding().GetString(ReadBytesFunc(stream,count).ToArray());
            });

            return Read(input, offset, count, readString, string.Empty, out data);
        }

        /// <summary>
        /// Reads the string from an entire stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> ReadAllString(IResourceProvider<Stream> input, Encodings encoding, out string data)
        {
            Func<Stream, long, string> readAllString = ((stream, c) =>
            {
                using (var reader = new StreamReader(stream, encoding.ToEncoding(), true, StreamUtils.SmallBufferSize, leaveOpen: true))
                {
                    return reader.ReadToEnd();
                }
            });

            return Read(input, 0, long.MaxValue, readAllString, string.Empty, out data);
        }


        private static IResourceProvider<Stream> Write<T>(IResourceProvider<Stream> input, Action<Stream, T> WriteAction, T data, long offset)
        {
            input.Using((stream) =>
            {
                if (stream.CanSeek)
                    stream.Position += offset;
                WriteAction(stream, data);
            });
            return input;
        }

        /// <summary>
        /// Writes bytes to a stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> WriteBytes(IResourceProvider<Stream> input, IEnumerable<byte> data, long offset)
        {
            return Write(input, (stream, d) => { foreach (var b in d) { stream.WriteByte(b); } }, data, offset);
        }

        /// <summary>
        /// Writes a string to a stream
        /// </summary>
        /// <param name="input"></param>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static IResourceProvider<Stream> WriteString(IResourceProvider<Stream> input, string data, Encodings encoding, long offset)
        {
            var writeString = new Action<Stream, string>((stream, d) =>
            {
                using (var writer = new StreamWriter(stream, encoding.ToEncoding(), StreamUtils.SmallBufferSize, leaveOpen: true))
                    writer.Write(d);
            });

            return Write(input, writeString, data, offset);
        }
    }
}
