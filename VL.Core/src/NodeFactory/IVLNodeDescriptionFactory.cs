#nullable enable
using System;
using System.Collections.Immutable;

namespace VL.Core
{
    /// <summary>
    /// WARNING: This interface is experimental!
    /// </summary>
    public interface IVLNodeDescriptionFactory
    {
        string Identifier { get; }

        string? FilePath => null;

        ImmutableArray<IVLNodeDescription> NodeDescriptions { get; }

        IObservable<object> Invalidated { get; }

        IVLNodeDescriptionFactory? ForPath(string path);

        /// <summary>
        /// Allows the node factory to take part in the export process. Gets invoked for each node factory directly referenced by the project.
        /// The invocations will be in build order. 
        /// Use <see cref="ExportContext.SolutionWideStorage"/> to store information across the whole export process.
        /// </summary>
        /// <param name="exportContext">The export context of the current project.</param>
        void Export(ExportContext exportContext);
    }
}
#nullable restore