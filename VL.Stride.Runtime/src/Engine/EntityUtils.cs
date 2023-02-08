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
    public static class EntityUtils
    {
        private static readonly PropertyKey<TreeNodeParentManager<object, Entity>> parentManagerKey = new PropertyKey<TreeNodeParentManager<object, Entity>>("EntityParentManager", typeof(EntityUtils));

        /// <summary>
        /// Installs a <see cref="TreeNodeParentManager{TParent, TNode}"/> for the given entity.
        /// The manager will be used by the EntityManager node to ensure that the Entity has one parent only.
        /// </summary>
        /// <param name="entity">The entity to manage.</param>
        /// <param name="nodeContext">The path of the node will be used to generate the warning.</param>
        /// <returns>The entity itself.</returns>
        public static Entity WithParentManager(this Entity entity, NodeContext nodeContext)
        {
            var manager = GetParentManager(entity);

            // Subscribe to warnings of the manager and make them visible in the patch
            var warnings = new CompositeDisposable()
                .DisposeBy(entity);

            manager.ToggleWarning.Subscribe(v =>
                {
                    warnings.Clear();
                    if (v)
                    {
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            IVLRuntime.Current?.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Warning, "Entity should only be connected to one parent entity or scene."))
                                .DisposeBy(warnings);
                        }
                    }
                })
                .DisposeBy(entity);

            return entity;
        }

        /// <summary>
        /// Retrieves the parent manager for an entity. A manager will be created on the fly and registered internally for the entity should it not have been created yet.
        /// </summary>
        /// <param name="entity">The entity for which to retrieve a manager for.</param>
        /// <returns>The parent manager for the entity.</returns>
        public static TreeNodeParentManager<object, Entity> GetParentManager(this Entity entity)
        {
            var manager = entity.Tags.Get(parentManagerKey);
            if (manager is null)
            {
                manager = new TreeNodeParentManager<object, Entity>(entity, (p, c) => SetParent(p, c), (p, c) => Detach(c))
                    .DisposeBy(entity);
                entity.Tags.Set(parentManagerKey, manager);
            }
            return manager;
        }

        static void SetParent(object parent, Entity child)
        {
            if (parent is Scene s)
                child.Scene = s;
            else if (parent is Entity entity)
                child.SetParent(entity);
            else
                throw new ArgumentException("Parent must either be a Scene or an Entity.", nameof(parent));
        }

        static void Detach(Entity child)
        {
            // First from parent
            child.SetParent(null);
            // Then from scene
            child.Scene = null;
        }
    }
}
