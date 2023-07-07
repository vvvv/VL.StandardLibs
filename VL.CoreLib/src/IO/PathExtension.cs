using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetPath = System.IO.Path;
using VL.Core;
using Microsoft.VisualBasic.FileIO;

namespace VL.Lib.IO
{
    public static class PathExtension
    {
        /// <summary>
        /// Converts the string to a Path
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static Path ToPath(string input)
        {
            return new Path(input);
        }

        /// <summary>
        /// Converts the string to a Path, explicitly decide if file or folder
        /// </summary>
        /// <param name="input"></param>
        /// <param name="isDirectory"></param>
        /// <returns></returns>
        public static Path ToPathExplicit(string input, bool isDirectory)
        {
            return isDirectory?ToDirectoryPath(input):ToFilePath(input);
        }

        public static Path ToFilePath(string input)
        {
            try
            {
                return Path.FilePath(input);
            }
            catch
            {
                return new Path(input);
            }
        }

        public static Path ToDirectoryPath(string input)
        {
            try
            {
                return Path.DirectoryPath(input);
            }
            catch
            {
                return new Path(input);
            }
        }

        /// <summary>
        /// Creates a Path from directory, file and extension strings
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static Path Filename(string directory, string filename, string extension)
        {
            extension = extension.Trim();
            if (!string.IsNullOrEmpty(extension))
                filename += "." + extension;
            if (!string.IsNullOrEmpty(filename))
                return ToFilePath(NetPath.Combine(directory, filename));
            else
                return new Path(directory); //was ToDirectoryPath but takes too much perf while there's no good default we can set
        }

        /// <summary>
        /// Combines strings to a path
        /// </summary>
        /// <param name="input"></param>
        /// <param name="input2"></param>
        /// <returns></returns>
        public static Path MakePath(Path input, string input2)
        {
            string root = $"x:{NetPath.DirectorySeparatorChar}";
            string builder = input;

            bool isRooted = NetPath.IsPathRooted(builder);
            if (!isRooted)
                builder = NetPath.Combine(root, builder);

            builder = NetPath.Combine(builder, input2);

            bool endsWithSeparator = builder.EndsWith(NetPath.DirectorySeparatorChar.ToString());
            if (!endsWithSeparator)
                builder += NetPath.DirectorySeparatorChar;
            builder = NetPath.GetFullPath(builder);  // combines c:\foo\bar + ..\fighters -> c:\foo\figthers
            if (!endsWithSeparator)
                builder = builder.Remove(builder.Length - 1);
            if (!isRooted)           // but Path.GetFullPath needs a rooted path.     
                builder = builder.Replace(root, string.Empty); //want it relative, subtract root again

            return new Path(builder);
        }

        /* only if implict to subtype works
        //Combines strings to a path
        public static Path MakePath(string input, string input2)
        {
            string root = $"x:{NetPath.DirectorySeparatorChar}";
            string builder = input;

            bool isRooted = NetPath.IsPathRooted(builder);
            if (!isRooted)
                builder = NetPath.Combine(root, builder);

            builder = NetPath.Combine(builder, input2);

            builder = NetPath.GetFullPath(builder);  // combines c:\foo\bar + ..\fighters -> c:\foo\figthers
            if (!isRooted)           // but Path.GetFullPath needs a rooted path.     
                builder = builder.Replace(root, string.Empty); //want it relative, subtract root again

            return new Path(builder);
        } */

        /// <summary>
        /// Checks if a path is absolute or relative
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool IsRooted(Path input)
        {
            return NetPath.IsPathRooted(input);
        }

        /// <summary>
        /// Normalizes the specified path, aka canonicalization.
        /// e.g. converts c:\aaa\bbb\..\ccc to c:\aaa\ccc
        /// This operation is rather slow.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Path Normalize(this Path path)
        {
            return new Path(new Uri(path).LocalPath);
        }

        /// <summary>
        /// Returns various folders of the system
        /// </summary>
        /// <param name="specialFolder"></param>
        /// <returns></returns>
        public static Path SystemFolder(SpecialFolder specialFolder)
        {
            var spf = (Environment.SpecialFolder)specialFolder;
            return new Path(null, Environment.GetFolderPath(spf));
        }

        /// <summary>
        /// Creates the folder of the path
        /// </summary>
        /// <param name="input"></param>
        /// <param name="create"></param>
        /// <param name="success"></param>
        /// <returns></returns>
        public static Path CreateDirectory(this Path input, bool create, out bool success)
        {
            success = false;
            if (create)
                input.CreateDirectory(out success);
            return input;
        }

        /// <summary>
        /// Moves the file or folder to a new location
        /// </summary>
        /// <param name="input"></param>
        /// <param name="newPath"></param>
        /// <param name="replaceExisting"></param>
        /// <returns></returns>
        public static Path Move(this Path input, Path newPath, bool replaceExisting)
        {
            return input.Move(newPath, replaceExisting);
        }

        /// <summary>
        /// Renames the file or folder
        /// </summary>
        /// <param name="input"></param>
        /// <param name="newName"></param>
        /// <param name="replaceExisting"></param>
        /// <returns></returns>
        public static Path Rename(this Path input, string newName, bool replaceExisting)
        {
            return input.Rename(newName, replaceExisting);
        }

        /// <summary>
        /// Copies the file or folder to a new location
        /// </summary>
        /// <param name="input"></param>
        /// <param name="newPath"></param>
        /// <param name="replaceExisting"></param>
        /// <param name="copy"></param>
        /// <returns></returns>
        public static Path Copy(this Path input, Path newPath, bool replaceExisting, out Path copy)
        {
            var w = new List<Func<Path>>();
            copy = input.CopyTo(newPath, replaceExisting, out w);
            foreach (var f in w)
                f.Invoke();
            return input;
        }

        /// <summary>
        /// Deletes the file or folder to Recycle Bin, optionally removes it completely.
        /// </summary>
        public static void Delete(this Path input, out bool success, bool toRecycleBin = true, bool @do = false)
        {
            if (toRecycleBin)
                DeleteRecycleBin(input, @do, out success);
            else
                DeleteHard(input, @do, out success);
        }

        /// <summary>
        /// Deletes the file or folder
        /// </summary>
        /// <param name="input"></param>
        /// <param name="do"></param>
        /// <param name="success"></param>
        public static void DeleteHard(this Path input, bool @do, out bool success)
        {
            if (!@do)
                success = false;
            else
                success = input.Delete();
        }

        /// <summary>
        /// Moves a file or folder to Recycle bin.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="do"></param>
        /// <param name="success"></param>
        public static void DeleteRecycleBin(this Path input, bool @do, out bool success)
        {
            if (!@do || input == null || !input.IsRooted)
                success = false;
            else
            {
                try
                {
                    if (input.IsDirectory)
                        FileSystem.DeleteDirectory(input, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                    else
                        FileSystem.DeleteFile(input, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);

                    success = true;
                }
                catch (Exception)
                {
                    success = false;
                }
            }
        }
    }

    //translates to two Process nodes in vl:
    //- Copier
    //- Mover
    public class FileMover : IDisposable
    {
        Path FFromPath;
        Path FToPath;
        bool FReplaceExisting;
        bool FSuccess;

        private Task FLastTask;
        private CancellationTokenSource FLastCancellationTokenSource;

        public FileMover()
        {
            FFromPath = Path.Default;
            FToPath = Path.Default;
        }

        public void Dispose()
        {
            if (FLastCancellationTokenSource != null)
                FLastCancellationTokenSource.Cancel();
        }

        private void Update(Path from, Path to, bool replaceExisting, bool @do, bool abort, out bool inProgress, out bool success,
            Action<Path, Path, bool, CancellationToken> worker)
        {
            abort |= (FFromPath != from || FToPath != to || FReplaceExisting != replaceExisting || @do);

            if (FLastCancellationTokenSource != null && abort) //cancel task if running
                FLastCancellationTokenSource.Cancel();

            if (FLastTask != null && FLastTask.IsFaulted)
                throw FLastTask.Exception.InnerException;

            if (@do && (from!=Path.Default)  && (to!=Path.Default))
            {
                FFromPath = from;
                FToPath = to;
                FReplaceExisting = replaceExisting;

                var cts = new CancellationTokenSource();
                FLastCancellationTokenSource = cts;
                FLastTask = Task.Run(() => worker(from, to, replaceExisting, cts.Token), cts.Token)
                    .ContinueWith((a) => EndWorker(a), TaskScheduler.FromCurrentSynchronizationContext());
            }
            success = FSuccess;
            if (FSuccess)
                FSuccess = false;
            inProgress = (FLastTask != null && (FLastTask.Status == TaskStatus.Running || FLastTask.Status == TaskStatus.WaitingForActivation));
        }

        void EndWorker(Task antecedent)
        {
            if (!(antecedent.IsCanceled || antecedent.IsFaulted))
                FSuccess = true;
        }

        public void CopyAsync(Path from, Path to, bool replaceExisting, bool copy, bool abort, out bool inProgress, out bool success)
        {
            Update(from, to, replaceExisting, copy, abort, out inProgress, out success, CopyTask);
        }

        void CopyTask(Path from, Path to, bool replace, CancellationToken ct)
        {
            var fs = new List<Func<Path>>();
            from.CopyTo(to, replace, out fs);
            foreach (var f in fs)
            {
                ct.ThrowIfCancellationRequested();
                f.Invoke();
            }
        }

        public void MoveAsync(Path from, Path to, bool replaceExisting, bool move, bool abort, out bool inProgress, out bool success)
        {
            Update(from, to, replaceExisting, move, abort, out inProgress, out success, MoveTask);
        }

        void MoveTask(Path from, Path to, bool replace, CancellationToken ct)
        {
            from.Move(to, replace);
            ct.ThrowIfCancellationRequested();
        }
    }
}
