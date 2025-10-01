#nullable enable
using System;

namespace VL.Core.Import;

public abstract class ImportAttribute : Attribute
{
    public abstract bool IsMatch(Type type);
    public abstract string? GetCategory(string? ns);
}
