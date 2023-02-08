using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VL.Lib.Collections;
using Ookii.Dialogs.WinForms;
using System.Threading;

namespace VL.Lib.IO
{
    /// <summary>
    /// Opens a folder selection dialog and returns the selected folder
    /// </summary>
    public class FolderDialog : IDisposable
    {
        private VistaFolderBrowserDialog FDialog;
        private bool FIsOpen;
        private Path FOutput;
        private bool FOk;

        public FolderDialog()
        {
            FDialog = new VistaFolderBrowserDialog();
            FDialog.UseDescriptionForTitle = true;
            FOutput = Path.Default;
        }

        public void Dispose()
        {
            FDialog.Dispose();
        }

        public Path Update(string title, string initialDirectory, bool showNewFolderButton, bool show, out bool ok)
        {
            if (show && !FIsOpen)
            {
                FDialog.Description = title;
                FDialog.ShowNewFolderButton = showNewFolderButton;
                FDialog.SelectedPath = initialDirectory.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString())?initialDirectory:initialDirectory+ System.IO.Path.DirectorySeparatorChar;

                FIsOpen = true;
                var t = new Thread(() => ShowDialog(FDialog));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            ok = FOk;
            FOk = false;
            return FOutput;
        }

        private DialogResult ShowDialog(VistaFolderBrowserDialog dialog)
        {
            var d = dialog.ShowDialog();
            if (d == DialogResult.OK)
            {
                FOutput = new Path(FDialog.SelectedPath);
                FOk = true;
            }
            FIsOpen = false;
            return d;
        }
    }

    /// <summary>
    /// Opens a file selection dialog and returns the selected file(s)
    /// </summary>
    public class FileDialogOpen : IDisposable
    {
        private OpenFileDialog FDialog;
        private bool FIsOpen;
        private Spread<Path> FOutput;
        private bool FOk;

        public FileDialogOpen()
        {
            FDialog = new OpenFileDialog();
            FOutput = Spread<Path>.Empty;
        }

        public void Dispose()
        {
            FDialog.Dispose();
        }

        public Spread<Path> Update(string title, string initialDirectory, string filter, bool allowMultipleSelections, bool checkPathExists, bool show, out bool ok)
        {
            if (show && !FIsOpen)
            {
                FDialog.Title = title;
                FDialog.InitialDirectory = initialDirectory;
                FDialog.Filter = filter;
                FDialog.Multiselect = allowMultipleSelections;
                FDialog.CheckPathExists = checkPathExists;
                FDialog.CheckFileExists = checkPathExists;
                
                FIsOpen = true;
                var t = new Thread(() => ShowDialog(FDialog));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            ok = FOk;
            FOk = false;
            return FOutput;
        }

        private DialogResult ShowDialog(FileDialog dialog)
        {
            var d = dialog.ShowDialog();
            if (d == DialogResult.OK)
            {
                FOutput = FDialog.FileNames.Select(s => new Path(s)).ToSpread();
                FOk = true;
            }
            FIsOpen = false;
            return d;
        }
    }

    /// <summary>
    /// Opens a file save dialog and returns the selected file
    /// </summary>
    public class FileDialogSave : IDisposable
    {
        private SaveFileDialog FDialog;
        private bool FIsOpen;
        private Path FOutput;
        private bool FOk;

        public FileDialogSave()
        {
            FDialog = new SaveFileDialog();
            FOutput = Path.Default;
        }

        public void Dispose()
        {
            FDialog.Dispose();
        }

        public Path Update(string title, string initialDirectory, string filter, bool allowMultipleSelections, bool checkPathExists, bool show, out bool ok)
        {
            if (show && !FIsOpen)
            {
                FDialog.Title = title;
                FDialog.InitialDirectory = initialDirectory;
                FDialog.Filter = filter;
                FDialog.CheckPathExists = checkPathExists;
                FDialog.CheckFileExists = checkPathExists;

                FIsOpen = true;
                var t = new Thread(() => ShowDialog(FDialog));
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
            }
            ok = FOk;
            FOk = false;
            return FOutput;
        }

        private DialogResult ShowDialog(FileDialog dialog)
        {
            var d = dialog.ShowDialog();
            if (d == DialogResult.OK)
            {
                FOutput = new Path(FDialog.FileName);
                FOk = true;
            }
            FIsOpen = false;
            return d;
        }
    }
}
