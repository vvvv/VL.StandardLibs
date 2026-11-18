#nullable enable
using System;
using System.Collections.Generic;
using VL.Core.Diagnostics;

namespace VL.Core
{
    /// <summary>
    /// Describes a node: its name, category, pins and how to create instances of it. Nodes shown in the node browser
    /// are backed by such descriptions, which are provided by an <see cref="IVLNodeDescriptionFactory"/>.
    /// </summary>
    public interface IVLNodeDescription
    {
        /// <summary>
        /// The factory which provided this description.
        /// </summary>
        IVLNodeDescriptionFactory Factory { get; }
        /// <summary>
        /// The name of the node as shown in the node browser.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// The category of the node, used to group nodes in the node browser (e.g. "Primitive" or "Graphics.Skia"). Use dots for sub-categories.
        /// </summary>
        string Category { get; }
        /// <summary>
        /// The file this node originates from, if any. Used for navigating to the node's source.
        /// </summary>
        string? FilePath => null;

        /// <summary>
        /// Whether the system shall create getter and setter fragments for each pin. 
        /// The first getter will also be marked as the default fragment.
        /// </summary>
        bool Fragmented { get; }

        /// <summary>
        /// The input pins of the node.
        /// </summary>
        IReadOnlyList<IVLPinDescription> Inputs { get; }
        /// <summary>
        /// The output pins of the node.
        /// </summary>
        IReadOnlyList<IVLPinDescription> Outputs { get; }
        /// <summary>
        /// Diagnostics (warnings, errors) to show on the node.
        /// </summary>
        IEnumerable<Message> Messages { get; }
        /// <summary>
        /// Notifies when this description changed (e.g. its pins) and nodes using it need to be updated.
        /// </summary>
        IObservable<object> Invalidated { get; }

        /// <summary>
        /// Creates a new instance of the node.
        /// </summary>
        IVLNode CreateInstance(NodeContext context);

        /// <summary>
        /// Return a function that tries opening the editor and returns true when successfull. 
        /// null when it's clear early-on that there is no editor. This is the default implementation.
        /// </summary>
        Func<bool>? OpenEditorAction => null;
    }
}
#nullable restore