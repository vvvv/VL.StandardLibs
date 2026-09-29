#nullable enable
using System;
using System.Net;

namespace VL.Core.Boygrouping;

public record BoygroupClientInfo(Guid Id, string Name, string Version, IPEndPoint EndPoint)
{
    public string Address => EndPoint.Address.ToString();

    public override string ToString() => Name;
}