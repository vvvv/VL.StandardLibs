using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;

namespace VL.Core.Reactive
{
    class ChannelHubConfigWatcher : IDisposable
    {
        readonly string filePath;
        readonly AppHost appHost;
        readonly FileSystemWatcher watcher;

        public BehaviorSubject<ChannelBuildDescription[]> Descriptions;

        public ChannelHubConfigWatcher(AppHost appHost, string filePath)
        {
            this.appHost = appHost;
            this.filePath = filePath;
            watcher = new FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(filePath);
            watcher.Filter = Path.GetFileName(filePath);
            watcher.EnableRaisingEvents = true;

            Descriptions = new BehaviorSubject<ChannelBuildDescription[]>(GetChannelBuildDescriptions().ToArray());

            Observable.FromEventPattern<FileSystemEventArgs>(watcher, "Changed", scheduler: Scheduler.CurrentThread)
                .Do(_ => Console.WriteLine($"{filePath} changed"))
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ => PushChannelBuildDescriptions());

            this.DisposeBy(appHost);
        }

        void PushChannelBuildDescriptions()
        {
            try
            {
                Descriptions.OnNext(GetChannelBuildDescriptions().ToArray());
            }
            catch (Exception)
            {
                Console.WriteLine($"reading {filePath} failed.");
            }
        }

        IEnumerable<ChannelBuildDescription> GetChannelBuildDescriptions()
        {
            var lines = File.ReadLines(filePath).ToArray();
            foreach (var l in lines)
            {
                var _ = l.Split(':');
                var t = appHost.TypeRegistry.GetTypeByName(_[1]);
                yield return new ChannelBuildDescription(Name: _[0], _[1], appHost.TypeRegistry);
            }
        }

        public void Dispose()
        {
            watcher.Dispose();
            allwatchers.TryRemove(filePath, out var _);
        }

        static ConcurrentDictionary<string, ChannelHubConfigWatcher> allwatchers = new ConcurrentDictionary<string, ChannelHubConfigWatcher>();

        public static ChannelHubConfigWatcher GetWatcherForPath(string path)
        {
            return allwatchers.GetOrAdd(path, p => new ChannelHubConfigWatcher(AppHost.Global, p));
        }

        internal static string GetConfigFilePath(string p) => Path.Combine(p, "GlobalChannels.txt");

        internal static ChannelHubConfigWatcher FromApplicationBasePath(string p)
        {
            var path = GetConfigFilePath(p);
            if (!File.Exists(path))
                return null;
            return GetWatcherForPath(path);
        }
    }
}
