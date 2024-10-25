using Stride.Animations;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Input;
using Stride.Rendering;
using VL.Stride.Shaders.ShaderFX;

namespace VL.Stride.Input
{
    [DataContract("InputSourceComponent")]
    [DefaultEntityComponentProcessor(typeof(InputSourceProcessor), ExecutionMode = ExecutionMode.All)]
    public class InputSourceComponent : ActivableEntityComponent
    {
        public IInputSource InputSource { get; set; }
    }

    internal class InputSourceProcessor : EntityProcessor<InputSourceComponent, InputSourceProcessor.AssociatedData>
    {
        protected override AssociatedData GenerateComponentData([NotNull] Entity entity, [NotNull] InputSourceComponent component)
        {
            return new AssociatedData(); // { InputSource = component.InputSource };
        }

        public class AssociatedData
        {
            //public IInputSource InputSource { get; set; }
        }

        public override void Draw(RenderContext context)
        {
            var inputSource = context.GetWindowInputSource();

            foreach (var entityKeyPair in ComponentDatas)
            {
                var component = entityKeyPair.Key;
                //var associatedData = entityKeyPair.Value;

                if (component.Enabled)
                    component.InputSource = inputSource;
                else
                    component.InputSource = null;
            }
            base.Draw(context);
        }

        /// <summary>Run when a matching entity is removed from this entity processor.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="component"></param>
        /// <param name="data">  The associated data.</param>
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InputSourceComponent component, [NotNull] InputSourceProcessor.AssociatedData data)
        {
            component.InputSource = null;
        }

    }
}
