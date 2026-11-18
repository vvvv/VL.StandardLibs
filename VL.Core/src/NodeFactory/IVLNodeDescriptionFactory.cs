#nullable enable
using System;
using System.Collections.Immutable;

namespace VL.Core
{
    /// <summary>
    /// Provides <see cref="IVLNodeDescription"/>s - the nodes shown in the node browser - for a source such as an assembly or a file.
    /// Use <see cref="Invalidated"/> to tell the system that the set of nodes has changed (e.g. after the underlying file was modified).
    /// </summary>
    /// <remarks>
    /// Factories get registered with <see cref="NodeFactoryRegistry.RegisterNodeFactory"/> (<see cref="AppHost.NodeFactoryRegistry"/>),
    /// typically from <see cref="IStartup.Configure"/> (or <see cref="VL.Core.CompilerServices.AssemblyInitializer.Configure(AppHost)"/>).
    /// For each document referencing the factory, the registry also calls <see cref="ForPath"/>, allowing the factory to contribute additional nodes based on the scope of that document.
    /// <para>
    /// Implementing this interface directly is optional. Most factories are created via <see cref="NodeBuilding"/> and
    /// <see cref="AppHost.NodeFactoryCache"/>, which takes care of caching the factory by its identifier and of creating it lazily:
    /// </para>
    /// <code>
    /// appHost.NodeFactoryRegistry.RegisterNodeFactory(appHost.NodeFactoryCache.GetOrAdd("MyLib.Nodes", factory =>
    /// {
    ///     var nodes = ImmutableArray.Create&lt;IVLNodeDescription&gt;(/* ... */);
    ///     return NodeBuilding.NewFactoryImpl(nodes);
    /// }));
    /// </code>
    /// </remarks>
    public interface IVLNodeDescriptionFactory
    {
        /// <summary>
        /// Uniquely identifies this factory.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// The file this factory is based on, if any.
        /// </summary>
        string? FilePath => null;

        /// <summary>
        /// The nodes this factory currently provides.
        /// </summary>
        ImmutableArray<IVLNodeDescription> NodeDescriptions { get; }

        /// <summary>
        /// Notifies when <see cref="NodeDescriptions"/> or any of the descriptions changed and need to be queried again.
        /// </summary>
        IObservable<object> Invalidated { get; }

        /// <summary>
        /// Called for each document referencing this factory, with the path of that document's scope (e.g. its folder).
        /// Allows the factory to contribute nodes which depend on the document using it.
        /// </summary>
        /// <param name="path">The path of the scope of the document.</param>
        /// <returns>A factory providing the additional nodes for that scope, or null if there are none.</returns>
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