using System;
using System.Collections.Generic;

namespace VL.Core.CompilerServices
{
    public sealed class DependencyCollector
    {
        private readonly HashSet<object> visited = new();
        private readonly List<AssemblyInitializer> dependencies = new();
        private readonly List<(string path, string packageId)> paths = new();

        public void AddDependency(AssemblyInitializer dependency)
        {
            if (visited.Add(dependency))
            {
                dependency.CollectDependencies(this);
                dependencies.Add(dependency);
            }
        }

        public void AddPath(string path)
        {
            if (visited.Add(path))
                paths.Add((path, null));
        }

        public void AddPath(string path, string packageId)
        {
            var tuple = (path, packageId);
            if (visited.Add(tuple))
                paths.Add(tuple);
        }

        internal List<AssemblyInitializer> Dependencies => dependencies;

        internal List<(string path, string packageId)> Paths => paths;
    }
}
