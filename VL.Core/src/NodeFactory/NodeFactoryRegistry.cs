using System.Collections.Generic;

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
    }
}
