using Microsoft.Extensions.DependencyInjection;
using VL.Core;
using VL.Core.Commands;
using VL.Core.Import;
using VL.Lang.PublicAPI;

namespace VL.TestNodes;

[ProcessNode]
public class ANodeWithAWindow : FactoryBasedVLNode, IHasCommands, IDisposable
{
    private readonly IDisposable? nodeSubscription;
    private Form? form;

    public ANodeWithAWindow(NodeContext nodeContext) : base(nodeContext)
    {
        nodeSubscription = IDevSession.Current?.RegisterNode(this);
    }

    public void Update()
    {

    }

    private void CreateWindow()
    {
        if (form is null)
        {
            form = new Form();
            form.HandleDestroyed += (s, e) => form = null;
        }
        form.Visible = true;
        form.Activate();
    }

    IEnumerable<(string Name, ICommand Command)> IHasCommands.Commands
    {
        get
        {
            yield return ("Show Window", Command.Create(CreateWindow).ExecuteOn(AppHost.SynchronizationContext));
        }
    }

    void IDisposable.Dispose()
    {
        form?.Dispose();
        nodeSubscription?.Dispose();
    }
}
