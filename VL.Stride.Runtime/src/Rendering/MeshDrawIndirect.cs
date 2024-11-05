// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    // Need to add support for fields in auto data converter
    [DataContract]
    public class MeshDrawIndirect : MeshDraw
    {
        public MeshDrawIndirect() :base() {}

        public new PrimitiveType PrimitiveType
        {
            get => base.PrimitiveType;
            set => base.PrimitiveType = value;
        }

        public new int DrawCount
        {
            get => base.DrawCount;
            set => base.DrawCount = value;
        }

        public new int StartLocation
        {
            get => base.StartLocation;
            set => base.StartLocation = value;
        }

        public new VertexBufferBinding[] VertexBuffers
        {
            get => base.VertexBuffers;
            set => base.VertexBuffers = value;
        }

        public new IndexBufferBinding IndexBuffer
        {
            get => base.IndexBuffer;
            set => base.IndexBuffer = value;
        }

        public Buffer DrawArgs;

        bool drawAuto = false;
        public bool DrawAuto
        {
            get => drawAuto;
            set => drawAuto = value;
        }
    }
}
