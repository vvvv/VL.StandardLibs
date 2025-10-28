#nullable enable

using System.IO;
using VL.Core;
using VL.Core.Import;
using VL.Lib;

[assembly: ImportType(typeof(PatchPath), Category = "System.Application")]

namespace VL.Lib;

/// <summary>
/// Returns the path to the directory of the current patch, including a trailing directory separator.
/// </summary>
/// <remarks>
/// <para>
/// During development: Returns the path to the directory containing the .vl document where the calling patch is defined.
/// When exported: Returns the path to the directory containing the assembly where the calling patch is compiled.
/// </para>
/// <para>
/// The returned path always ends with a directory separator character (\ on Windows, / on Unix-based systems),
/// allowing for direct string concatenation with file or folder names.
/// </para>
/// </remarks>
[ProcessNode]
public sealed class PatchPath
{
    public PatchPath(NodeContext nodeContext)
    {
        var inferedPath = InferPath(nodeContext) ?? nodeContext.AppHost.AppBasePath;
        Output = $"{inferedPath}{Path.DirectorySeparatorChar}";

        static string? InferPath(NodeContext context)
        {
            // App (DefId = App) -> Foo (DefId = Foo) -> PatchPath (DefId = PatchPath)
            var typeId = context.Parent?.DefinitionId;
            if (!typeId.HasValue)
                return null;

            var appHost = context.AppHost;
            var typeRegistry = appHost.TypeRegistry;
            var typeInfo = typeRegistry.GetTypeById(typeId.Value);
            if (typeInfo is null)
                return null;

            var filePath = appHost.IsExported ? typeInfo.ClrType.Assembly.Location : appHost.GetDocumentPath(typeId.Value);
            if (string.IsNullOrEmpty(filePath))
                return null;

            return Path.GetDirectoryName(filePath);
        }
    }

    [Fragment(IsDefault = true)]
    public string Output { get; }
}
