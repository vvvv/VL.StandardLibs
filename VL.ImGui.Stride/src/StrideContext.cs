using ImGuiNET;
using MathNet.Numerics.Distributions;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;
using VL.Lib.ColorMath;
using VL.Lib.Mathematics;
using VL.Skia;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;
    internal sealed class StrideContext : Context
    {

    }

    internal sealed class RenderLayer
    {
        public IGraphicsRendererBase? Layer { get; set; }
        public RenderView? RenderView { get; set; }
        public Viewport? Viewport { get; set; }
    }



}
