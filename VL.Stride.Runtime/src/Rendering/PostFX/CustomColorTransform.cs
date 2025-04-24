#nullable enable
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;
using Stride.Rendering.Materials;
using Stride.Shaders;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride.Rendering.PostFX.Internal;
using VL.Stride.Shaders.ShaderFX;
using VL.Stride.Shaders.ShaderFX.Control;
using static Stride.Rendering.ParameterCollection;

namespace VL.Stride.Rendering.PostFX;

/// <summary>
/// Allows to patch a custom <see cref="ColorTransform"/> with ShaderFX.
/// </summary>
[ProcessNode(Name = "CustomColorTransform")]
public sealed class CustomColorTransformNode
{
    private static readonly SetVar<Color4> _defaultOutput = ShaderFXUtils.Constant(Color4.Black);
    private static readonly SetVar<Color4> _sourceColor = ShaderFXUtils.Semantic<Color4>("SOURCECOLOR", "SourceColor");

    private readonly IResourceHandle<GraphicsDevice> _deviceHandle;

    private CreateHandler? _createHandler;
    private ColorTransformUpdateHandler? _updateHandler;
    private object? _state;
    private SetVar<Color4>? _lastOutput;
    private CustomColorTransform? _colorTransform;

    public CustomColorTransformNode()
    {
        _deviceHandle = AppHost.Current.Services.GetDeviceHandle();
    }

    [return: Pin(Name = "Output")]
    public ColorTransform? Update(CreateHandler create, ColorTransformUpdateHandler update)
    {
        _createHandler = create;
        _updateHandler = update;

        if (_createHandler is null || _updateHandler is null)
            return null;

        if (_state is null)
            _createHandler(out _state);

        _updateHandler(_state, _sourceColor, out _state, out var output);

        if (output is null)
            output = _defaultOutput;

        if (output != _lastOutput)
        {
            _lastOutput = output;

            _colorTransform = new CustomColorTransform();
            _colorTransform.InputGraph = GenerateShader(output, _colorTransform.Parameters);
        }

        return _colorTransform;
    }


    private ShaderSource GenerateShader(SetVar<Color4> output, ParameterCollection parameterCollection)
    {
        var value = output.GetVarValue();
        var graph = ShaderGraph.BuildFinalShaderGraph(value);
        var @do = new Do<Color4>(graph, value);
        var context = ShaderGraph.NewShaderGeneratorContext(_deviceHandle.Resource, parameterCollection, new CompositeDisposable());
        var key = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
        return @do.GenerateShaderSource(context, key);
    }

    private sealed class CustomColorTransform : ColorTransform
    {
        private Copier copier;

        public CustomColorTransform()
            : base("CustomColorTransform")
        {
        }

        public ShaderSource InputGraph
        {
            get => Parameters.Get(CustomColorTransformKeys.InputGraph);
            set => Parameters.Set(CustomColorTransformKeys.InputGraph, value);
        }

        public override void PrepareParameters(ColorTransformContext context, ParameterCollection parentCollection, string keyRoot)
        {
            base.PrepareParameters(context, parentCollection, keyRoot);

            copier = new Copier(parentCollection, Parameters);
        }

        public override void UpdateParameters(ColorTransformContext context)
        {
            copier.Copy();
        }
    }
}
