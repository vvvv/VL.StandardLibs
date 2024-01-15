using System;
using System.Collections.Generic;
using System.Linq;

namespace VL.Core
{
    public abstract class NodeFactoryRegistry
    {
        public abstract void RegisterNodeFactory(IVLNodeDescriptionFactory factory);
        public abstract void RegisterPath(string path);

        /// <summary>
        /// The registered paths. Each registered node factory can produce additional factories per path.
        /// </summary>
        public abstract IEnumerable<string> Paths { get; }

        public abstract IEnumerable<IVLNodeDescriptionFactory> Factories { get; }

        public IReadOnlyDictionary<string, IVLNodeDescriptionFactory> FactoriesById
        {
            get
            {
                return factoriesById ??= Compute();

                Dictionary<string, IVLNodeDescriptionFactory> Compute()
                {
                    var result = new Dictionary<string, IVLNodeDescriptionFactory>();
                    foreach (var factory in Factories)
                        result[factory.Identifier] = factory;
                    return result;
                }
            }
        }
        Dictionary<string, IVLNodeDescriptionFactory> factoriesById;

        public IVLNode CreateNode(NodeContext context, string name, string category)
        {
            var nodeDesc = GetNodeDescription(name, category);
            if (nodeDesc is null)
                throw new ArgumentException($"Node \"{name} [{category}]\" not found.");

            return nodeDesc.CreateInstance(context);
        }

        public IVLNodeDescription GetNodeDescription(string name, string category)
        {
            foreach (var nodeFactory in Factories)
            {
                var nodeDesc = nodeFactory.NodeDescriptions.FirstOrDefault(d => d.Name == name && d.Category == category);
                if (nodeDesc != null)
                    return nodeDesc;
            }
            return null;
        }
    }
}
