#nullable enable
using System.Linq;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Collections;
using VL.Core.Import;
using VL.Lib.IO;

[assembly: ImportType(typeof(FileDialogs), Category = "IO")]

namespace VL.Lib.IO
{
    /// <summary>
    /// The file dialogs of the app (<see cref="IPlatformServices"/>), asynchronous: the program goes on while the dialog is open, the task gives the path when the person has chosen.
    /// </summary>
    public static class FileDialogs
    {
        /// <summary>Asks the person for existing files. The task gives their paths, none when the person cancelled or the app shows no dialogs.</summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="initialDirectory">The folder the dialog shows first.</param>
        /// <param name="filter">The kinds of files with their patterns: "Text files|*.txt;*.log|All files|*.*".</param>
        /// <param name="allowMultipleSelections">Whether several files can be chosen; else the task gives one at most.</param>
        public static async Task<Spread<Path>> OpenFileDialogAsync(string title = "Open", Path? initialDirectory = null, string filter = "All files (*.*)|*.*", bool allowMultipleSelections = false)
        {
            if (PlatformServices.Default is not { } services)
                return Spread<Path>.Empty;
            var files = await services.ShowOpenFileDialogAsync(title, initialDirectory?.ToString(), filter, allowMultipleSelections);
            return files.Select(f => new Path(f)).ToSpread();
        }

        /// <summary>Asks the person for the path to save a file to. The task gives the path, none when the person cancelled or the app shows no dialogs.</summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="initialDirectory">The folder the dialog shows first.</param>
        /// <param name="fileName">The name the dialog proposes.</param>
        /// <param name="filter">The kinds of files with their patterns: "Text files|*.txt;*.log|All files|*.*".</param>
        public static Task<Optional<Path>> SaveFileDialogAsync(string title = "Save", Path? initialDirectory = null, string fileName = "", string filter = "All files (*.*)|*.*")
            => ToPathAsync(PlatformServices.Default?.ShowSaveFileDialogAsync(title, initialDirectory?.ToString(), fileName, filter));

        static async Task<Optional<Path>> ToPathAsync(Task<Optional<string>>? dialog)
        {
            if (dialog is null)
                return default;
            var file = await dialog;
            return file.HasValue ? new Path(file.Value) : default(Optional<Path>);
        }
    }
}
