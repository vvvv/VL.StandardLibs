#nullable enable
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using VL.Core;
using VL.Lib.Basics.Resources;
using VL.Stride.Engine;
using VL.Stride.Games;
using VL.Stride.Rendering.PostFX.Advanced;
using VL.Stride.Rendering.PostFX.Internal;

namespace VL.Stride.Rendering.PostFX;

/// <summary>
/// Allows to patch a custom post-processing effect.
/// </summary>
[ProcessNode]
public sealed class CustomPostFX : ImageEffect, IPostProcessingEffects
{
    private readonly ILogger _logger;
    private readonly IResourceHandle<Game> _gameHandle;
    private readonly SchedulerSystem _schedulerSystem;

    private RenderDrawContext? _renderDrawContext;
    private object? _state;
    private CreateHandler? _createHandler;
    private DrawHandler? _drawHandler;
    private bool _requiresVelocityBuffer, _requiresNormalBuffer, _requiresSpecularRoughnessBuffer;
    private bool _userTakesCareOfWritingToRenderTarget;

    public CustomPostFX([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
        : base()
    {
        _logger = nodeContext.GetLogger();
        _gameHandle = nodeContext.AppHost.Services.GetGameHandle();
        _schedulerSystem = ((VLGame)_gameHandle.Resource).SchedulerSystem;
    }

    /// <summary>
    /// Configures the custom post-processing effect.
    /// </summary>
    /// <param name="create"></param>
    /// <param name="draw"></param>
    /// <param name="requiresNormalBuffer"></param>
    /// <param name="requiresSpecularRoughnessBuffer"></param>
    /// <param name="requiresVelocityBuffer"></param>
    /// <param name="userTakesCareOfWritingToRenderTarget">Tells the node that the render target gets written and therefor no extra work has to be done.</param>
    [return: Pin(Name = "Output")]
    public IPostProcessingEffects Update(CreateHandler create, DrawHandler draw, bool requiresNormalBuffer, bool requiresSpecularRoughnessBuffer, bool requiresVelocityBuffer, bool userTakesCareOfWritingToRenderTarget)
    {
        _createHandler = create;
        _drawHandler = draw;
        _requiresNormalBuffer = requiresNormalBuffer;
        _requiresSpecularRoughnessBuffer = requiresSpecularRoughnessBuffer;
        _requiresVelocityBuffer = requiresVelocityBuffer;
        _userTakesCareOfWritingToRenderTarget = userTakesCareOfWritingToRenderTarget;
        return this;
    }

    protected override void DrawCore(RenderDrawContext context)
    {
        var input = GetInput(0);
        var output = GetOutput(0);
        if (input == null || output == null)
        {
            return;
        }

        // If input == output, than copy the input to a temporary texture
        if (input == output)
        {
            var newInput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
            context.CommandList.Copy(input, newInput);
            input = newInput;
        }

        Texture? userOutput = null;

        using (_schedulerSystem.WithPrivateScheduler(Schedule))
        {
            _renderDrawContext = context;

            try
            {
                var postFxContext = new PostFXDrawContext()
                {
                    DrawContext = context,
                    RenderTarget = output,
                    ColorBuffer = input,
                    DepthBuffer = GetInput(1),
                    NormalBuffer = GetInputOrDefault(2),
                    SpecularRoughnessBuffer = GetInputOrDefault(3),
                    VelocityBuffer = GetInputOrDefault(6),
                };

                // Ensure something gets written out
                userOutput = RunUserPatch(in postFxContext);
            }
            catch (Exception ex)
            {
                // We want user nodes to turn pink
                RuntimeGraph.ReportException(ex);
            }
        }

        if (!_userTakesCareOfWritingToRenderTarget)
        {
            // Ensure the final output is written to
            if (userOutput != output)
            {
                Scaler.SetInput(userOutput ?? input);
                Scaler.SetOutput(output);
                Scaler.Draw(context);
            }
        }

        Texture? RunUserPatch(in PostFXDrawContext context)
        {
            if (_state is null && _createHandler != null)
                _createHandler(out _state);

            if (_state is null)
                return null;

            if (_drawHandler != null)
            {
                _drawHandler(_state, context, out _state, out var output);
                return output;
            }

            return null;
        }
    }

    private void Schedule(IGraphicsRendererBase graphicsRenderer)
    {
        // We're already in a draw call, so let's draw right away
        graphicsRenderer.Draw(_renderDrawContext);
    }

    protected override void Destroy()
    {
        base.Destroy();

        try
        {
            if (_state is IDisposable disposable)
                disposable.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected exception while disposing");
        }

        _gameHandle.Dispose();
    }

    private Texture? GetInputOrDefault(int index) => index < InputCount ? GetInput(index) : null;

    bool IPostProcessingEffects.RequiresVelocityBuffer => _requiresVelocityBuffer;

    bool IPostProcessingEffects.RequiresNormalBuffer => _requiresNormalBuffer;

    bool IPostProcessingEffects.RequiresSpecularRoughnessBuffer => _requiresSpecularRoughnessBuffer;

    Guid IIdentifiable.Id { get; set; } = Guid.NewGuid();

    void IPostProcessingEffects.Collect(RenderContext context)
    {
    }

    void IPostProcessingEffects.Draw(RenderDrawContext drawContext, RenderOutputValidator outputValidator, Texture[] inputs, Texture inputDepthStencil, Texture outputTarget)
    {
        // Ensure we start from a clean state
        Reset();

        var colorIndex = outputValidator.Find<ColorTargetSemantic>();
        if (colorIndex < 0)
            return;

        SetInput(0, inputs[colorIndex]);
        SetInput(1, inputDepthStencil);

        var normalsIndex = outputValidator.Find<NormalTargetSemantic>();
        if (normalsIndex >= 0)
        {
            SetInput(2, inputs[normalsIndex]);
        }

        var specularRoughnessIndex = outputValidator.Find<SpecularColorRoughnessTargetSemantic>();
        if (specularRoughnessIndex >= 0)
        {
            SetInput(3, inputs[specularRoughnessIndex]);
        }

        var reflectionIndex0 = outputValidator.Find<OctahedronNormalSpecularColorTargetSemantic>();
        var reflectionIndex1 = outputValidator.Find<EnvironmentLightRoughnessTargetSemantic>();
        if (reflectionIndex0 >= 0 && reflectionIndex1 >= 0)
        {
            SetInput(4, inputs[reflectionIndex0]);
            SetInput(5, inputs[reflectionIndex1]);
        }

        var velocityIndex = outputValidator.Find<VelocityTargetSemantic>();
        if (velocityIndex != -1)
        {
            SetInput(6, inputs[velocityIndex]);
        }

        SetOutput(outputTarget);
        Draw(drawContext);
    }
}
