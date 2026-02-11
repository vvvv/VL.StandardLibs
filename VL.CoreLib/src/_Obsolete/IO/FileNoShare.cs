using System;
using System.IO;
using VL.Core;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;

namespace VL.Lib.IO.Obsolete
{
    /// <summary>
    /// Gets or creates the stream of a file for reading and writing
    /// </summary>
    public class ObsoleteFileNoSharing
    {
        private readonly NodeContext FNodeContext;
        private IFrameClock FFrameClock;
        private IResourceProvider<Stream> FStreamProvider;
        private Path FFilePath;
        private FileMode FFileMode;
        private FileAccess FFileAccess;
        private FileShare FFileShare;

        public ObsoleteFileNoSharing(NodeContext nodeContext, IFrameClock clock)
        {
            FNodeContext = nodeContext;
            FFrameClock = clock;
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

                FStreamProvider = ResourceProvider.New(() => { return (filePath != Path.Default) ? FNodeContext.AppHost.FileSystem.Open(filePath, fileMode, fileAccess, fileShare) : Stream.Null; });
            }
            return FStreamProvider;
        }
    }
}