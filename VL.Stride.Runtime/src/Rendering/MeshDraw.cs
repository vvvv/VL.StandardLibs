// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Graphics;

namespace VL.Stride.Rendering
{
    // Need to add support for fields in auto data converter
    [DataContract]
    public class MeshDraw : global::Stride.Rendering.MeshDraw
    {
        public Buffer DrawArgs;
    }
}
