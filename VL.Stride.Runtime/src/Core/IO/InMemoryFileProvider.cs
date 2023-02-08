using Stride.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VL.Stride.Core.IO
{
    class InMemoryFileProvider : VirtualFileProviderBase, IDisposable
    {
        readonly Dictionary<string, byte[]> inMemory = new Dictionary<string, byte[]>();
        readonly HashSet<string> tempFiles = new HashSet<string>();
        readonly IVirtualFileProvider virtualFileProvider;

        public InMemoryFileProvider(IVirtualFileProvider baseFileProvider) : base(baseFileProvider.RootPath)
        {
            virtualFileProvider = baseFileProvider;
        }

        public void Register(string url, string content)
        {
            inMemory[url] = Encoding.Default.GetBytes(content);

            // The effect system assumes there is a /path - doesn't do a FileExists check first :/
            var path = GetTempFileName();
            tempFiles.Add(path);
            File.WriteAllText(path, content);
            inMemory[$"{url}/path"] = Encoding.Default.GetBytes(path);
        }

        // Don't use Path.GetTempFileName() where we can run into overflow issue (had it during development)
        // https://stackoverflow.com/questions/18350699/system-io-ioexception-the-file-exists-when-using-system-io-path-gettempfilena
        private string GetTempFileName()
        {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        }

        public new void Dispose()
        {
            try
            {
                foreach (var f in tempFiles)
                    File.Delete(f);
            }
            catch (Exception)
            {
                // Ignore
            }
            base.Dispose();
        }

        public override bool FileExists(string url)
        {
            if (inMemory.ContainsKey(url))
                return true;

            return virtualFileProvider.FileExists(url);
        }

        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        {
            if (inMemory.TryGetValue(url, out var bytes))
            {
                return new MemoryStream(bytes);
            }

            return virtualFileProvider.OpenStream(url, mode, access, share, streamFlags);
        }
    }
}
