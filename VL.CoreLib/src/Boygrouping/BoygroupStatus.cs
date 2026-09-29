#nullable enable
using System.Collections.Immutable;
using VL.Core;
using VL.Core.Boygrouping;
using VL.Core.Import;
using VL.Lib.Boygrouping;
using VL.Lib.Collections;

[assembly: ImportType(typeof(BoygroupServerStatus), Category = "System.Boygrouping")]
[assembly: ImportType(typeof(BoygroupClientStatus), Category = "System.Boygrouping")]
[assembly: ImportType(typeof(BoygroupClientInfo), Category = "System.Boygrouping")]

namespace VL.Lib.Boygrouping;

[ProcessNode]
public sealed class BoygroupServerStatus
{
    private readonly IBoygroupServerStatusProvider? statusProvider;
    private Spread<BoygroupClientInfo> connectedClients = Spread<BoygroupClientInfo>.Empty;
    private Spread<BoygroupClientInfo> allClients = Spread<BoygroupClientInfo>.Empty;

    public BoygroupServerStatus(NodeContext nodeContext)
    {
        statusProvider = nodeContext.AppHost.Services.GetService<IBoygroupServerStatusProvider>();
    }

    [Fragment(IsDefault = true)]
    public bool IsServer => statusProvider != null;

    /// <inheritdoc cref="IBoygroupServerStatusProvider.ConnectedClients"/>
    [Fragment(IsDefault = true)]
    public Spread<BoygroupClientInfo> ConnectedClients => AsSpread(ref connectedClients, statusProvider?.ConnectedClients);

    /// <inheritdoc cref="IBoygroupServerStatusProvider.AllClients"/>
    public Spread<BoygroupClientInfo> AllClients => AsSpread(ref allClients, statusProvider?.AllClients);

    /// <inheritdoc cref="IBoygroupServerStatusProvider.WorkingDirectory"/>
    public string WorkingDirectory => statusProvider?.WorkingDirectory ?? string.Empty;

    private static Spread<T> AsSpread<T>(ref Spread<T> spread, ImmutableArray<T>? array)
    {
        if (array != spread._array)
            spread = array.HasValue ? new(array.Value) : Spread<T>.Empty;
        return spread;
    }
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
