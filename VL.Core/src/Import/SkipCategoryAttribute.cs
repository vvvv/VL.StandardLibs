using System;

namespace VL.Core.Import;

/// <summary>
/// Use on static classes to prevent them from being categorized. 
/// This is useful for classes that are only used as containers for extension methods, and should not be visible in the category browser.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SkipCategoryAttribute : Attribute
{
}
