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
        public ImmutableStack<UniqueId> Stack { get; }

        public NodePath(ImmutableStack<UniqueId> stack)
        {
            Stack = stack;
        }

        [Obsolete("Please use Stack")]
        [Browsable(false)]
        // Patches like VL.OpenCV reference this property directly. So keep it for now until those patches are fixed.
        public ImmutableStack<uint> ObsoleteStack
        {
            get => ImmutableStack.CreateRange(Stack.Reverse().Select(x => x.VolatileId));
        }

        public bool IsDefault => Stack == null;

        public bool Equals(NodePath other) => other == this;

        public override bool Equals(object obj)
        {
            if (obj is NodePath other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            if (Stack == null || Stack.IsEmpty)
                return 0;
            return Stack.PeekRef().GetHashCode();
        }

        public override string ToString()
        {
            if (Stack == null)
                return base.ToString();
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
