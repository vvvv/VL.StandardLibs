using System;
using System.Runtime.CompilerServices;
using Stride.Engine;
using Stride.Graphics;
using Stride.Core.IO;
using System.Buffers;
using System.IO;
using Buffer = Stride.Graphics.Buffer;
using VL.Core;

namespace VL.Stride.Graphics
{
    public struct BufferViewDescription
    {
        /// <summary>
        /// The flags used for the view. If <see cref="BufferFlags.None"/> then the view is using the flags from the buffer.
        /// </summary>
        public BufferFlags Flags;

        /// <summary>
        /// The format of the view, used for typed buffers, usually a 32-bit float format for e.g. Buffer&lt;float4&gt;. Set to <see cref="PixelFormat.None"/> when the buffer is raw or structured.
        /// </summary>
        public PixelFormat Format;

        //used in patch
        public static void Split(ref BufferViewDescription bufferViewDescription, out BufferFlags flags, out PixelFormat format)
        {
            flags = bufferViewDescription.Flags;
            format = bufferViewDescription.Format;
        }
    }
}
