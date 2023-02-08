#nullable enable
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace VL.Core
{
    public sealed class ExportContext
    {
        public ExportContext(object projectRootElement, string directoryPath, ConcurrentDictionary<object, object> solutionWideStorage)
        {
            ProjectRootElement = projectRootElement;
            DirectoryPath = directoryPath;
            SolutionWideStorage = solutionWideStorage;
        }

        /// <summary>
        /// The Microsoft.Build.Construction.ProjectRootElement which represent the MSBuild project.
        /// The property is typed as object to avoid a runtime dependency on MSBuild.
        /// Should the factory code do indeed need to interact with the project it needs to ensure that
        /// the method doing the cast will trigger a type load exception should MSBuild not be present (for example if a patch was exported).
        /// For details see https://github.com/microsoft/MSBuildLocator
        /// </summary>
        public object ProjectRootElement { get; }

        /// <summary>
        /// The directory that the project being generated is in.
        /// </summary>
        public string DirectoryPath { get; }

        /// <summary>
        /// A dictionary whose lifetime is tied to the generation of the whole solution.
        /// Can be used to store information whether or not certain tasks (like copying files from a directory) have already been done.
        /// </summary>
        public ConcurrentDictionary<object, object> SolutionWideStorage { get; }

        internal readonly HashSet<object> Visited = new HashSet<object>();
    }
}
#nullable restore