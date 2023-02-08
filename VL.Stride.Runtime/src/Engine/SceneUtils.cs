using Stride.Core;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core;
using VL.Lib.Collections.TreePatching;

namespace VL.Stride.Engine
{
    public static class SceneUtils
    {
        private static readonly PropertyKey<TreeNodeParentManager<Scene, Scene>> parentManagerKey = new PropertyKey<TreeNodeParentManager<Scene, Scene>>("SceneParentManager", typeof(SceneUtils));

        /// <summary>
        /// Installs a <see cref="TreeNodeParentManager{TParent, TNode}"/> for the given scene.
        /// The manager will be used by the SceneManager node to ensure that the Scene has one parent only.
        /// </summary>
        /// <param name="scene">The scene to manage.</param>
        /// <param name="nodeContext">The path of the node will be used to generate the warning.</param>
        /// <returns>The scene itself.</returns>
        public static Scene WithParentManager(this Scene scene, NodeContext nodeContext)
        {
            var manager = GetParentManager(scene);

            // Subscribe to warnings of the manager and make them visible in the patch
            var warnings = new CompositeDisposable()
                .DisposeBy(scene);

            manager.ToggleWarning.Subscribe(v =>
                {
                    warnings.Clear();
                    if (v)
                    {
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            IVLRuntime.Current?.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Warning, "Scene should only be connected to one parent scene."))
                                .DisposeBy(warnings);
                        }
                    }
                })
                .DisposeBy(scene);

            return scene;
        }

        /// <summary>
        /// Retrieves the parent manager for a scene. A manager will be created on the fly and registered internally for the scene should it not have been created yet.
        /// </summary>
        /// <param name="scene">The scene for which to retrieve a manager for.</param>
        /// <returns>The parent manager for the scene.</returns>
        public static TreeNodeParentManager<Scene, Scene> GetParentManager(this Scene scene)
        {
            var manager = scene.Tags.Get(parentManagerKey);
            if (manager is null)
            {
                manager = new TreeNodeParentManager<Scene, Scene>(scene, (p, c) => c.Parent = p, (p, c) => c.Parent = null)
                    .DisposeBy(scene);
                scene.Tags.Set(parentManagerKey, manager);
            }
            return manager;
        }
    }
}
