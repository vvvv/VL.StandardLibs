using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using VL.Core;
using VL.Lib.Collections.TreePatching;

namespace VL.Stride.Engine
{
    public static class ComponentUtils
    {
        private static readonly ConditionalWeakTable<EntityComponent, TreeNodeParentManager<Entity, EntityComponent>> managers = new ConditionalWeakTable<EntityComponent, TreeNodeParentManager<Entity, EntityComponent>>();

        /// <summary>
        /// Installs a <see cref="TreeNodeParentManager{TParent, TNode}"/> for the given component.
        /// The manager will be used by the Entity node to ensure that the Component is attached to one entity only.
        /// </summary>
        /// <param name="component">The component to manage.</param>
        /// <param name="nodeContext">The path of the node will be used to generate the warning.</param>
        /// <param name="container">The container to which the lifetime of the generated objects will be tied to.</param>
        /// <returns>The component itself.</returns>
        public static TComponent WithParentManager<TComponent>(this TComponent component, NodeContext nodeContext, ICollection<IDisposable> container)
            where TComponent : EntityComponent
        {
            var manager = GetParentManager(component)
                .DisposeBy(container);

            // Subscribe to warnings of the manager and make them visible in the patch
            var warnings = new CompositeDisposable()
                .DisposeBy(container);

            manager.ToggleWarning.Subscribe(v =>
                {
                    warnings.Clear();
                    if (v)
                    {
                        foreach (var id in nodeContext.Path.Stack)
                        {
                            IVLRuntime.Current?.AddPersistentMessage(new VL.Lang.Message(id, Lang.MessageSeverity.Warning, "Component should only be connected to one Entity."))
                                .DisposeBy(warnings);
                        }
                    }
                })
                .DisposeBy(container);

            return component;
        }

        /// <summary>
        /// Retrieves the parent manager for a component. A manager will be created on the fly and registered internally for the component should it not have been created yet.
        /// </summary>
        /// <param name="component">The component for which to retrieve a manager for.</param>
        /// <returns>The parent manager for the component.</returns>
        public static TreeNodeParentManager<Entity, EntityComponent> GetParentManager(this EntityComponent component)
        {
            return managers.GetValue(component, component => new TreeNodeParentManager<Entity, EntityComponent>(component, (e, c) => e.Add(c), (e, c) => e.Remove(c)));
        }
    }
}
