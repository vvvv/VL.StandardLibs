using Stride.Core.Serialization;
using System;
using Stride.Graphics;
using Buffer = Stride.Graphics.Buffer;
using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;

namespace VL.Stride.Graphics
{
    [DataSerializer(typeof(IndirectIndexBufferBinding.Serializer))]
    public class IndirectIndexBufferBinding : IndexBufferBinding
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DrawArgsStruct
        {
            UInt32 IndexCountPerInstance;
            UInt32 InstanceCount;
            UInt32 StartIndexLocation;
            Int32 BaseVertexLocation;
            UInt32 StartInstanceLocation;

            public DrawArgsStruct(UInt32 IndexCountPerInstance, UInt32 InstanceCount, UInt32 StartIndexLocation, Int32 BaseVertexLocation, UInt32 StartInstanceLocation)
            {
                this.IndexCountPerInstance = IndexCountPerInstance;
                this.InstanceCount = InstanceCount;
                this.StartIndexLocation = StartIndexLocation;
                this.BaseVertexLocation = BaseVertexLocation;
                this.StartInstanceLocation = StartInstanceLocation;
            }

            public ReadOnlyMemory<DrawArgsStruct> AsReadOnlyMemory()
            {
                return new ReadOnlyMemory<DrawArgsStruct>(new DrawArgsStruct[] {this} );
            }
        }

        public Buffer DrawArgs { get; private set; }

        public IndirectIndexBufferBinding(Buffer indexBuffer, bool is32Bit, int count, int indexOffset = 0) : base(indexBuffer, is32Bit, count, indexOffset)
        {

        }

        public IndirectIndexBufferBinding(Buffer indexBuffer, Buffer drawArgs, bool is32Bit, int count, int indexOffset = 0) : base(indexBuffer, is32Bit, count, indexOffset)
        {
            if (drawArgs == null) throw new ArgumentNullException("drawArgs");
            DrawArgs = drawArgs;
        }

        internal class Serializer : DataSerializer<IndirectIndexBufferBinding>
        {
            public override void Serialize(ref IndirectIndexBufferBinding indexBufferBinding, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var buffer = stream.Read<Buffer>();
                    var is32Bit = stream.ReadBoolean();
                    var count = stream.ReadInt32();
                    var offset = stream.ReadInt32();
                    var drawArgs = stream.Read<Buffer>();

                    indexBufferBinding = new IndirectIndexBufferBinding(buffer, drawArgs, is32Bit, count, offset);
                }
                else
                {
                    stream.Write(indexBufferBinding.Buffer);
                    stream.Write(indexBufferBinding.Is32Bit);
                    stream.Write(indexBufferBinding.Count);
                    stream.Write(indexBufferBinding.Offset);
                    stream.Write(indexBufferBinding.DrawArgs);
                }
            }
        }
    }
}
