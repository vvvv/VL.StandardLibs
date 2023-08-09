using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using VL.Core.CompilerServices;
using VL.Lang;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.Core
{
    /// <summary>
    /// Non-generic interface implemented by VL emitted classes and records.
    /// </summary>
    public interface IVLObject
    {
        /// <summary>
        /// The service registry which was current when this instance was created.
        /// </summary>
        [Obsolete("Use AppHost.Services")]
        ServiceRegistry Services => AppHost.Services;

        /// <summary>
        /// The app host which created this instance.
        /// </summary>
        AppHost AppHost { get; }

        /// <summary>
        /// The context in which this instance was created.
        /// </summary>
        NodeContext Context { get; }

        /// <summary>
        /// The type of the object.
        /// </summary>
        IVLTypeInfo Type => AppHost.TypeRegistry.GetTypeInfo(GetType());

        /// <summary>
        /// The unique identity of the object. Gets preserved for immutable types.
        /// </summary>
        uint Identity { get; }

        IVLObject With(IReadOnlyDictionary<string, object> values);
    }

#nullable enable
    /// <summary>
    /// Interface to interact with the VL runtime.
    /// </summary>
    public interface IVLRuntime
    {
        public static IVLRuntime? Current => AppHost.Current.Services.GetService<IVLRuntime>();

        /// <summary>
        /// Whether or not VL is in a running state. If not calls into its object graph are not allowed.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Report an exception to the runtime. If possible the responsible nodes will be colored pink by the patch editor.
        /// </summary>
        /// <param name="exception">The exception to report.</param>
        void ReportException(Exception exception);

        /// <summary>
        /// Add a message for one frame. 
        /// </summary>
        void AddMessage(Message message);

        /// <summary>
        /// Add a message for one frame. 
        /// </summary>
        void AddMessage(UniqueId elementId, string message, MessageSeverity severity = MessageSeverity.Warning)
            => AddMessage(new Message(elementId, severity, message, source: MessageSource.Runtime));

        /// <summary>
        /// Add a persistent message. Use the returned disposable to remove the message.
        /// </summary>
        IDisposable AddPersistentMessage(Message message);

        /// <summary>
        /// Toggle message on and off. Note: you are responsible of turning the message off again! 
        /// </summary>
        // TODO: This is not a good API design. The method should return a disposable which turns the message off.
        // With this approach the user needs to hold on to the message as well as the runtime host.
        [Obsolete("Use AddPersistentMessage")]
        void TogglePersistentUserRuntimeMessage(Message message, bool on);
    }
#nullable restore

    /// <summary>
    /// Interface to create VL objects.
    /// </summary>
    public interface IVLFactory
    {
        /// <summary>
        /// The app host associated with this factory.
        /// </summary>
        AppHost AppHost { get; }

        /// <summary>
        /// Lookup a type by name. The name will be parsed based on the usual VL type annotation rules.
        /// For example "Integer32" or "Spread [Collections] &lt;Float32&gt;".
        /// </summary>
        /// <param name="name">The name of the type.</param>
        /// <returns>The first type which matches the name or null.</returns>
        Type GetTypeByName(string name);

        /// <summary>
        /// Fetch the VL type info for a given CLR type.
        /// </summary>
        /// <param name="type">The CLR type to fetch the VL type info for.</param>
        /// <returns>The VL type info wrapping the given CLR type.</returns>
        IVLTypeInfo GetTypeInfo(Type type);

        /// <summary>
        /// Creates a new instance of the given type using the VL generated constructor.
        /// </summary>
        /// <param name="type">The type to create a new instance of.</param>
        /// <param name="nodeContext">The context to use when creating the instance.</param>
        /// <returns>The newly created instance or null if the type is not known to VL.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        object CreateInstance(Type type, NodeContext nodeContext);

        /// <summary>
        /// Returns the default value of the given type as defined by VL through the CreateDefault operations.
        /// </summary>
        /// <param name="type">The type to return the default value of.</param>
        /// <returns>The default value of the given type as defined by VL or null if the type is not known to VL or no default has been defined.</returns>
        [Obsolete("Please use AppHost.GetDefaultValue")]
        object GetDefaultValue(Type type);

        /// <summary>
        /// Registers a factory function which gets invoked when a certain service of type <paramref name="serviceType"/> is requested for
        /// a specific value of type <paramref name="forType"/>. The factory function must return a service of the specified <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="forType">The type of the value for which a service will be requested.</param>
        /// <param name="serviceType">The type of the service.</param>
        /// <param name="serviceFactory">The factory function creating such a service.</param>
        void RegisterService(Type forType, Type serviceType, Func<object, object> serviceFactory);

        /// <summary>
        /// Retrieves the factory function which will create the service of type <paramref name="serviceType"/> for the given <paramref name="forType"/>.
        /// </summary>
        /// <param name="forType">The type of the value for which a service is requested.</param>
        /// <param name="serviceType">The type of the service.</param>
        /// <returns>The factory function creating the service or null.</returns>
        Func<object, object> GetServiceFactory(Type forType, Type serviceType);
    }

    /// <summary>
    /// Interface to interact with VL properties.
    /// </summary>
    public interface IVLPropertyInfo
    {
        /// <summary>
        /// The type which declared this property.
        /// </summary>
        IVLTypeInfo DeclaringType { get; }

        /// <summary>
        /// The id of the property.
        /// </summary>
        uint Id { get; }

        /// <summary>
        /// The name of the property. Special characters are escaped. 
        /// </summary>
        [Obsolete("Got replaced by NameForTextualCode. Also consider using OriginalName, which can contain spaces.")]
        string Name { get; }

        /// <summary>
        /// The name of the property. Special characters are escaped. Used in serialization and C#.
        /// </summary>
        string NameForTextualCode { get; }

        /// <summary>
        /// The original property name. All characters are allowed.
        /// </summary>
        string OriginalName { get; }

        /// <summary>
        /// The type of the property.
        /// </summary>
        IVLTypeInfo Type { get; }

        /// <summary>
        /// Whether or not the property is system generated.
        /// </summary>
        bool IsManaged { get; }

        /// <summary>
        /// Gets the property value of the given instance.
        /// </summary>
        /// <param name="instance">The instance to get the value from.</param>
        /// <returns>The value of the property.</returns>
        object GetValue(IVLObject instance);

        /// <summary>
        /// Sets the property value of the given instance.
        /// </summary>
        /// <param name="instance">The instance to set the value on.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The instance with the newly set value.</returns>
        IVLObject WithValue(IVLObject instance, object value);

        IEnumerable<TAttribute> GetAttributes<TAttribute>() where TAttribute : Attribute;
    }

    public record struct ObjectGraphNode(string Path, object Value, Type Type);

    public static class VLFactoryExtensions
    {
        /// <summary>
        /// Returns the VL defined default value for the given type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type for which to return the VL defined default value.</typeparam>
        /// <param name="factory">The factory to use to create the default value.</param>
        /// <returns>The VL defined default value of the given type <typeparamref name="T"/>.</returns>
        [Obsolete("Please use TypeUtils.Default")]
        public static T GetDefaultValue<T>(this IVLFactory factory) => factory.AppHost.GetDefaultValue<T>();

        /// <summary>
        /// Tries to create an instance of the given type <typeparamref name="T"/> using the VL generated constructor.
        /// Returns true in case an instance was created.
        /// </summary>
        /// <typeparam name="T">The type of which to create an instance for.</typeparam>
        /// <param name="factory">The factory which will create the instance.</param>
        /// <param name="defaultValue">The default value to use in case an instance couldn't be created.</param>
        /// <param name="nodeContext">The context in which the new instance will be created.</param>
        /// <param name="instance">The newly created instance or the given default value.</param>
        /// <returns>True in case a new instance was created.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        public static bool TryCreateInstance<T>(this IVLFactory factory, T defaultValue, NodeContext nodeContext, out T instance)
        {
            var value = factory.AppHost.CreateInstance(typeof(T), nodeContext);
            if (value is T)
            {
                instance = (T)value;
                return true;
            }
            instance = defaultValue;
            return false;
        }

        /// <summary>
        /// Creates a new instance of the given type using the VL generated constructor.
        /// </summary>
        /// <param name="factory">The VL factory which will create the instance.</param>
        /// <param name="type">The type to create a new instance of.</param>
        /// <returns>The newly created instance or null if the type is not known to VL.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        public static object CreateInstance(this IVLFactory factory, Type type)
            => CreateInstance(factory, type, default);

        /// <summary>
        /// Creates a new instance of the given type using the VL generated constructor.
        /// </summary>
        /// <param name="factory">The VL factory which will create the instance.</param>
        /// <param name="type">The type to create a new instance of.</param>
        /// <param name="rootId">The node id to use for the context in which the instance will be created.</param>
        /// <returns>The newly created instance or null if the type is not known to VL.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        public static object CreateInstance(this IVLFactory factory, Type type, UniqueId rootId)
            => factory.AppHost.CreateInstance(type, NodeContext.Create(rootId));

        /// <summary>
        /// Creates a new instance of the given type using the VL generated constructor.
        /// </summary>
        /// <param name="factory">The VL factory which will create the instance.</param>
        /// <param name="type">The type to create a new instance of.</param>
        /// <returns>The newly created instance or null if the type is not known to VL.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        public static object CreateInstance(this IVLFactory factory, IVLTypeInfo type) 
            => CreateInstance(factory, type, default);

        /// <summary>
        /// Creates a new instance of the given type using the VL generated constructor.
        /// </summary>
        /// <param name="factory">The VL factory which will create the instance.</param>
        /// <param name="type">The type to create a new instance of.</param>
        /// <param name="rootId">The node id to use for the context in which the instance will be created.</param>
        /// <returns>The newly created instance or null if the type is not known to VL.</returns>
        [Obsolete("Please use AppHost.CreateInstance")]
        public static object CreateInstance(this IVLFactory factory, IVLTypeInfo type, UniqueId rootId) 
            => factory.CreateInstance(type.ClrType, NodeContext.Create(rootId));

        /// <summary>
        /// Registers a factory function which gets invoked when a service of type <typeparamref name="TService"/> is requested for
        /// a value of type <typeparamref name="TForType"/>.
        /// </summary>
        /// <typeparam name="TForType">The type of the value for which a service will be requested.</typeparam>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory to register the service in.</param>
        /// <param name="create">The factory function to invoke when such a service is requested.</param>
        /// <returns>The factory with the registered service.</returns>
        public static IVLFactory RegisterService<TForType, TService>(this IVLFactory factory, Func<TForType, TService> create)
        {
            factory.RegisterService(typeof(TForType), typeof(TService), v => create(v is TForType x ? x : default));
            return factory;
        }

        /// <summary>
        /// Registers a factory function which gets invoked when a service of type <typeparamref name="TService"/> is requested.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory to register the service in.</param>
        /// <param name="service">The service to register.</param>
        /// <returns>The factory with the registered service.</returns>
        public static IVLFactory RegisterService<TService>(this IVLFactory factory, TService service)
        {
            factory.RegisterService(typeof(Unit), typeof(TService), _ => service);
            return factory;
        }

        /// <summary>
        /// Creates a service of the given type <typeparamref name="TService"/> for the given value.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory which creates the service.</param>
        /// <param name="value">The value to create the service for.</param>
        /// <returns>The service or null in case no service of type <typeparamref name="TService"/> was registered for the type of the value.</returns>
        public static TService CreateService<TService>(this IVLFactory factory, object value)
        {
            TService service;
            factory.TryCreateService(value, value?.GetType(), default(TService), out service);
            return service;
        }

        /// <summary>
        /// Gets the service type <typeparamref name="TService"/> or null.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory which contains the service.</param>
        /// <returns>The service or null in case no service of type <typeparamref name="TService"/> was registered.</returns>
        public static TService GetService<TService>(this IVLFactory factory) where TService : class
        {
            return factory.CreateService<TService>(Unit.Default) ?? factory.AppHost.Services.GetService<TService>();
        }

        /// <summary>
        /// Creates a service of the given type <typeparamref name="TService"/> for the given value. returns default if creating the service failed.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory which creates the service.</param>
        /// <param name="value">The value to create the service for.</param>
        /// <returns>The service or null in case no service of type <typeparamref name="TService"/> was registered for the type of the value.</returns>
        public static TService CreateServiceSafe<TService>(this IVLFactory factory, object value)
        {
            try
            {
                return factory.CreateService<TService>(value);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <summary>
        /// Tries to create a service of the given type <typeparamref name="TService"/> for the given value.
        /// Returns true if such a service was found for the given value, otherwise the given default service will be used.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory which creates the service.</param>
        /// <param name="value">The value which will be passed to the service constructor.</param>
        /// <param name="forType">The type for which to create the service for. If null the type of the value will be used.</param>
        /// <param name="defaultService">The default service to use in case no service of that type was registered for the type of the value.</param>
        /// <param name="service">The registered service or the given default service.</param>
        /// <returns>True in case a service was found and created.</returns>
        public static bool TryCreateService<TService>(this IVLFactory factory, object value, Type forType, TService defaultService, out TService service)
        {
            var type = forType ?? value?.GetType();
            if (type != null)
            {
                var serviceFactory = factory.GetServiceFactory(type, typeof(TService));
                if (serviceFactory != null)
                {
                    var obj = serviceFactory(value);
                    if (obj is TService s)
                    {
                        service = s;
                        return true;
                    }
                }
            }

            service = defaultService;
            return false;
        }

        public static bool HasService<TValue, TService>(this IVLFactory factory) => factory.GetServiceFactory(typeof(TValue), typeof(TService)) != null;
        public static bool HasService<TService>(this IVLFactory factory, object obj) => factory.GetServiceFactory(obj.GetType(), typeof(TService)) != null;
    }

    public static class VLObjectExtensions
    {
        sealed class DefaultImpl : IVLObject
        {
            public AppHost AppHost => AppHost.CurrentOrGlobal;
            public NodeContext Context => NodeContext.Default;
            public uint Identity => 0;
            public IVLObject With(IReadOnlyDictionary<string, object> values) => this;
        }

        public static readonly IVLObject Default = new DefaultImpl();

        static readonly Regex FValueIndexerRegex = new Regex(@"(.+)\[([0-9]+)\]$", RegexOptions.Compiled);
        static readonly Regex FStringIndexerRegex = new Regex(@"(.+)\[""(.*?)""\]$", RegexOptions.Compiled);

        /// <summary>
        /// Tries to retrieve the path from the instance to the descendant.
        /// </summary>
        /// <remarks>
        /// The function does a depth first search into the object tree given by the instance. 
        /// It will traverse all user defined properties which are either a VL object or a collection of VL objects.
        /// The supported collection types are spreads and dictionaries.
        /// </remarks>
        /// <param name="instance">The instance from where the search shall start.</param>
        /// <param name="descendant">The descendant to look for.</param>
        /// <param name="path">The path from the instance to the descendant.</param>
        /// <returns>True if the descendant was found.</returns>
        public static bool TryGetPath(this IVLObject instance, IVLObject descendant, out string path)
        {
            if (instance.Identity == descendant.Identity)
            {
                path = "";
                return true;
            }

            foreach (var property in instance.Type.Properties)
            {
                var value = property.GetValue(instance);
                if (value is IVLObject child)
                {
                    if (child.Identity == descendant.Identity)
                    {
                        path = property.OriginalName;
                        return true;
                    }
                    else if (child.TryGetPath(descendant, out var subPath))
                    {

                        path = $"{property.OriginalName}.{subPath}";
                        return true;
                    }
                }
                else if (value is ISpread children)
                {
                    var count = children.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (children.GetItem(i) is IVLObject obj)
                        {
                            if (obj.Identity == descendant.Identity)
                            {
                                path = $"{property.OriginalName}[{i}]";
                                return true;
                            }
                            else if (obj.TryGetPath(descendant, out var subPath))
                            {
                                path = $"{property.OriginalName}[{i}].{subPath}";
                                return true;
                            }
                        }
                    }
                }
                else if (value is IDictionary dict)
                {
                    var enumerator = dict.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;
                        if (entry.Value is IVLObject obj)
                        {
                            if (obj.Identity == descendant.Identity)
                            {
                                path = $"{property.OriginalName}[\"{entry.Key}\"]";
                                return true;
                            }
                            else if (obj.TryGetPath(descendant, out string subPath))
                            {
                                path = $"{property.OriginalName}[\"{entry.Key}\"].{subPath}";
                                return true;
                            }
                        }
                    }
                }
            }

            path = null;
            return false;
        }

        public interface ICrawlObjectGraphFilter
        {
            public void Reset() { }
            bool Include(string path, string localID, object value, int depth) => true;
            public bool IndexingCountsAsHop => false;
            public bool CrawlVLObjects => true;
            public bool CrawlAllProperties => false;
            public bool IndexIntoSpreads => true;
            public bool IndexIntoDictionaries => true;
            public bool CrawlOnTypeLevel => false;
            public bool AlsoCollectNulls => false;

            // missing still:
            // crawl any collection
            // crawl any object
        }

        public record class DefaultCrawlObjectGraphFilter(int MaxCount = 1000) : ICrawlObjectGraphFilter
        {
            int Count = 0;

            public void Reset()
            {
                Count = 0;
            }

            public bool Include(string path, string localID, object value, int depth)
            {
                Count++;
                return Count <= MaxCount;
            }
        }

        public static Spread<ObjectGraphNode> CrawlObjectGraph(object instance, string rootPath, ICrawlObjectGraphFilter filter, bool includeRoot, Type type = default)
        {
            if (instance is null)
                return Spread<ObjectGraphNode>.Empty;
            if (type is null)
                type = instance.GetType();
            else
                if (!instance.GetType().IsAssignableTo(type))
                    throw new ArgumentException($"{instance.GetType()} is not of specified type {type}");
            if (filter == null)
                filter = new DefaultCrawlObjectGraphFilter();
            filter.Reset();
            var collection = new SpreadBuilder<ObjectGraphNode>();
            if (includeRoot)
                collection.Add(new ObjectGraphNode(rootPath, instance, type));
            CollectChildPaths(instance, rootPath, filter, collection, -1, type);
            return collection.ToSpread();
        }

        static void CollectChildPaths(object value, string pathOfParent, ICrawlObjectGraphFilter filter, SpreadBuilder<ObjectGraphNode> collection, int parentDepth, Type type)
        {
            if (filter.CrawlOnTypeLevel)
            {
                if (type.IsAssignableTo(typeof(IVLObject)))
                {
                    var child = (IVLObject)value;
                    if (filter.CrawlVLObjects)
                        CollectChildPaths(child, pathOfParent, filter, collection, parentDepth + 1, type);
                }
                else if (type.IsAssignableTo(typeof(ISpread)))
                {
                    var spread = (ISpread)value;
                    if (filter.IndexIntoSpreads)
                        CollectChildPaths(spread, pathOfParent, filter, collection, filter.IndexingCountsAsHop ? parentDepth + 1 : parentDepth, type);
                }
                else if (type.IsAssignableTo(typeof(IDictionary)))
                {
                    var dict = (IDictionary)value;
                    if (filter.IndexIntoDictionaries)
                        CollectChildPaths(dict, pathOfParent, filter, collection, filter.IndexingCountsAsHop ? parentDepth + 1 : parentDepth, type);
                }
            }
            else
            {
                if (value is null)
                    return;
                type = value.GetType();
                if (value is IVLObject child)
                {
                    if (filter.CrawlVLObjects)
                        CollectChildPaths(child, pathOfParent, filter, collection, parentDepth + 1, type);
                }
                else if (value is ISpread spread)
                {
                    if (filter.IndexIntoSpreads)
                        CollectChildPaths(spread, pathOfParent, filter, collection, filter.IndexingCountsAsHop ? parentDepth + 1 : parentDepth, type);
                }
                else if (value is IDictionary dict)
                {
                    if (filter.IndexIntoDictionaries)
                        CollectChildPaths(dict, pathOfParent, filter, collection, filter.IndexingCountsAsHop ? parentDepth + 1 : parentDepth, type);
                }
            }
        }
        
        static void CollectChildPaths(IVLObject instance, string pathOfParent, ICrawlObjectGraphFilter filter, SpreadBuilder<ObjectGraphNode> collection, int depth, Type type)
        {
            var typeInfo = filter.CrawlOnTypeLevel ? 
                instance.AppHost.TypeRegistry.GetTypeInfo(type) : 
                instance.Type;

            var properties = filter.CrawlAllProperties ? typeInfo.AllProperties : typeInfo.Properties;

            foreach (var property in properties)
            {
                var value = property.GetValue(instance);
                var localID = property.OriginalName;
                var path = string.IsNullOrWhiteSpace(pathOfParent) ? localID : $"{pathOfParent}.{localID}";
                if ((filter.AlsoCollectNulls || value is not null) && filter.Include(path, localID, value, depth))
                {
                    var childtype = property.Type.ClrType; // let's use the type of the property, not the type of the object. Embracing super types.
                    collection.Add(new ObjectGraphNode(path, value, childtype));
                    CollectChildPaths(value, path, filter, collection, depth, childtype);
                }
            }
        }

        static void CollectChildPaths(ISpread spread, string pathOfParent, ICrawlObjectGraphFilter filter, SpreadBuilder<ObjectGraphNode> collection, int depth, Type type)
        {
            var count = spread.Count;
            for (int i = 0; i < count; i++)
            {
                var value = spread.GetItem(i);
                var localID = $"[{i}]";
                var path = $"{pathOfParent}{localID}";
                if ((filter.AlsoCollectNulls || value is not null) && filter.Include(path, localID, value, depth))
                {
                    var childtype = spread.ElementType; // let's use the element type of the spread, not the type of the object. Embracing super types.
                    collection.Add(new ObjectGraphNode(path, value, childtype));
                    CollectChildPaths(value, path, filter, collection, depth, childtype);
                }
            }
        }

        static void CollectChildPaths(IDictionary dict, string pathOfParent, ICrawlObjectGraphFilter filter, SpreadBuilder<ObjectGraphNode> collection, int depth, Type type)
        {
            var enumerator = dict.GetEnumerator();
            var dictType = dict.GetType();
            var valueType = dictType.IsConstructedGenericType && dictType.GenericTypeArguments.Length == 2 ? dictType.GenericTypeArguments[1] : typeof(object);
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Entry;
                var value = entry.Value;
                var localID = $"[\"{entry.Key}\"]";
                var path = $"{pathOfParent}{localID}";
                if ((filter.AlsoCollectNulls || value is not null) && filter.Include(path, localID, value, depth))
                {
                    var childtype = valueType; // let's use the type of the value collection, not the type of the object. Embracing super types.
                    collection.Add(new ObjectGraphNode(path, value, childtype));
                    CollectChildPaths(value, path, filter, collection, depth, childtype);
                }
            }
        }

        /// <summary>
        /// Tries to retrieve the value from the given property.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="instance">The instance to retrieve the value from.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value to use in case retrieval failed.</param>
        /// <param name="value">The returned values.</param>
        /// <returns>True if the retrieval succeeded.</returns>
        public static bool TryGetValue<T>(this IVLObject instance, string name, T defaultValue, out T value)
        {
            var property = instance.Type.GetProperty(name);
            if (property != null)
            {
                var v = property.GetValue(instance);
                if (v is T)
                {
                    value = (T)v;
                    return true;
                }
            }
            value = defaultValue;
            return false;
        }

        public static bool TryGetValueIncludingAllFields<T>(this IVLObject instance, uint id, T defaultValue, out T value)
        {
            var property = instance.Type.AllProperties.FirstOrDefault(p => p.Id == id);
            if (property != null)
            {
                var v = property.GetValue(instance);
                if (v is T)
                {
                    value = (T)v;
                    return true;
                }
            }
            value = defaultValue;
            return false;
        }


        /// <summary>
        /// Tries to set the value of the given property and returns a new instance (if it is a record) with the set value.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <typeparam name="TValue">The type of the value to set.</typeparam>
        /// <param name="instance">The instance to set the property value on.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The new instance (if it is a record) with the set value.</returns>
        public static TInstance WithValue<TInstance, TValue>(this TInstance instance, string name, TValue value)
            where TInstance : class, IVLObject
        {
            var property = instance.Type.GetProperty(name);
            if (property != null)
                return (property.WithValue(instance, value) as TInstance) ?? instance;
            return instance;
        }

        /// <summary>
        /// Tries to retrieve the value from the given path. The path is a dot separated string of property names.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="instance">The root instance to start the lookup from.</param>
        /// <param name="path">A dot separated string of property names. Spreaded properties can be indexed using [N] for example "MySpread[0]" retrieves the first value in MySpread.</param>
        /// <param name="defaultValue">The default value to use in case the lookup failed.</param>
        /// <param name="value">The returned value.</param>
        /// <returns>True if the lookup succeeded.</returns>
        public static bool TryGetValueByPath<T>(this IVLObject instance, string path, T defaultValue, out T value)
        {
            var spine = instance.GetSpine(path);
            var entry = spine.Pop();
            var leaf = entry.Value;
            return TryGetValueByExpression(leaf, entry.Key, defaultValue, out value);
        }

        /// <summary>
        /// Tries to set the value of the given path. The path is a dot separated string of property names.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <typeparam name="TValue">The expected type of the value.</typeparam>
        /// <param name="instance">The root instance to start the lookup from.</param>
        /// <param name="path">A dot separated string of property names. Spreaded properties can be indexed using [N] for example "MySpread[0]" sets the first value in MySpread.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The new root instance (if it is a record) with the updated spine.</returns>
        public static TInstance WithValueByPath<TInstance, TValue>(this TInstance instance, string path, TValue value)
            where TInstance : class, IVLObject
        {
            return ((IVLObject)instance).WithValueByPath(path, value) as TInstance;
        }

        /// <summary>
        /// Whether or not the type is supported by <see cref="TryReplaceDescendant"/>.
        /// </summary>
        public static bool IsSupportedCollectionType(IVLTypeInfo type)
        {
            var clrType = type?.ClrType;
            if (clrType is null)
                return false;
            var arguments = clrType.GenericTypeArguments;
            if (typeof(ISpread).IsAssignableFrom(clrType) && arguments.Length == 1 && typeof(IVLObject).IsAssignableFrom(arguments[0]))
                return true;
            if (typeof(IDictionary).IsAssignableFrom(clrType) && arguments.Length == 2 && typeof(IVLObject).IsAssignableFrom(arguments[1]))
                return true;
            return false;
        }

        /// <summary>
        /// Traverses into the object graph of <paramref name="instance"/> and if it can find a descendant with the same <see cref="IVLObject.Identity"/>
        /// as the given <paramref name="descendant"/> replaces it and outputs a new <paramref name="updatedInstance"/>.
        /// </summary>
        /// <remarks>
        /// Only user defined properties will be traversed. If a property holds many children it must be of type <see cref="ISpread"/> or <see cref="IDictionary"/>. Other collection types will not be looked at.
        /// </remarks>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <typeparam name="TDescendant">The type of the descendant.</typeparam>
        /// <param name="instance">The instance to traverse into.</param>
        /// <param name="descendant">The new descendant.</param>
        /// <param name="updatedInstance">The updated instance with the descendant replaced.</param>
        /// <returns>Returns true if a descendant with the same <see cref="IVLObject.Identity"/> as the given one was found and replaced.</returns>
        public static bool TryReplaceDescendant<TInstance, TDescendant>(this TInstance instance, TDescendant descendant, out TInstance updatedInstance)
            where TInstance : class, IVLObject
            where TDescendant : class, IVLObject
        {
            if (instance.Identity == descendant.Identity)
            {
                updatedInstance = descendant as TInstance;
                return true;
            }
            foreach (var property in instance.Type.Properties)
            {
                var value = property.GetValue(instance);
                if (value is IVLObject child)
                {
                    if (child.TryReplaceDescendant(descendant, out var newChild))
                    {
                        if (newChild != child)
                            updatedInstance = property.WithValue(instance, newChild) as TInstance;
                        else
                            updatedInstance = instance;
                        return true;
                    }
                }
                else if (value is ISpread children)
                {
                    var count = children.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (children.GetItem(i) is IVLObject obj)
                        {
                            if (obj.TryReplaceDescendant(descendant, out var newObj))
                            {
                                if (newObj != obj)
                                {
                                    var updatedChildren = children.SetItem(i, newObj);
                                    updatedInstance = property.WithValue(instance, updatedChildren) as TInstance;
                                }
                                else
                                    updatedInstance = instance;
                                return true;
                            }
                        }
                    }
                }
                else if (value is IDictionary dict)
                {
                    var enumerator = dict.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;
                        if (entry.Value is IVLObject obj)
                        {
                            if (obj.TryReplaceDescendant(descendant, out var newObj))
                            {
                                if (newObj != obj)
                                {
                                    var updatedDict = SetItem(dict, entry.Key, newObj);
                                    updatedInstance = property.WithValue(instance, updatedDict) as TInstance;
                                }
                                else
                                    updatedInstance = instance;
                                return true;
                            }
                        }
                    }
                }
            }
            updatedInstance = instance;
            return false;
        }

        static IVLObject WithValueByPath<T>(this IVLObject instance, string path, T value)
        {
            var spine = instance.GetSpine(path);

            // Update the leaf with the value
            var entry = spine.Pop();
            var leaf = entry.Value;

            var updatedInstance = leaf.WithValueFromExpression(entry.Key, value);

            // Update the VL objects along the spine
            while (spine.Count > 0)
            {
                entry = spine.Pop();
                updatedInstance = entry.Value.WithValueFromExpression(entry.Key, updatedInstance);
            }

            return updatedInstance;
        }

        static Stack<KeyValuePair<string, IVLObject>> GetSpine(this IVLObject instance, string path)
        {
            var stack = new Stack<KeyValuePair<string, IVLObject>>();
            var p = path.Split('.');
            var current = instance;
            for (int i = 0; i < p.Length; i++)
            {
                stack.Push(new KeyValuePair<string, IVLObject>(p[i], current));
                if (TryGetValueByExpression(current, p[i], default(IVLObject), out var next))
                {
                    current = next;
                }
                else
                {
                    break;
                }
            }
            return stack;
        }

        static bool TryGetValueByExpression<T>(this IVLObject instance, string expression, T defaultValue, out T value)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                value = defaultValue;
                return false;
            }

            if (TryParseValueIndexer(expression, out var propertyName, out var index))
            {
                if (TryGetValueByExpression(instance, propertyName, default(ISpread), out ISpread spread) && spread.Count > index)
                {
                    var item = spread.GetItem(index);
                    if (item is T)
                    {
                        value = (T)item;
                        return true;
                    }
                }
                value = defaultValue;
                return false;
            }
            else if (TryParseStringIndexer(expression, out propertyName, out var key))
            {
                if (TryGetValueByExpression(instance, propertyName, default(IDictionary), out IDictionary dict))
                {
                    if (dict.Contains(key))
                    {
                        var item = dict[key];
                        if (item is T)
                        {
                            value = (T)item;
                            return true;
                        }
                    }
                }
                value = defaultValue;
                return false;
            }
            else
            {
                return instance.TryGetValue(propertyName, defaultValue, out value);
            }
        }

        static IVLObject WithValueFromExpression<T>(this IVLObject instance, string expression, T value)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return instance;

            if (TryParseValueIndexer(expression, out var propertyName, out var index))
            {
                if (instance.TryGetValueByExpression(propertyName, null, out ISpread spread))
                    return instance.WithValueFromExpression(propertyName, spread.SetItem(index, value));
                return instance;
            }
            if (TryParseStringIndexer(expression, out propertyName, out var key))
            {
                if (instance.TryGetValueByExpression(propertyName, null, out IDictionary dict))
                    return instance.WithValueFromExpression(propertyName, SetItem(dict, key, value));
                return instance;
            }

            return instance.WithValue(propertyName, value);
        }

        static IDictionary SetItem(IDictionary dict, object key, object value)
        {
            var dictType = dict.GetType();
            var toBuilderMethod = dictType.GetMethod(nameof(ImmutableDictionary<string, object>.ToBuilder));
            if (toBuilderMethod != null)
            {
                var builder = toBuilderMethod.Invoke(dict, null) as IDictionary;
                builder[key] = value;
                var toImmutableMethod = builder.GetType().GetMethod(nameof(ImmutableDictionary<string, object>.Builder.ToImmutable));
                return toImmutableMethod.Invoke(builder, null) as IDictionary;
            }
            else
            {
                dict[key] = value;
                return dict;
            }
        }

        // Assume expression.Length > 0
        static bool TryParseValueIndexer(string expression, out string propertyName, out int index)
        {
            if (expression[expression.Length - 1] == ']')
            {
                var match = FValueIndexerRegex.Match(expression);
                if (match.Success)
                {
                    propertyName = match.Groups[1].Value;
                    if (int.TryParse(match.Groups[2].Value, out index))
                        return true;
                }
            }
            propertyName = expression;
            index = -1;
            return false;
        }

        // Assume expression.Length > 0
        static bool TryParseStringIndexer(string expression, out string propertyName, out string key)
        {
            if (expression[expression.Length - 1] == ']')
            {
                var match = FStringIndexerRegex.Match(expression);
                if (match.Success)
                {
                    propertyName = match.Groups[1].Value;
                    key = match.Groups[2].Value;
                    return true;
                }
            }
            propertyName = expression;
            key = null;
            return false;
        }
    }

    public static class VLPropertyInfoExtensions
    {
        class DefaultImpl : IVLPropertyInfo
        {
            public IVLTypeInfo DeclaringType => VLObjectExtensions.Default.Type;
            public string Name => "DUMMY"; 
            public string NameForTextualCode => "DUMMY";
            public string OriginalName => Name;
            public uint Id => 0;
            public bool IsManaged => false;
            public IVLTypeInfo Type => VLObjectExtensions.Default.Type;
            public object DefaultValue => null;
            public object GetValue(IVLObject instance) => null;
            public IVLObject WithValue(IVLObject instance, object value) => instance;
            public IEnumerable<TAttribute> GetAttributes<TAttribute>() where TAttribute : Attribute => Enumerable.Empty<TAttribute>();
        }

        public static IVLPropertyInfo Default = new DefaultImpl();

        /// <summary>
        /// Tries to get the property value of the given instance.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="property">The property to read.</param>
        /// <param name="instance">The instance to get the property value from.</param>
        /// <param name="defaultValue">The value to return in case retrieval failed.</param>
        /// <param name="value">The retrieved property value.</param>
        /// <returns>Whether or not the operation succeeded.</returns>
        public static bool TryGetValue<T>(this IVLPropertyInfo property, IVLObject instance, T defaultValue, out T value)
        {
            var obj = property.GetValue(instance);
            if (obj is T)
            {
                value = (T)obj;
                return true;
            }
            else
            {
                value = defaultValue;
                return false;
            }
        }

        /// <summary>
        /// Sets the property value of the given instance.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <typeparam name="TValue">The type of the value to set.</typeparam>
        /// <param name="property">The property to write.</param>
        /// <param name="instance">The instance to set the property value on.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The instance with the set value.</returns>
        public static TInstance WithValue<TInstance, TValue>(this IVLPropertyInfo property, TInstance instance, TValue value)
            where TInstance : class, IVLObject
        {
            return property.WithValue(instance, value) as TInstance;
        }
    }
}