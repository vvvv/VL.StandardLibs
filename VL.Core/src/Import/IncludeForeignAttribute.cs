#nullable enable
using System;

namespace VL.Core.Import;

/// <summary>
/// If set all types and their members not imported by the <see cref="ImportAsIsAttribute"/> are exposed as foreign types in VL.
/// </summary>
/// <remarks>
/// It allows to use the new features provided by the import attributes while keeping compatibility with existing code.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class IncludeForeignAttribute : Attribute;
