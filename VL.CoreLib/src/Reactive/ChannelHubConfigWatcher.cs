using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using VL.Lib.Collections;
using VL.Lib.Mathematics;
using VL.Lib.Reactive;

namespace VL.Core.Reactive
{
    class ChannelHubConfigWatcher : IDisposable
    {
        readonly string filePath;
        readonly AppHost appHost;
        readonly IDisposable subscription;

        public Channel<PublicChannelDescription[]> Descriptions;

        public ChannelHubConfigWatcher(AppHost appHost, string filePath)
        {
            this.appHost = appHost;
            this.filePath = filePath;

            Descriptions = new Channel<PublicChannelDescription[]>();
            PushChannelBuildDescriptions();

            subscription = appHost.FileSystem.Watch(Path.GetDirectoryName(filePath), Path.GetFileName(filePath))
                .ObserveOn(Scheduler.CurrentThread)
                .Where(_ => DateTime.UtcNow > lastSave + TimeSpan.FromSeconds(10))
                .Do(_ => appHost.DefaultLogger.LogDebug($"{filePath} changed"))
                .Throttle(TimeSpan.FromSeconds(1))
                .Subscribe(_ => PushChannelBuildDescriptions());

            this.DisposeBy(appHost);
        }

        void PushChannelBuildDescriptions()
        {
            try
            {
                var c = GetChannelBuildDescriptions().ToArray();
                Descriptions.SetValueAndAuthor(c, author: "file");
                appHost.DefaultLogger.LogInformation($"recreated {c.Length} channels via {filePath}.");
            }
            catch (Exception)
            {
                appHost.DefaultLogger.LogWarning($"reading {filePath} failed.");
            }
        }

        IEnumerable<PublicChannelDescription> GetChannelBuildDescriptions()
        {
            using var stream = Task.Run(() => appHost.FileSystem.OpenReadOrNullAsync(filePath)).Result;
            if (stream is null)
                yield break;

            // Try to read as XML first
            List<PublicChannelDescription> result = null;
            try
            {
                var doc = XDocument.Load(stream);
                var channels = doc.Root?.Elements("Channel");
                if (channels != null)
                {
                    result = new List<PublicChannelDescription>();
                    foreach (var channel in channels)
                    {
                        var name = channel.Attribute("Name")?.Value ?? "";
                        var typeName = channel.Attribute("Type")?.Value ?? "";
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(typeName))
                            result.Add(new PublicChannelDescription(name, typeName));
                    }
                }
            }
            catch
            {
                // Fallback to legacy text format if XML parsing fails
            }

            if (result != null)
            {
                foreach (var item in result)
                    yield return item;
                yield break;
            }

            // Legacy: read as text file (Name:TypeName per line)
            using var reader = new StreamReader(stream, leaveOpen: true /* stream already in using block */);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                    continue;
                var parts = line.Split(':');
                if (parts.Length >= 2)
                    yield return new PublicChannelDescription(Name: parts[0], TypeName: parts[1]);
            }
        }

        public void Dispose()
        {
            subscription.Dispose();
            allwatchers.TryRemove(filePath, out var _);
        }

        static ConcurrentDictionary<string, ChannelHubConfigWatcher> allwatchers = new ConcurrentDictionary<string, ChannelHubConfigWatcher>();

        public static ChannelHubConfigWatcher GetWatcherForPath(AppHost appHost, string path)
        {
            return allwatchers.GetOrAdd(path, p => new ChannelHubConfigWatcher(appHost, p));
        }

        internal static string GetConfigFilePath(string p) => Path.Combine(p, "GlobalChannels.txt");

        internal static string GetConfigFilePath(AppHost app) => Path.Combine(app.AppBasePath_CanBeRemote, app.AppName + ".pc");

        internal static ChannelHubConfigWatcher FromApplicationBasePath(AppHost appHost)
        {
            var path = GetConfigFilePath(appHost);
            return GetWatcherForPath(appHost, path);
        }

        DateTime lastSave = DateTime.MinValue;

        // Only called by server, no need for virtual file system for now
        public void Save(IEnumerable<PublicChannelDescription> descriptions)
        {
            if (descriptions.SequenceEqual(Descriptions.Value))
                return;

            if (descriptions.Any())
            {
                XElement rootElement = new XElement("PublicChannels",
                    descriptions.Select(d => new XElement("Channel",
                        new XAttribute("Name", d.Name),
                        new XAttribute("Type", d.TypeName))));
                //Serialization.Serialize(NodeContext.CurrentRoot, descriptions.ToSpread());
                var document = new XDocument(rootElement);
                lastSave = DateTime.UtcNow;
                document.Save(filePath, SaveOptions.None);
                var c = descriptions.ToSpread();
                appHost.DefaultLogger.LogInformation($"saved {c.Count} channels to {filePath}.");
            }
            else
            {
                File.Delete(filePath);
                appHost.DefaultLogger.LogInformation($"deleted {filePath}.");
            }
        }
    }
}
