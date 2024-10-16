using Stride.Animations;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride.Input
{
    [DataContract("CameraInputSourceComponent")]
    [DefaultEntityComponentProcessor(typeof(InputSourceProcessor), ExecutionMode = ExecutionMode.All)]
    public class CameraInputSourceComponent : ActivableEntityComponent
    {
        IInputSource inputSource;

        public IInputSource InputSource
        { 
            get => Enabled ? inputSource : null;
            set => inputSource = value;
        }
    }

    public class CameraInputSourceSceneRenderer : SceneRendererBase
    {
        public IInputSource InputSource { get; set; }

        CameraInputSourceComponent FCameraInputSourceComponent;
        
        public CameraInputSourceComponent CameraInputSourceComponent
        {
            get => FCameraInputSourceComponent;
            set
            {
                if (value != FCameraInputSourceComponent)
                {
                    if (FCameraInputSourceComponent != null)
                        FCameraInputSourceComponent.InputSource = null;
                    FCameraInputSourceComponent = value;
                }
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var inputSource = Enabled ? InputSource ?? context.GetWindowInputSource() : null;

            if (CameraInputSourceComponent != null)
            {
                CameraInputSourceComponent.InputSource = inputSource;
            }
        }
    }
}
