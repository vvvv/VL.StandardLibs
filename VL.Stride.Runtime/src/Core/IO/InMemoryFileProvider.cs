namespace Stride.Core.IO;

// From Stride.Core.Tests/MemoryFileProvider.cs
sealed class MemoryFileProvider : VirtualFileProviderBase, IDisposable
{
    private readonly Dictionary<string, FileInfo> files = [];

    public MemoryFileProvider(string rootPath) : base(rootPath)
    {
    }

    public override bool FileExists(string url) => files.ContainsKey(url);

    public override void FileMove(string sourceUrl, string destinationUrl)
    {
        if (files.Remove(sourceUrl, out var fileInfo))
            files.Add(destinationUrl, fileInfo);
        else
            throw new FileNotFoundException();
    }

    public override bool DirectoryExists(string url)
    {
        return false;
    }

    public override void CreateDirectory(string url)
    {
    }

    public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
    {
        lock (files)
        {
            var exists = files.TryGetValue(url, out var fileInfo);
            var write = access != VirtualFileAccess.Read;

            switch (mode)
            {
                case VirtualFileMode.CreateNew:
                    if (exists)
                        throw new IOException("File already exists.");
                    files.Add(url, fileInfo = new FileInfo());
                    return new MemoryFileStream(this, fileInfo, write);
                case VirtualFileMode.Create:
                    files.Remove(url);
                    files.Add(url, fileInfo = new FileInfo());
                    return new MemoryFileStream(this, fileInfo, write);
                case VirtualFileMode.Truncate:
                    if (!exists)
                        throw new IOException("File doesn't exists.");
                    files.Remove(url);
                    return new MemoryStream();
                case VirtualFileMode.Open:
                    if (!exists)
                        throw new FileNotFoundException();
                    if (write)
                        throw new NotImplementedException();
                    return new MemoryFileStream(this, fileInfo!, false, fileInfo!.Data);
                case VirtualFileMode.OpenOrCreate:
                    if (!exists)
                        files.Add(url, fileInfo = new FileInfo());
                    return new MemoryFileStream(this, fileInfo, write);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    private class FileInfo
    {
        public byte[] Data;
        public int Streams;
    }

    private class MemoryFileStream : MemoryStream
    {
        private readonly MemoryFileProvider provider;
        private readonly FileInfo fileInfo;

        public MemoryFileStream(MemoryFileProvider provider, FileInfo fileInfo, bool write)
        {
            this.provider = provider;
            this.fileInfo = fileInfo;
            Initialize(fileInfo, write);
        }

        public MemoryFileStream(MemoryFileProvider provider, FileInfo fileInfo, bool write, byte[] data)
            : base(data)
        {
            this.provider = provider;
            this.fileInfo = fileInfo;
            Initialize(fileInfo, write);
        }

        private static void Initialize(FileInfo fileInfo, bool write)
        {
            if (Interlocked.Increment(ref fileInfo.Streams) > 1 && write)
                throw new InvalidOperationException();
        }

        protected override void Dispose(bool disposing)
        {
            lock (provider.files)
            {
                fileInfo.Data = ToArray();
                Interlocked.Decrement(ref fileInfo.Streams);
            }
            base.Dispose(disposing);
        }
    }
}
