using System;
using Stride.Engine;

namespace VL.Stride.Engine
{
    /// <summary>
    /// Establishes a parent-child relationship between a parent and a child <see cref="Entity"/>.
    /// </summary>
    public sealed class EntityLink : IDisposable
    {
        readonly Entity parent;
        Entity child;

        public EntityLink(Entity parent)
        {
            this.parent = parent;
        }

        public void Update(Entity child)
        {
            Child = child;
        }

        public void Dispose()
        {
            Child = null;
        }

        public Entity Child
        {
            get => child;
            set
            {
                if (value != child)
                {
                    if (child != null && child.GetParent() == parent)
                    {
                        // Unlink
                        child.SetParent(null);
                        child.Scene = null;
                        child = null;
                    }
                    if (value != null && value.GetParent() == null)
                    {
                        // Link
                        value.SetParent(parent);
                        child = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Establishes a parent-child relationship between a parent <see cref="Scene"/> and a child <see cref="Entity"/>.
    /// </summary>
    public sealed class EntitySceneLink : IDisposable
    {
        readonly Scene parent;
        Entity child;

        public EntitySceneLink(Scene parent)
        {
            this.parent = parent;
        }

        public void Update(Entity child)
        {
            Child = child;
        }

        public void Dispose()
        {
            Child = null;
        }

        public Entity Child
        {
            get => child;
            set
            {
                if (value != child)
                {
                    if (child != null && child.GetParent() == null && child.Scene == parent)
                    {
                        // Unlink
                        child.Scene = null;
                        child = null;
                    }
                    if (value != null && value.GetParent() == null && value.Scene == null)
                    {
                        // Link
                        value.Scene = parent;
                        child = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Establishes a parent-child relationship between a parent and a child <see cref="Scene"/>.
    /// </summary>
    public sealed class SceneLink : IDisposable
    {
        readonly Scene parent;
        Scene child;

        public SceneLink(Scene parent)
        {
            this.parent = parent;
        }

        public void Update(Scene child)
        {
            Child = child;
        }

        public void Dispose()
        {
            Child = null;
        }

        public Scene Child
        {
            get => child;
            set
            {
                if (value != child)
                {
                    if (child != null && child.Parent == parent)
                    {
                        // Unlink
                        child.Parent = null;
                        child = null;
                    }
                    if (value != null && value.Parent == null)
                    {
                        // Link
                        value.Parent = parent;
                        child = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Establishes a link between a <see cref="EntityComponent"/> and its <see cref="Entity"/>.
    /// </summary>
    public sealed class ComponentLink : IDisposable
    {
        readonly Entity entity;
        EntityComponent component;

        public ComponentLink(Entity entity)
        {
            this.entity = entity;
        }

        public void Update(EntityComponent component)
        {
            Component = component;
        }

        public void Dispose()
        {
            Component = null;
        }

        public EntityComponent Component
        {
            get => component;
            set
            {
                if (value != component)
                {
                    if (component != null && component.Entity == entity)
                    {
                        // Unlink
                        entity.Remove(component);
                        component = null;
                    }
                    if (value != null && value.Entity == null)
                    {
                        // Link
                        entity.Add(value);
                        component = value;
                    }
                }
            }
        }
    }
}
