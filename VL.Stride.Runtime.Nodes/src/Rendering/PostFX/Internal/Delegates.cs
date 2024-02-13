using Stride.Graphics;
using Stride.Rendering;
using VL.Stride.Rendering.PostFX.Advanced;

namespace VL.Stride.Rendering.PostFX.Internal;

public delegate void CreateHandler(out object stateOutput);
public delegate void CollectHandler(object stateInput, RenderContext renderContext, out object stateOutput);
public delegate void DrawHandler(object stateInput, PostFXDrawContext drawContext, out object stateOutput, out Texture? output);