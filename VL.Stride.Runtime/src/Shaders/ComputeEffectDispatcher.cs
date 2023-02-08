using System;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Shaders;
using Stride.Rendering.ComputeEffect;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Buffer = Stride.Graphics.Buffer;
using System.Reactive.Disposables;
using Stride.Core;

namespace VL.Stride.Shaders
{
    public class ComputeEffectDispatcher : DrawEffect
    {
        private MutablePipelineState pipelineState;
        private bool pipelineStateDirty = true;
        private EffectBytecode previousBytecode;

        public ComputeEffectDispatcher(RenderContext context, string effectName = "ComputeEffectShader")
            : base(context, null)
        {
            Subscriptions.DisposeBy(this);

            pipelineState = new MutablePipelineState(context.GraphicsDevice);

            // Setup the effect compiler
            EffectInstance = new DynamicEffectInstance(effectName, Parameters);
            EffectInstance.Initialize(context.Services);

            // We give ComputeEffectShader a higher priority, since they are usually executed serially and blocking
            EffectInstance.EffectCompilerParameters.TaskPriority = -1;

            ThreadNumbers = new Int3(1);
            ThreadGroupCounts = new Int3(1);

            SetDefaultParameters();
        }

        internal readonly CompositeDisposable Subscriptions = new CompositeDisposable();

        /// <summary>
        /// The current effect instance.
        /// </summary>
        public DynamicEffectInstance EffectInstance { get; private set; }

        /// <summary>
        /// Gets or sets the number of group counts the shader should be dispatched to.
        /// </summary>
        public Int3 ThreadGroupCounts { get; set; }

        /// <summary>
        /// Gets or sets the number of threads desired by thread group.
        /// </summary>
        public Int3 ThreadNumbers { get; set; }

        /// <summary>
        /// Gets or sets the name of the input compute shader file (.xksl)
        /// </summary>
        public string ShaderSourceName { get; set; }

        public string LastError { get; private set; } = string.Empty;
        public bool IsIndirect { get; set; }
        public Buffer IndirectArgsBuffer { get; set; }
        public int ArgsBufferAlignedByteOffset { get; set; }

        /// <summary>
        /// Sets the default parameters (called at constructor time and if <see cref="DrawEffect.Reset"/> is called)
        /// </summary>
        protected override void SetDefaultParameters()
        {
        }

        protected override void PreDrawCore(RenderDrawContext context)
        {
            base.PreDrawCore(context);

            // Default handler for parameters
            UpdateParameters();
        }

        /// <summary>
        /// Updates the effect <see cref="ComputeEffectDispatcher.Parameters" /> from properties defined in this instance. See remarks.
        /// </summary>
        protected virtual void UpdateParameters()
        {
        }
        bool FCompiled;
        private TimeSpan FRefreshTime;

        protected override void DrawCore(RenderDrawContext context)
        {
            if (string.IsNullOrEmpty(ShaderSourceName) || FRefreshTime > context.RenderContext.Time.Total)
                return;

            Parameters.Set(ComputeEffectShaderKeys.ThreadNumbers, ThreadNumbers);
            Parameters.Set(ComputeEffectShaderKeys.ComputeShaderName, ShaderSourceName);
            Parameters.Set(ComputeShaderBaseKeys.ThreadGroupCountGlobal, ThreadGroupCounts);

            var effectUpdated = false;
            try
            {
                effectUpdated = EffectInstance.UpdateEffect(GraphicsDevice);
                FCompiled = true;
                LastError = string.Empty;

            }
            catch (Exception e)
            {
                LastError = e.InnermostException().Message;
                FCompiled = false;
                FRefreshTime = context.RenderContext.Time.Total + TimeSpan.FromSeconds(3);
            }

            if (!FCompiled)
                return;

            try
            {
                if (effectUpdated || pipelineStateDirty || previousBytecode != EffectInstance.Effect.Bytecode)
                {
                    // The EffectInstance might have been updated from outside
                    previousBytecode = EffectInstance.Effect.Bytecode;

                    pipelineState.State.SetDefaults();
                    pipelineState.State.RootSignature = EffectInstance.RootSignature;
                    pipelineState.State.EffectBytecode = EffectInstance.Effect.Bytecode;
                    pipelineState.Update();
                    pipelineStateDirty = false;
                }
            }
            catch (Exception e)
            {

                LastError = e.InnermostException().Message;
                FCompiled = false;
                FRefreshTime = context.RenderContext.Time.Total + TimeSpan.FromSeconds(3);
                return;
            }

            // Apply pipeline state
            var commandList = context.CommandList;
            commandList.SetPipelineState(pipelineState.CurrentState);

            drawRenderDrawContext = context;
            drawCommandList = commandList;

            // Un-apply
            //throw new InvalidOperationException();
            //EffectInstance.Effect.UnbindResources(GraphicsDevice);
        }

        CommandList drawCommandList;
        RenderDrawContext drawRenderDrawContext;

        /// <summary>
        /// Dispatches the compute shader and sets the counter value, change per iteration effect parameters right before this call.
        /// </summary>
        public void DrawIteration()
        {
            if (FCompiled && drawCommandList != null && drawRenderDrawContext != null)
            {
                // Apply the effect, TODO: only update parameters here and Apply only once in Draw
                EffectInstance.Apply(drawRenderDrawContext.GraphicsContext);

                // Dispatch compute shader
                if (IsIndirect)
                    drawCommandList.DispatchIndirect(IndirectArgsBuffer, ArgsBufferAlignedByteOffset);
                else
                    drawCommandList.Dispatch(ThreadGroupCounts.X, ThreadGroupCounts.Y, ThreadGroupCounts.Z);
            }
        }
    }
}
