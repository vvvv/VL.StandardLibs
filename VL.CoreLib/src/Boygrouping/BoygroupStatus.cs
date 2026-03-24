#nullable enable
using System.Collections.Immutable;
using VL.Core;
using VL.Core.Boygrouping;
using VL.Core.Import;
using VL.Lib.Boygrouping;

[assembly: ImportType(typeof(BoygroupServerStatus), Category = "System.Boygrouping")]
[assembly: ImportType(typeof(BoygroupClientStatus), Category = "System.Boygrouping")]
[assembly: ImportType(typeof(BoygroupClientInfo), Category = "System.Boygrouping")]

namespace VL.Lib.Boygrouping;

[ProcessNode]
public sealed class BoygroupServerStatus
{
    private readonly IBoygroupServerStatusProvider? statusProvider;

    public BoygroupServerStatus(NodeContext nodeContext)
    {
        statusProvider = nodeContext.AppHost.Services.GetService<IBoygroupServerStatusProvider>();
    }

    [Fragment(IsDefault = true)]
    public bool IsServer => statusProvider != null;

    /// <inheritdoc cref="IBoygroupServerStatusProvider.ConnectedClients"/>
    [Fragment(IsDefault = true)]
    public ImmutableArray<BoygroupClientInfo> ConnectedClients => statusProvider != null ? statusProvider.ConnectedClients : ImmutableArray<BoygroupClientInfo>.Empty;

    /// <inheritdoc cref="IBoygroupServerStatusProvider.AllClients"/>
    public ImmutableArray<BoygroupClientInfo> AllClients => statusProvider != null ? statusProvider.AllClients : ImmutableArray<BoygroupClientInfo>.Empty;

    /// <inheritdoc cref="IBoygroupServerStatusProvider.WorkingDirectory"/>
    public string WorkingDirectory => statusProvider?.WorkingDirectory ?? string.Empty;
}

[ProcessNode]
public sealed class BoygroupClientStatus
{
    private readonly IBoygroupClientStatusProvider? statusProvider;

    public BoygroupClientStatus(NodeContext nodeContext)
    {
        statusProvider = nodeContext.AppHost.Services.GetService<IBoygroupClientStatusProvider>();
    }

    [Fragment(IsDefault = true)]
    public bool IsClient => statusProvider != null;

    /// <inheritdoc cref="IBoygroupClientStatusProvider.IsConnected"/>
    [Fragment(IsDefault = true)]
    public bool IsConnected => statusProvider != null && statusProvider.IsConnected;

    /// <inheritdoc cref="IBoygroupClientStatusProvider.ServerAddress"/>
    public string ServerAddress => statusProvider?.ServerAddress ?? string.Empty;

    /// <inheritdoc cref="IBoygroupClientStatusProvider.WorkingDirectory"/>
    public string WorkingDirectory => statusProvider?.WorkingDirectory ?? string.Empty;
}
