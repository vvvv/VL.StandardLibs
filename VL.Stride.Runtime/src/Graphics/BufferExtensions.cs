using System;
using System.Runtime.CompilerServices;
using Stride.Engine;
using Stride.Graphics;
using Stride.Core.IO;
using System.Buffers;
using System.IO;
using Buffer = Stride.Graphics.Buffer;
using VL.Core;
using System.Reflection;
using Stride.Core;
using System.Diagnostics;
using MapMode = Stride.Graphics.MapMode;

namespace VL.Stride.Graphics
{
    public enum StructuredBufferType
    {
        None = BufferFlags.None,
        StructuredBuffer = BufferFlags.StructuredBuffer,
        StructuredAppendBuffer = BufferFlags.StructuredAppendBuffer,
        StructuredCounterBuffer = BufferFlags.StructuredCounterBuffer
    }

    public static class BufferExtensions
    {
        /// <summary>
        /// Following the spec from: https://docs.microsoft.com/en-us/windows/win32/api/d3d11/ne-d3d11-d3d11_buffer_uav_flag
        /// </summary>
        /// <param name="bufferFlags"></param>
        /// <param name="viewFormat"></param>
        /// <param name="structuredBufferType"></param>
        /// <param name="flags"></param>
        /// <param name="format"></param>
        public static void CombineWithStructuredBufferTypeFlag(BufferFlags bufferFlags, PixelFormat viewFormat, StructuredBufferType structuredBufferType, out BufferFlags flags, out PixelFormat format)
        {
            switch (structuredBufferType)
            {
                case StructuredBufferType.None:
                    flags = bufferFlags;
                    format = viewFormat;
                    break;
                case StructuredBufferType.StructuredBuffer:
                    flags = bufferFlags | BufferFlags.StructuredBuffer;
                    format = viewFormat;
                    break;
                case StructuredBufferType.StructuredAppendBuffer:
                    flags = bufferFlags | BufferFlags.StructuredAppendBuffer;
                    format = PixelFormat.None;
                    break;
                case StructuredBufferType.StructuredCounterBuffer:
                    flags = bufferFlags | BufferFlags.StructuredCounterBuffer;
                    format = PixelFormat.None;
                    break;
                default:
                    flags = bufferFlags;
                    format = viewFormat;
                    break;
            }
        }

        public static Buffer New(GraphicsDevice graphicsDevice, BufferDescription description, BufferViewDescription viewDescription, IntPtr intialData)
        {
            var buffer = BufferCtor(graphicsDevice);
            return BufferInit(buffer, description, viewDescription, intialData);
        }

        const BindingFlags NonPunblicInst = BindingFlags.NonPublic | BindingFlags.Instance;

        static Buffer BufferCtor(GraphicsDevice graphicsDevice)
        {
            var ctor = typeof(Buffer).GetConstructor(NonPunblicInst, null, new[] { typeof(GraphicsDevice) }, null);
            return (Buffer)ctor.Invoke(new[] { graphicsDevice });
        }

        static Buffer BufferInit(Buffer buffer, BufferDescription description, BufferViewDescription viewDescription, IntPtr intialData)
        {
            var init = typeof(Buffer).GetMethod("InitializeFromImpl", NonPunblicInst, null, new[] { typeof(BufferDescription), typeof(BufferFlags), typeof(PixelFormat), typeof(IntPtr) }, null);
            return (Buffer)init.Invoke(buffer, new object[] { description, viewDescription.Flags, viewDescription.Format, intialData});
        }

        internal static readonly PropertyKey<Buffer> ParentBuffer = new PropertyKey<Buffer>(nameof(ParentBuffer), typeof(Buffer));

        public static Buffer ToBufferView(this Buffer parentBuffer, Buffer bufferView, BufferViewDescription viewDescription, GraphicsDevice graphicsDevice)
        {
            SetGraphicsDevice(bufferView, graphicsDevice);

            //bufferDescription = description;
            SetField(bufferView, "bufferDescription", parentBuffer.Description);

            //nativeDescription = ConvertToNativeDescription(Description);
            SetField(bufferView, "nativeDescription", ConvertToNativeDescription(parentBuffer.Description));

            //ViewFlags = viewFlags;
            SetProp(bufferView, "ViewFlags", viewDescription.Flags);

            //InitCountAndViewFormat(out this.elementCount, ref viewFormat);
            InitCountAndViewFormat(bufferView, out var count, ref viewDescription.Format);
            SetField(bufferView, "elementCount", count);

            //ViewFormat = viewFormat;
            SetProp(bufferView, "ViewFormat", viewDescription.Format);

            //NativeDeviceChild = new SharpDX.Direct3D11.Buffer(GraphicsDevice.NativeDevice, dataPointer, nativeDescription);
            SetNativeChild(bufferView, GetNativeChild(parentBuffer));

            //if (nativeDescription.Usage != ResourceUsage.Staging)
            //    this.InitializeViews();
            InitializeViews(bufferView);

            if (parentBuffer is IReferencable referencable)
            {
                referencable.AddReference();
                bufferView.Destroyed += (e, s) => referencable.Release();
            }

            return bufferView;
        }

        static SharpDX.Direct3D11.DeviceChild GetNativeChild(GraphicsResourceBase graphicsResource)
        {
            var prop = typeof(GraphicsResourceBase).GetProperty("NativeDeviceChild", NonPunblicInst);
            return (SharpDX.Direct3D11.DeviceChild)prop.GetValue(graphicsResource);
        }

        static void SetNativeChild(GraphicsResourceBase graphicsResource, SharpDX.Direct3D11.DeviceChild deviceChild)
        {
            var iUnknownObject = deviceChild as SharpDX.IUnknown;
            if (iUnknownObject != null)
            {
                var refCountResult = iUnknownObject.AddReference();
                Debug.Assert(refCountResult > 1);
            }
            var prop = typeof(GraphicsResourceBase).GetProperty("NativeDeviceChild", NonPunblicInst);
            prop.SetValue(graphicsResource, deviceChild);
        }

        static SharpDX.Direct3D11.BufferDescription ConvertToNativeDescription(BufferDescription description)
        {
            var method = typeof(Buffer).GetMethod("ConvertToNativeDescription", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(BufferDescription) }, null);
            return (SharpDX.Direct3D11.BufferDescription)method.Invoke(null, new object[] { description });
        }

        static void SetField(Buffer buffer, string name, object arg)
        {
            var field = typeof(Buffer).GetField(name, NonPunblicInst);
            field.SetValue(buffer, arg);
        }

        static void SetGraphicsDevice(Buffer buffer, object arg)
        {
            var prop = typeof(GraphicsResourceBase).GetProperty("GraphicsDevice", BindingFlags.Public | BindingFlags.Instance);
            prop.SetValue(buffer, arg);
        }

        static void SetProp(Buffer buffer, string name, object arg)
        {
            var prop = typeof(Buffer).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            prop.SetValue(buffer, arg);
        }
        
        static void InitCountAndViewFormat(Buffer buffer, out int count, ref PixelFormat viewFormat)
        {
            var method = typeof(Buffer).GetMethod("InitCountAndViewFormat", NonPunblicInst);
            var args = new object[] { 0, viewFormat };
            method.Invoke(buffer, args);
            count = (int)args[0];
        }

        static void InitializeViews(Buffer buffer)
        {
            var method = typeof(Buffer).GetMethod("InitializeViews", NonPunblicInst);
            method.Invoke(buffer, null);
        }

        /// <summary>
        /// Copies the <paramref name="fromData"/> to the given <paramref name="buffer"/> on GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="buffer">The <see cref="Buffer"/>.</param>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        /// <returns>The GPU buffer.</returns>
        public static unsafe Buffer SetData<TData>(this Buffer buffer, CommandList commandList, IHasMemory<TData> fromData, int offsetInBytes = 0) where TData : struct
        {
            if (fromData.TryGetMemory(out ReadOnlyMemory<TData> memory))
                return buffer.SetData(commandList, memory, offsetInBytes);
            return buffer;
        }

        /// <summary>
        /// Copies the <paramref name="memory"/> to the given <paramref name="buffer"/> on GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="buffer">The <see cref="Buffer"/>.</param>
        /// <param name="commandList">The <see cref="CommandList"/>.</param>
        /// <param name="memory">The memory to copy from.</param>
        /// <param name="offsetInBytes">The offset in bytes to write to.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>
        /// See the unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        /// <returns>The GPU buffer.</returns>
        public static unsafe Buffer SetData<TData>(this Buffer buffer, CommandList commandList, ReadOnlyMemory<TData> memory, int offsetInBytes = 0) where TData : struct
        {
            using (var handle = memory.Pin())
            {
                var elementSize = Unsafe.SizeOf<TData>();
                var dataPointer = new DataPointer(handle.Pointer, memory.Length * elementSize);
                buffer.SetData(commandList, dataPointer, offsetInBytes);
                return buffer;
            }
        }

        public static Buffer SetDataFromProvider(this Buffer buffer, CommandList commandList, IGraphicsDataProvider data, int offsetInBytes = 0)
        {
            if (buffer != null && data != null)
            {
                using (var handle = data.Pin())
                {
                    buffer.SetData(commandList, new DataPointer(handle.Pointer, data.SizeInBytes), offsetInBytes);
                } 
            }

            return buffer;
        }

        /// <summary>
        /// Creates a new <see cref="Buffer"/> initialized with a copy of the given data.
        /// </summary>
        /// <typeparam name="TData">The element type.</typeparam>
        /// <param name="device">The graphics device.</param>
        /// <param name="fromData">The data to use to initialize the buffer.</param>
        /// <param name="bufferFlags">The buffer flags.</param>
        /// <param name="usage">The buffer usage.</param>
        /// <exception cref="ArgumentException">If retrieval of read-only memory failed.</exception>
        /// <returns>The newly created buffer.</returns>
        public static unsafe Buffer New<TData>(GraphicsDevice device, IHasMemory<TData> fromData, BufferFlags bufferFlags, GraphicsResourceUsage usage) where TData : struct
        {
            if (fromData.TryGetMemory(out ReadOnlyMemory<TData> memory))
                return New(device, memory, bufferFlags, usage);
            throw new ArgumentException($"Failed to create buffer because retrieval of read-only memory failed.", nameof(fromData));
        }

        /// <summary>
        /// Creates a new <see cref="Buffer"/> initialized with a copy of the given data.
        /// </summary>
        /// <typeparam name="TData">The element type.</typeparam>
        /// <param name="device">The graphics device.</param>
        /// <param name="memory">The data to use to initialize the buffer.</param>
        /// <param name="bufferFlags">The buffer flags.</param>
        /// <param name="usage">The buffer usage.</param>
        /// <exception cref="ArgumentException">If retrieval of read-only memory failed.</exception>
        /// <returns>The newly created buffer.</returns>
        public static unsafe Buffer New<TData>(GraphicsDevice device, ReadOnlyMemory<TData> memory, BufferFlags bufferFlags, GraphicsResourceUsage usage) where TData : struct
        {
            using (var handle = memory.Pin())
            {
                var elementSize = Unsafe.SizeOf<TData>();
                var dataPointer = new DataPointer(handle.Pointer, memory.Length * elementSize);
                return Buffer.New(device, dataPointer, elementSize, bufferFlags, usage);
            }
        }


        //    public static unsafe void WriteToDisk(this Buffer buffer, string filepath)
        //    {

        //    }

        //    public static unsafe void WriteToDisk(this Buffer buffer, Stream stream)
        //    {
        //        buffer.GetData()

        //        var pool = ArrayPool<byte>.Shared;
        //        var chunk = pool.Rent(Math.Min(buffer.SizeInBytes, 0x10000));
        //        try
        //        {
        //            fixed (byte* chunkPtr = chunk)
        //            {
        //                var offset = 0;
        //                while (stream.CanRead)
        //                {
        //                    var bytesRead = stream.Read(chunk, 0, chunk.Length);
        //                    if (bytesRead > 0)
        //                    {
        //                        var dp = new DataPointer(chunkPtr, bytesRead);
        //                        buffer.SetData(commandList, dp, offset);
        //                        offset += bytesRead;
        //                    }
        //                }
        //            }
        //        }
        //        finally
        //        {
        //            pool.Return(chunk);
        //        }
        //    }
        //}

        public static unsafe Buffer SetDataFromFile(this Buffer buffer, CommandList commandList, string filepath)
        {
            using (var stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                buffer.SetDataFromStream(commandList, stream);
            }
            return buffer;
        }

        public static unsafe Buffer SetDataFromXenkoAssetURL(this Buffer buffer, CommandList commandList, Game game, string url)
        {
            using (var stream = game.Content.OpenAsStream(url, StreamFlags.None))
            {
                buffer.SetDataFromStream(commandList, stream);
            }
            return buffer;
        }

        public static unsafe Buffer SetDataFromStream(this Buffer buffer, CommandList commandList, Stream stream)
        {
            var pool = ArrayPool<byte>.Shared;
            var chunk = pool.Rent(Math.Min(buffer.SizeInBytes, 0x10000));
            try
            {
                fixed (byte* chunkPtr = chunk)
                {
                    var offset = 0;
                    while (stream.CanRead)
                    {
                        var bytesRead = stream.Read(chunk, 0, chunk.Length);
                        if (bytesRead > 0)
                        {
                            var dp = new DataPointer(chunkPtr, bytesRead);
                            buffer.SetData(commandList, dp, offset);
                            offset += bytesRead;
                        }
                    }
                }
            }
            finally
            {
                pool.Return(chunk);
            }
            return buffer;
        }

        /// <summary>
        /// Calculates the expected element count of a buffer using a specified type.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <returns>The expected width</returns>
        /// <exception cref="System.ArgumentException">If the size is invalid</exception>
        public static int CalculateElementCount<TData>(this Buffer input) where TData : struct
        {
            var dataStrideInBytes = Utilities.SizeOf<TData>();

            return input.SizeInBytes / dataStrideInBytes;
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="thisBuffer"></param>
        /// <param name="commandList">The command list.</param>
        /// <param name="toData">The destination array to receive a copy of the buffer datas.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <param name="offsetInBytes"></param>
        /// <param name="lengthInBytes"></param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this buffer is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public static bool GetData<TData>(this Buffer thisBuffer, CommandList commandList, TData[] toData, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0) where TData : struct
        {
            // Get data from this resource
            if (thisBuffer.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                return thisBuffer.GetData(commandList, thisBuffer, toData, doNotWait, offsetInBytes, lengthInBytes);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using (var throughStaging = thisBuffer.ToStaging())
                    return thisBuffer.GetData(commandList, throughStaging, toData, doNotWait, offsetInBytes, lengthInBytes);
            }
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="thisBuffer"></param>
        /// <param name="commandList"></param>
        /// <param name="staginBuffer">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <param name="doNotWait"></param>
        /// <param name="offsetInBytes"></param>
        /// <param name="lengthInBytes"></param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public static bool GetData<TData>(this Buffer thisBuffer, CommandList commandList, Buffer staginBuffer, TData[] toData, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0) where TData : struct
        {
            using (var pinner = new GCPinner(toData))
                return thisBuffer.GetData(commandList, staginBuffer, new DataPointer(pinner.Pointer, toData.Length * Utilities.SizeOf<TData>()), doNotWait, offsetInBytes, lengthInBytes);
        }

        /// <summary>
        /// Copies the content of this buffer to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="thisBuffer"></param>
        /// <param name="commandList">The command list.</param>
        /// <param name="toData">The destination array to receive a copy of the buffer datas.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <param name="offsetInBytes"></param>
        /// <param name="lengthInBytes"></param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this buffer is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public static bool GetData(this Buffer thisBuffer, CommandList commandList, DataPointer toData, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            // Get data from this resource
            if (thisBuffer.Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                return thisBuffer.GetData(commandList, thisBuffer, toData, doNotWait, offsetInBytes, lengthInBytes);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using (var throughStaging = thisBuffer.ToStaging())
                    return thisBuffer.GetData(commandList, throughStaging, toData, doNotWait, offsetInBytes, lengthInBytes);
            }
        }

        /// <summary>
        /// Copies the content of this buffer from GPU memory to a CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="thisBuffer"></param>
        /// <param name="commandList"></param>
        /// <param name="stagingBuffer">The staging buffer used to transfer the buffer.</param>
        /// <param name="toData">To data pointer.</param>
        /// <param name="doNotWait"></param>
        /// <param name="offsetInBytes"></param>
        /// <param name="lengthInBytes"></param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public static bool GetData(this Buffer thisBuffer, CommandList commandList, Buffer stagingBuffer, DataPointer toData, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            // Check size validity of data to copy to
            if (toData.Pointer == IntPtr.Zero || toData.Size != thisBuffer.SizeInBytes)
                return false;

            // Copy the texture to a staging resource
            if (!ReferenceEquals(thisBuffer, stagingBuffer))
                commandList.Copy(thisBuffer, stagingBuffer);

            var mappedResource = commandList.MapSubresource(stagingBuffer, 0, MapMode.Read, doNotWait, offsetInBytes, lengthInBytes);
            
            try
            {
                if (mappedResource.DataBox.DataPointer != IntPtr.Zero)
                {
                    Utilities.CopyMemory(toData.Pointer, mappedResource.DataBox.DataPointer, toData.Size);
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                // Make sure that we unmap the resource in case of an exception
                commandList.UnmapSubresource(mappedResource);
            }

            return true;
        }
    }
}
