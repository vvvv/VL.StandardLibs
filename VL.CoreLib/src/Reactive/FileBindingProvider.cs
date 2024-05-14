#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Xml.Linq;
using VL.Core;
using VL.Core.Reactive;

namespace VL.Lib.Reactive
{
    // TODO: Add file watcher?
    // TODO: Different serialization formats? Have for example a look at Microsoft.Extensions.Configuration.Xml/Ini/Json how it's done.
    /// <summary>
    /// Allows to create a binding between a <see cref="IChannel{T}"/> and an element in a XML file.
    /// </summary>
    public class FileBindingProvider : IDisposable
    {
        private const string Author = nameof(FileBindingProvider);

        private readonly NodeContext nodeContext;
        private readonly SerializationService serialization;
        private readonly string filePath;
        private readonly List<FileBinding> bindings = new();

        public FileBindingProvider(NodeContext nodeContext, string filePath)
        {
            this.nodeContext = nodeContext;
            this.serialization = nodeContext.AppHost.SerializationService;
            this.filePath = filePath;
        }

        public void Dispose()
        {
            foreach (var binding in bindings)
                binding.Dispose();
            bindings.Clear();
        }

        public IBinding CreateBinding(IChannel channel, string key) => new FileBinding(this, channel, key);

        public void Load()
        {
            var document = File.Exists(filePath) ? XDocument.Load(filePath) : null;
            if (document?.Root is null)
                return;

            foreach (var binding in bindings)
            {
                if (TryLoad(document.Root, binding.key, binding.channel.ClrTypeOfValues, out var value))
                    binding.channel.SetObjectAndAuthor(value, Author);
            }
        }

        public void SaveAll()
        {
            var document = LoadOrCreateDocument();
            foreach (var binding in bindings)
                SaveToTree(document.Root!, binding.channel, binding.key);
            document.Save(filePath);
        }

        private XDocument LoadOrCreateDocument()
        {
            var document = File.Exists(filePath) ? XDocument.Load(filePath) : new XDocument();
            if (document.Root is null)
                document.Add(new XElement("configuration"));
            return document;
        }

        private void Save(IChannel channel, string key)
        {
            var document = LoadOrCreateDocument();
            SaveToTree(document.Root!, channel, key);
            document.Save(filePath);
        }

        private void SaveToTree(XElement root, IChannel channel, string key)
        {
            if (TrySerialize(channel.Object, channel.ClrTypeOfValues, out var result) && result != null)
            {
                var element = GetOrCreateElementForPath(root, key);
                element.Value = result;
            }
        }

        private static XElement GetOrCreateElementForPath(XElement container, string path)
        {
            var dotIndex = path.IndexOf('.');
            if (dotIndex < 0)
            {
                return GetOrCreateElementForName(container, path);
            }
            else
            {
                var subKey = path.Substring(0, dotIndex);
                var subContainer = GetOrCreateElementForName(container, subKey);
                return GetOrCreateElementForPath(subContainer, path.Substring(dotIndex + 1));
            }
        }

        private static XElement GetOrCreateElementForName(XElement container, string name)
        {
            var element = container.Element(name);
            if (element is null)
            {
                element = new XElement(name);
                container.Add(element);
            }
            return element;
        }

        private bool TryLoad(XElement container, string path, Type type, out object? value)
        {
            var element = FindElement(container, path);
            if (element is not null)
                return TryDeserialize(element.Value, type, out value);

            value = null;
            return false;
        }

        private static XElement? FindElement(XElement container, string path)
        {
            var dotIndex = path.IndexOf('.');
            if (dotIndex < 0)
            {
                return container.Element(path);
            }
            else
            {
                var subKey = path.Substring(0, dotIndex);
                var subContainer = FindElement(container, subKey);
                return subContainer?.Element(path.Substring(dotIndex + 1));
            }
        }

        private bool TrySerialize(object? value, Type type, out string? result)
        {
            var content = serialization.Serialize(nodeContext, value, type, includeDefaults: false, forceElement: false, pathsAreRelativeToDocument: false, out var errors);
            if (content is string s)
            {
                result = s;
                return true;
            }

            result = null;
            return false;
        }

        private bool TryDeserialize(string? content, Type type, out object? result)
        {
            result = serialization.Deserialize(nodeContext, content, type, pathsAreRelativeToDocument: false, out var errors);
            return errors.Count == 0;
        }

        private sealed class FileBinding : IBinding
        {
            private readonly FileBindingProvider provider;
            private IDisposable? subscription;

            public readonly IChannel channel;
            public readonly string key;

            public FileBinding(FileBindingProvider provider, IChannel channel, string key)
            {
                this.provider = provider;
                this.channel = channel;
                this.key = key;

                subscription = channel.ChannelOfObject.Subscribe(OnChannelChanged);

                provider.bindings.Add(this);
                channel.AddComponent(this);
            }

            public IModule? Module => null;

            public string Description => Path.GetFileName(provider.filePath);

            public BindingType BindingType => BindingType.SendAndReceive;

            public void Dispose()
            {
                var subscription = Interlocked.Exchange(ref this.subscription, null);
                if (subscription is null)
                    return;

                channel.RemoveComponent(this);
                provider.bindings.Remove(this);
                subscription.Dispose();
            }

            private void OnChannelChanged(object? value)
            {
                if (channel.LatestAuthor != Author)
                    provider.Save(channel, key);
            }
        }
    }
}
