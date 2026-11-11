using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Runtime.CompilerServices;
using VL.Lib.Basics.Video;
using CommandList = Stride.Graphics.CommandList;
using PipelineState = Stride.Graphics.PipelineState;

namespace VL.Stride.Graphics;

/// <summary>
/// The Direct3D 11 implementation of <see cref="NativeInterop.RestoreCommandListState"/>. All access to Stride's internals
/// for it lives here so it can be adapted in one place when Stride changes.
/// </summary>
static class D3D11StateRestore
{
    // Keep in sync with Stride.Graphics.PipelineState.Apply (Direct3D11)
    public static void Restore(CommandList commandList)
    {
        var pipelineState = commandList.currentPipelineState();
        var context = commandList.nativeDeviceContext();
        if (pipelineState is null || context is null)
            return;

        context.ComputeShader.Set(pipelineState.computeShader());
        context.VertexShader.Set(pipelineState.vertexShader());
        context.PixelShader.Set(pipelineState.pixelShader());
        context.HullShader.Set(pipelineState.hullShader());
        context.DomainShader.Set(pipelineState.domainShader());
        context.GeometryShader.Set(pipelineState.geometryShader());

        context.OutputMerger.SetBlendState(pipelineState.blendState(), context.OutputMerger.BlendFactor, pipelineState.sampleMask());
        context.Rasterizer.State = pipelineState.rasterizerState();
        context.OutputMerger.DepthStencilState = pipelineState.depthStencilState();

        context.InputAssembler.InputLayout = pipelineState.inputLayout();
        context.InputAssembler.PrimitiveTopology = pipelineState.primitiveTopology();
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "currentPipelineState")]
    static extern ref PipelineState currentPipelineState(this CommandList self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "nativeDeviceContext")]
    static extern ref DeviceContext nativeDeviceContext(this CommandList self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "vertexShader")]
    static extern ref VertexShader vertexShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "geometryShader")]
    static extern ref GeometryShader geometryShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "pixelShader")]
    static extern ref PixelShader pixelShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "hullShader")]
    static extern ref HullShader hullShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "domainShader")]
    static extern ref DomainShader domainShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "computeShader")]
    static extern ref ComputeShader computeShader(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "blendState")]
    static extern ref BlendState blendState(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "sampleMask")]
    static extern ref uint sampleMask(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "rasterizerState")]
    static extern ref RasterizerState rasterizerState(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "depthStencilState")]
    static extern ref DepthStencilState depthStencilState(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "inputLayout")]
    static extern ref InputLayout inputLayout(this PipelineState self);
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "primitiveTopology")]
    static extern ref PrimitiveTopology primitiveTopology(this PipelineState self);
}