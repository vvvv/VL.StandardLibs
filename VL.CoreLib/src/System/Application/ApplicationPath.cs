#nullable enable

using System.IO;
using VL.Core;
using VL.Core.Import;
using VL.Lib;

[assembly: ImportType(typeof(ApplicationPath), Category = "System.Application")]

namespace VL.Lib;

/// <summary>
/// Returns the path to the directory of the application, including a trailing directory separator.
/// </summary>
/// <remarks>
/// <para>
/// During development: Returns the path to the directory containing the main .vl document.
/// When exported: Returns the path to the directory containing the executable.
/// </para>
/// <para>
/// The returned path always ends with a directory separator character (\ on Windows, / on Unix-based systems),
/// allowing for direct string concatenation with file or folder names.
/// </para>
/// </remarks>
[ProcessNode]
public sealed class ApplicationPath
{
    public ApplicationPath()
    {
        Output = $"{AppHost.Current.AppBasePath}{Path.DirectorySeparatorChar}";
    }

    [Fragment(IsDefault = true)]
    public string Output { get; }
}
