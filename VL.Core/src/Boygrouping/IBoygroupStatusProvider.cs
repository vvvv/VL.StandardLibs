#nullable enable
using System.Collections.Immutable;

namespace VL.Core.Boygrouping
{
    internal interface IBoygroupServerStatusProvider
    {
        /// <summary>
        /// The currently connected clients.
        /// </summary>
        ImmutableArray<BoygroupClientInfo> ConnectedClients { get; }

        /// <summary>
        /// All clients which have ever connected since the server started.
        /// </summary>
        ImmutableArray<BoygroupClientInfo> AllClients { get; }

        /// <summary>
        /// The working directory of the server. Only files within the working directory are synchronized with the clients.
        /// </summary>
        string WorkingDirectory { get; }
    }

    internal interface IBoygroupClientStatusProvider
    {
        /// <summary>
        /// Whether or not the client is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The server address.
        /// </summary>
        string ServerAddress { get; }

        /// <summary>
        /// The working directory of the client.
        /// </summary>
        string WorkingDirectory { get; }
    }
}
