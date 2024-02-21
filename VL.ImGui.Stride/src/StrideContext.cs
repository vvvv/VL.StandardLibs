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
    internal sealed class StrideContext : Context
    {
        private readonly List<RenderLayer> Layers = new List<RenderLayer> { new RenderLayer() };

        public override void NewFrame()
        {
            Layers.Clear();
            base.NewFrame();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>id ... first layer is 1 not 0</returns>
        internal int AddLayer(RenderLayer layer)
        {
            Vector2 size = (Vector2)layer.Viewport?.Size;

            Layers.Add(layer);
            DrawListPtr.AddImage(new IntPtr(Layers.Count), default, Unsafe.As<Vector2, System.Numerics.Vector2>(ref size));
            return Layers.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">id ... first layer is 1 not 0</param>
        /// <returns>RenderLayer</returns>
        internal RenderLayer? GetLayer(int id)
        {
            return Layers.GetItemOrNull(id - 1);
        }

    }

    internal sealed class RenderLayer
    {
        public IGraphicsRendererBase? Layer { get; set; }
        public RenderView? RenderView { get; set; }
        public Viewport? Viewport { get; set; }
    }



}
