#nullable enable
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using VL.Stride.Rendering.PostFX.Advanced;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride.Rendering.PostFX.Internal;

public delegate void CreateHandler(out object stateOutput);
public delegate void CollectHandler(object stateInput, RenderContext renderContext, out object stateOutput);
public delegate void DrawHandler(object stateInput, PostFXDrawContext drawContext, out object stateOutput, out Texture? output);

public delegate void ColorTransformUpdateHandler(object stateInput, SetVar<Color4> input, out object stateOutput, out SetVar<Color4> output);