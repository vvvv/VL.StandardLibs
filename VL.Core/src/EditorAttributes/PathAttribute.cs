using System;

namespace VL.Core.EditorAttributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
public sealed class PathAttribute : Attribute
{
    public const string DefaultFilter = "All files (*.*)|*.*";

    public PathAttribute(bool isDirectory)
    {
        IsDirectory = isDirectory;
    }

    public bool IsDirectory { get; }

    public string Filter { get; set; }

    public override string ToString()
    {
        if (IsDirectory)
            return "Directory";
        else
            return $"File: {Filter}";
    }
}
