using VL.Core;
using VL.Core.Import;

namespace VL.TestNodes;

[ProcessNodeFactory(typeof(Factory))]
public class FancyNode
{
    class Factory : ProcessNodeFactory
    {
        public override IEnumerable<Node> GetNodes()
        {
            yield return new(new() { Name = "FancyNode1", Summary = "Something fancy", Tags = "fancy" }, PrivateData: "FancyNode1");
            yield return new(new() { Name = "FancyNode2", Summary = "Something fancy", Tags = "fancy" }, PrivateData: "FancyNode2");
        }

        public override IEnumerable<Node> GetNodesForPath(string path)
        {
            yield return new(new() { Name = $"FancyNodeFrom{Path.GetFileName(path)}", Tags = "fancy" }, PrivateData: path);
        }
    }

    private readonly string privateData;

    public FancyNode(NodeContext nodeContext)
    {
        privateData = nodeContext.PrivateData ?? throw new InvalidOperationException("No private data");
    }

    public string Update()
    {
        return privateData;
    }
}
