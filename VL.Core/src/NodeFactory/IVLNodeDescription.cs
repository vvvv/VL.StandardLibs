#nullable enable
using System;
using System.Collections.Generic;
using VL.Core.Diagnostics;

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLNodeDescription
    {
        IVLNodeDescriptionFactory Factory { get; }
        string Name { get; }
        string Category { get; }
        string? FilePath => null;

        /// <summary>
        /// Whether the system shall create getter and setter fragments for each pin. 
        /// The first getter will also be marked as the default fragment.
        /// </summary>
        bool Fragmented { get; }

        IReadOnlyList<IVLPinDescription> Inputs { get; }
        IReadOnlyList<IVLPinDescription> Outputs { get; }
        IEnumerable<Message> Messages { get; }
        IObservable<object> Invalidated { get; }

        IVLNode CreateInstance(NodeContext context);

        /// <summary>
        /// Return a function that tries opening the editor and returns true when successfull. 
        /// null when it's clear early-on that there is no editor. This is the default implementation.
        /// </summary>
        Func<bool>? OpenEditorAction => null;
    }
}
#nullable restore