using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using VL.Core;
using VL.Core.Utils;

namespace VL.Lib.Collections.TreePatching
{
    //public delegate TreeNodeParentManager<TNode, TChild> ParentManagerProviderDelegate<TNode, TChild>(TChild child, bool warnIfParentManagerNotFound);

    /// <summary>
    /// Place this process node in a patch that accepts children on update. Initialize it with your tree node.
    /// </summary>
    public class TreeNodeChildrenManager<TNode, TChild> : IDisposable
    {
        protected readonly TNode node;
        protected readonly bool childrenOrderingMatters;

        protected readonly Func<TChild, TreeNodeParentManager<TNode, TChild>> parentManagerProvider;
        protected IReadOnlyList<TChild> children = Spread<TChild>.Empty;

        public TreeNodeChildrenManager(TNode node, bool childrenOrderingMatters, Func<TChild, TreeNodeParentManager<TNode, TChild>> parentManagerProvider)
        {
            this.node = node;
            this.childrenOrderingMatters = childrenOrderingMatters;
            this.parentManagerProvider = parentManagerProvider ?? throw new ArgumentNullException(nameof(parentManagerProvider));
        }

        void ConnectToAllChildren()
        {
            foreach (var item in this.children)
            {
                var treeNodeParentManager = GetParentManager(item);
                if (treeNodeParentManager != null)
                {
                    treeNodeParentManager.AddWannaBeParent(node);
                    treeNodeParentManager.Rewire();
                }
            }
        }

        void DisconnectFromAllChildren()
        {
            foreach (var item in this.children)
            {
                var treeNodeParentManager = GetParentManager(item);
                if (treeNodeParentManager != null)
                {
                    treeNodeParentManager.RemoveWannaBeParent(node);
                    treeNodeParentManager.Rewire();
                }
            }
        }

        public void Update(IReadOnlyList<TChild> children)
        {
            if (children != this.children)
            {
                if (childrenOrderingMatters)
                {
                    DisconnectFromAllChildren();
                    this.children = children;
                    ConnectToAllChildren();
                }
                else
                {
                    // if ordering doesn't matter we can reduce the rewire calls

                    foreach (var item in this.children)
                        GetParentManager(item)?.RemoveWannaBeParent(node);

                    using var hash = Pooled.GetHashSet<TChild>(); // avoid memory alloc
                    hash.Value.AddRange(this.children);
                    hash.Value.AddRange(children);
                    this.children = children;

                    foreach (var item in this.children)
                        GetParentManager(item)?.AddWannaBeParent(node);

                    // this is where the tree gets built
                    foreach (var item in hash.Value)
                        GetParentManager(item)?.Rewire();
                }
            }
        }

        public void Dispose() => DisconnectFromAllChildren();

        TreeNodeParentManager<TNode, TChild> GetParentManager(TChild child)
        {
            if (child is null)
                return null;
            var manager = parentManagerProvider(child);
            if (manager is null)
                Trace.TraceWarning($"No {typeof(TreeNodeParentManager<TNode, TChild>)} found for {node}.");
            return manager;
        }
    }


    /// <summary>
    /// Place this process node in a patch that outputs a potential child of another node in your tree structure.
    /// </summary>
    public class TreeNodeParentManager<TParent, TNode> : IDisposable
    {
        protected TNode node;
        protected List<TParent> wannaBeParents = new List<TParent>();
        Action<TParent, TNode> attachToNewParent;
        Action<TParent, TNode> detachFromCurrentParent;

        public TreeNodeParentManager(TNode node, Action<TParent, TNode> attachToNewParent, Action<TParent, TNode> detachFromCurrentParent)
        {
            this.node = node;
            this.attachToNewParent = attachToNewParent;
            this.detachFromCurrentParent = detachFromCurrentParent;
        }

        TParent parent;
        public TParent Parent
        {
            get => parent;
            private set
            {
                if (!ReferenceEquals(value, parent))
                {
                    if (parent != null)
                        detachFromCurrentParent(parent, node);

                    parent = value;

                    if (parent != null)
                        attachToNewParent(parent, node);
                }
            }
        }

        public void AddWannaBeParent(TParent parentEntityOrScene)
        {
            wannaBeParents.Add(parentEntityOrScene);
        }

        public void RemoveWannaBeParent(TParent parentEntityOrScene)
        {
            wannaBeParents.Remove(parentEntityOrScene);
        }

        public void Rewire()
        {
            // another option would be too see if the current Parent is in the wannaBeParents still and if yes don't switch.
            // this can lead to different behavior if there are multiple links to certai parents. (two links into the a group, one link gets deleted)
            // taking care of this weird corner case might introduce or reduce irritations. 
            // sticking to the simplistic approach below should have more deterministic touch to it (regarding F8, F5)
            // LastOrDefault() is another very simple idea. This should result in a very responsive system + warning. to be discussed
            Parent = wannaBeParents.FirstOrDefault();

            if (AllowMultipleConnectionsToSameParent)
                ManyWannaBeParents = wannaBeParents.Distinct().Many();
            else
                ManyWannaBeParents = wannaBeParents.Many();
        }

        bool manyWannaBeParents;
        public bool ManyWannaBeParents
        {
            get => manyWannaBeParents;
            private set
            {
                if (manyWannaBeParents != value)
                {
                    if (manyWannaBeParents)
                        ToggleWarning.OnNext(false);
                    manyWannaBeParents = value;
                    if (manyWannaBeParents)
                        ToggleWarning.OnNext(true);
                }
            }
        }

        public bool AllowMultipleConnectionsToSameParent { get; set; } = true;

        public Subject<bool> ToggleWarning { get; } = new Subject<bool>();

        public void Dispose()
        {
            // descriptive thinking: let's get into the state as if no one would be attached downstream.
            wannaBeParents.Clear();

            // we get all neceessary actions for free: disconnecting from current Parent and possibly triggering ToggleWarning.OnNext(false)
            Rewire();
        }
    }
}
