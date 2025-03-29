using System;
using System.IO;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.IO;

[assembly: ImportType(typeof(FileNode), Category = "IO")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Gets or creates the stream of a file for reading and writing. 
    /// The directory will be created in case it doesn't exist and the file mode is set in a way that a new file should be created.
    /// </summary>
    [ProcessNode(Name = "File")]
    public class FileNode
    {
        private IResourceProvider<Stream> FStreamProvider;
        private Path FFilePath;
        private FileMode FFileMode;
        private FileAccess FFileAccess;
        private FileShare FFileShare;

        public FileNode(NodeContext nodeContext)
        {
        }

        public IResourceProvider<Stream> Update(Path filePath, FileMode fileMode = FileMode.OpenOrCreate, FileAccess fileAccess = FileAccess.Read, FileShare fileShare = FileShare.Read)
        {
            //check if input pin values have changed
            if (FFilePath != filePath || FFileMode != fileMode || FFileAccess != fileAccess || FFileShare != fileShare)
            {
                FFilePath = filePath;
                FFileMode = fileMode;
                FFileAccess = fileAccess;
                FFileShare = fileShare;

                FStreamProvider = ResourceProvider.New(() => 
                {
                    var createsFile = CreatesFile(fileMode);
                    if (createsFile)
                    {
                        var directory = System.IO.Path.GetDirectoryName(filePath);
                        Directory.CreateDirectory(directory);
                    }
                    return (filePath != Path.Default) ? new FileStream(filePath, fileMode, fileAccess, fileShare, StreamUtils.SmallBufferSize, true) : Stream.Null;
                });
            }
            return FStreamProvider;
        }

        static bool CreatesFile(FileMode fileMode)
        {
            switch (fileMode)
            {
                case FileMode.CreateNew:
                case FileMode.Create:
                case FileMode.OpenOrCreate:
                case FileMode.Append:
                    return true;
            }
            return false;
        }
    }
}