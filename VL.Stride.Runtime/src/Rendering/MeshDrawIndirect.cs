// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Rendering
{
    // Need to add support for fields in auto data converter
    [DataContract]
    public class MeshDrawIndirect : MeshDraw
    {
        public MeshDrawIndirect() :base() {}

        public Buffer DrawArgs;

        public bool DrawAuto = false;
    }
}
