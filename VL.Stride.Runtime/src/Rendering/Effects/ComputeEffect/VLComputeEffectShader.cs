using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using VL.Lib.Control;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Rendering.ComputeEffect
{
    /// <summary>
    /// A compute effect allowing to customize the dispatch method through <see cref="IComputeEffectDispatcher"/>.
    /// </summary>
    public class VLComputeEffectShader : DrawEffect
    {
        PerFrameParameters[] perFrameParams;
        PerViewParameters[] perViewParams;
        PerDrawParameters[] perDrawParams;
        TexturingParameters[] texturingParams;

        static Dictionary<string, ProfilingKey> profilingKeys = new Dictionary<string, ProfilingKey>();
        ProfilingKey profilingKey;

        private MutablePipelineState pipelineState;
        private bool pipelineStateDirty = true;
        private EffectBytecode previousBytecode;
        private TimeSpan FRefreshTime;
        private bool FCompiled;

        public string LastError { get; private set; }

        public VLComputeEffectShader(RenderContext context, string name, ParameterCollection mixinParams = null)
            : base(name)
        {
            if (mixinParams != null)
                Parameters = mixinParams;
            Subscriptions.DisposeBy(this);
            Initialize(context);
        }

        internal readonly CompositeDisposable Subscriptions = new CompositeDisposable();

        /// <summary>
        /// The current effect instance.
        /// </summary>
        public DynamicEffectInstance EffectInstance { get; private set; }

        /// <summary>
        /// Gets or sets the number of threads per thread group.
        /// This will be set as the [numthreads(X, Y, Z)] attribute in the shader.
        /// </summary>
        public Int3 ThreadGroupSize { get; set; }

        /// <summary>
        /// Gets or sets the dispatcher.
        /// </summary>
        public IComputeEffectDispatcher Dispatcher { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            pipelineState = new MutablePipelineState(Context.GraphicsDevice);

            // Setup the effect compiler
            EffectInstance = new DynamicEffectInstance("ComputeFXEffect", Parameters);

            // We give ComputeEffectShader a higher priority, since they are usually executed serially and blocking
            EffectInstance.EffectCompilerParameters.TaskPriority = -1;

            Parameters.Set(ComputeEffectShaderKeys.ThreadNumbers, new Int3(1));

            EffectInstance.Initialize(Context.Services);
            EffectInstance.UpdateEffect(Context.GraphicsDevice);

            perFrameParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerFrameMap).ToArray();
            perViewParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerViewMap).ToArray();
            perDrawParams = EffectInstance.Parameters.GetWellKnownParameters(WellKnownParameters.PerDrawMap).ToArray();
            texturingParams = EffectInstance.Parameters.GetTexturingParameters().ToArray();

            if (!profilingKeys.TryGetValue(Name, out profilingKey))
            {
                profilingKey = new ProfilingKey(Name);
                profilingKeys[Name] = profilingKey;
            }
            
        }

        /// <summary>
        /// Sets the default parameters (called at constructor time and if <see cref="DrawEffect.Reset"/> is called)
        /// </summary>
        protected override void SetDefaultParameters()
        {
            ThreadGroupSize = new Int3(1);
        }

        protected override void PreDrawCore(RenderDrawContext context)
        {
            base.PreDrawCore(context);

            // Default handler for parameters
            UpdateParameters(context);
        }

        /// <summary>
        /// Updates the effect <see cref="DrawEffect.Parameters"/> from properties defined in this instance.
        /// </summary>
        protected virtual void UpdateParameters(RenderDrawContext context)
        {
            Parameters.Set(ComputeEffectShaderKeys.ThreadNumbers, ThreadGroupSize);

            Parameters.SetPerFrameParameters(perFrameParams, context.RenderContext);

            var renderView = context.RenderContext.RenderView;
            var parentTransformation = context.RenderContext.Tags.Get(EntityRendererRenderFeature.CurrentParentTransformation);
            Parameters.SetPerDrawParameters(perDrawParams, renderView, ref parentTransformation);

            Parameters.SetPerViewParameters(perViewParams, renderView);

            Parameters.SetTexturingParameters(texturingParams);

            Dispatcher?.UpdateParameters(Parameters, ThreadGroupSize);
        }

        List<ParameterKey> uavs = new List<ParameterKey>();

        protected override void DrawCore(RenderDrawContext context)
        {
            if (string.IsNullOrEmpty(Name) || FRefreshTime > context.RenderContext.Time.Total)
                return;

            using (Profiler.Begin(profilingKey))
            using (context.PushRenderTargetsAndRestore())
            {
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

                        // get potential UAVs to unset
                        uavs.Clear();
                        var parameters = EffectInstance.Parameters;
                        if (parameters.HasLayout)
                        {
                            uavs.AddRange(parameters.Layout.LayoutParameterKeyInfos
                                .Where(p => typeof(Buffer).IsAssignableFrom(p.Key.PropertyType) || typeof(Texture).IsAssignableFrom(p.Key.PropertyType))
                                .Select(k => k.Key));
                        }
                    }
                }
                catch (Exception e)
                {

                    LastError = e.InnermostException().Message;
                    FCompiled = false;
                    FRefreshTime = context.RenderContext.Time.Total + TimeSpan.FromSeconds(3);
                    return;
                }

                // Clear render targets
                context.CommandList.SetRenderTargets(null);

                // Apply pipeline state
                context.CommandList.SetPipelineState(pipelineState.CurrentState);

                // Apply the effect
                EffectInstance.Apply(context.GraphicsContext);

                // Dispatch
                Dispatcher?.Dispatch(context);

                // Unset UAV
                for (int i = 0; i < uavs.Count; i++)
                {
                    UnsetUAV(context.CommandList, EffectInstance.Parameters, uavs[i]);
                }

            }
        }

        MethodInfo unsetUAV;
        object[] unsetUAVArg = new object[1];
        void UnsetUAV (CommandList commandList, ParameterCollection parameters, ParameterKey resourceKey)
        {
            var gr = parameters?.GetObject(resourceKey);

            GraphicsResource resource = null;
            if (gr is Buffer b)
            {
                if ((b.ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    resource = b;

            }
            else if (gr is Texture t)
            {
                if ((t.ViewFlags & TextureFlags.UnorderedAccess) != 0)
                    resource = t;
            }

            if (resource != null)
            {
                unsetUAV ??= typeof(CommandList).GetMethod("UnsetUnorderedAccessView", BindingFlags.NonPublic | BindingFlags.Instance);
                unsetUAVArg[0] = resource;
                unsetUAV.Invoke(commandList, unsetUAVArg);
            }
        }

    }
}
