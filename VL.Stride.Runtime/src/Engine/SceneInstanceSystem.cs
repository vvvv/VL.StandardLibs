using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;

namespace VL.Stride.Engine
{
    /// <summary>
    /// A game system that updates a scene instance. Drawing is done separately by the <see cref="SceneInstanceRenderer"/>.
    /// This allows you to render the same scene multiple times.
    /// </summary>
    public class SceneInstanceSystem : GameSystemBase
    {
        public SceneInstanceSystem([NotNull] IServiceRegistry registry) : base(registry)
        {
            Enabled = true;
            Visible = true;

            SceneInstance = new SceneInstance(registry);
        }

        public Scene RootScene
        {
            get => SceneInstance.RootScene;
            set => SceneInstance.RootScene = value;
        }

        /// <summary>
        /// Gets the scene instance.
        /// </summary>
        public SceneInstance SceneInstance { get; }

        public override void Update(GameTime gameTime)
        {
            // Game runs after Patch.Update which in turn could've disposed the system
            if (IsDisposed)
                return;

            SceneInstance.Update(gameTime);
        }

        public override bool BeginDraw()
        {
            // Do nothing. We can get drawn by multiple sinks through the IGraphicsRendererBase.Draw call.
            return false;
        }
    }
}
