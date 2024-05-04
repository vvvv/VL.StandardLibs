#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VL.Core
{
    public struct NodePath : IEquatable<NodePath>
    {
        private readonly NodeContext? _nodeContext;
        private readonly ImmutableStack<UniqueId>? _stack;

        public ImmutableStack<UniqueId> Stack => _stack ?? _nodeContext?.Stack ?? ImmutableStack<UniqueId>.Empty;

        public NodePath(ImmutableStack<UniqueId> stack)
        {
            _nodeContext = null;
            _stack = stack;
        }

        internal NodePath(NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
            _stack = null;
        }

        [Obsolete("Please use Stack")]
        [Browsable(false)]
        // Patches like VL.OpenCV reference this property directly. So keep it for now until those patches are fixed.
        public ImmutableStack<uint> ObsoleteStack
        {
            get => ImmutableStack.CreateRange(Stack.Reverse().Select(x => x.VolatileId));
        }

        public bool IsDefault => _nodeContext is null && _stack is null;

        public bool Equals(NodePath other) => other == this;

        public override bool Equals(object? obj)
        {
            if (obj is NodePath other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            if (Stack.IsEmpty)
                return 0;
            return Stack.PeekRef().GetHashCode();
        }

        public override string? ToString()
        {
            var sb = new StringBuilder();
            foreach (var x in Stack)
            {
                if (sb.Length > 0)
                    sb.Append(".");
                sb.Append(x);
            }
            return sb.ToString();
        }

        public static bool operator ==(NodePath a, NodePath b)
        {
            var stackA = a.Stack;
            var stackB = b.Stack;
            if (ReferenceEquals(stackA, stackB))
                return true;
            if (ReferenceEquals(stackA, null) || ReferenceEquals(stackB, null))
                return false;
            return stackA.SequenceEqual(stackB);
        }

        public static bool operator !=(NodePath a, NodePath b) => !(a == b);
    }
}
