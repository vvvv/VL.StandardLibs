using Stride.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace VL.Stride.Core.IO
{
    class AggregateFileProvider : VirtualFileProviderBase
    {
        private readonly List<IVirtualFileProvider> virtualFileProviders = new List<IVirtualFileProvider>();

        public AggregateFileProvider(params IVirtualFileProvider[] virtualFileProviders) 
            : base(rootPath: null)
        {
            this.virtualFileProviders.AddRange(virtualFileProviders);
        }

        public void Add(IVirtualFileProvider virtualFileProvider)
        {
            lock (virtualFileProviders)
            {
                virtualFileProviders.Add(virtualFileProvider);
            }
        }

        public void Remove(IVirtualFileProvider virtualFileProvider)
        {
            lock (virtualFileProviders)
            {
                virtualFileProviders.Remove(virtualFileProvider);
            }
        }

        public override bool TryGetFileLocation(string path, out string filePath, out long start, out long end)
        {
            lock (virtualFileProviders)
            {
                foreach (var provider in virtualFileProviders)
                    if (provider.TryGetFileLocation(path, out filePath, out start, out end) && File.Exists(filePath))
                        return true;
            }

            return base.TryGetFileLocation(path, out filePath, out start, out end);
        }

        public override bool FileExists(string url)
        {
            lock (virtualFileProviders)
            {
                foreach (var provider in virtualFileProviders)
                    if (provider.FileExists(url))
                        return true;
                return false;
            }
        }

        public override bool DirectoryExists(string url)
        {
            lock (virtualFileProviders)
            {
                foreach (var provider in virtualFileProviders)
                    if (provider.DirectoryExists(url))
                        return true;
                return false;
            }
        }

        public override string[] ListFiles(string url, string searchPattern, VirtualSearchOption searchOption)
        {
            return ListFilesInternal(url, searchPattern, searchOption).ToArray();
        }

        IEnumerable<string> ListFilesInternal(string url, string searchPattern, VirtualSearchOption searchOption)
        {
            lock (virtualFileProviders)
            {
                foreach (var provider in virtualFileProviders)
                {
                    var result = new string[0];
                    try
                    {
                        result = provider.ListFiles(url, searchPattern, searchOption);
                    }
                    catch (Exception) { }

                    foreach (var filePath in result)
                    {
                        yield return filePath;
                    }
                }
            }
        }

        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        {
            if (mode == VirtualFileMode.Open)
            {
                lock (virtualFileProviders)
                {
                    foreach (var provider in virtualFileProviders)
                    {
                        if (provider.FileExists(url))
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                try
                                {
                                    return provider.OpenStream(url, mode, access, share, streamFlags);
                                }
                                catch (IOException)
                                {
                                    // We sometimes get file already in use exception. Let's try again.
                                    Thread.Sleep(10);
                                }
                            }
                        }
                    }

                    throw new FileNotFoundException(string.Format("Unable to find the file [{0}]", url));
                }
            }

            throw new ArgumentException("mode");
        }
    }
}
