// Copyright (c) Stride contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;
using Stride.Assets;
using Stride.Graphics;
using Stride.Navigation;
using Stride.Engine;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using Stride.Core.IO;
using Stride.Core.BuildEngine;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;

namespace VL.Stride.Assets
{
    /// <summary>
    /// A class that handles loading/unloading referenced resources for a game used in an editor.
    /// </summary>
    public sealed class RuntimeContentLoader : IDisposable
    {
        private Subject<ReloadingAsset> AssetBuilt = new Subject<ReloadingAsset>();
        private Subject<string> AssetRemoved = new Subject<string>();
        private CompositeDisposable subscriptions = new CompositeDisposable();
        public IObservable<Tuple<ReloadingAsset, object>> OnAssetBuilt { get; }
        private ConcurrentDictionary<string, AssetWrapperBase> allAssets = new ConcurrentDictionary<string, AssetWrapperBase>();
        public IReadOnlyCollection<KeyValuePair<string, AssetWrapperBase>> AllAssets => allAssets;
        public IObservable<string> OnAssetRemoved => AssetRemoved;
        private readonly ILogger logger;
        private readonly IRuntimeDatabase database;
        private RenderingMode currentRenderingMode;
        private ColorSpace currentColorSpace;
        private ObjectId currentNavigationGroupsHash;
        private int loadingAssetCount;

        public ContentManager ContentManager { get; }

#if DEBUG
        private ContentManagerStats debugStats;
        private bool enableReferenceLogging = true;
#endif

        /// <summary>
        /// RW lock for <see cref="assetsToReloadQueue"/> and <see cref="assetsToReloadMapping"/>.
        /// </summary>
        private readonly object assetsToReloadLock = new object();

        /// <summary>
        /// The assets currently waiting for a reload to be done.
        /// </summary>
        private readonly Queue<ReloadingAsset> assetsToReloadQueue = new Queue<ReloadingAsset>();

        /// <summary>
        /// Fast lookup to know what is in <see cref="assetsToReloadQueue"/>.
        /// </summary>
        private readonly Dictionary<AssetItem, ReloadingAsset> assetsToReloadMapping = new Dictionary<AssetItem, ReloadingAsset>();

        public RuntimeContentLoader(ILogger logger, Game game)
            : this(DispatcherService.Create(), RuntimeDatabase.Create(game), logger, game)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeContentLoader"/> class
        /// </summary>
        /// <param name="gameDispatcher">The dispatcher to the game thread.</param>
        /// <param name="logger">The logger to use to log operations.</param>
        /// <param name="asset">The asset associated with this instance.</param>
        /// <param name="game">The editor game associated with this instance.</param>
        public RuntimeContentLoader(IDispatcherService gameDispatcher, IRuntimeDatabase runtimDatabase, ILogger logger, Game game)
        {
            GameDispatcher = gameDispatcher ?? throw new ArgumentNullException(nameof(gameDispatcher));
            Game = game ?? throw new ArgumentNullException(nameof(game));
            this.logger = logger;
            database = runtimDatabase;
            UpdateGameSettings(game);

            //TODO: just a hack for now, merges the databases
            var defaultDatabase = game.Services.GetService<IDatabaseFileProviderService>().FileProvider;
            if (defaultDatabase != null)
            {
                MicrothreadLocalDatabases.AddToSharedGroup(defaultDatabase.ContentIndexMap.GetMergedIdMap()
                        .Select(idm => new OutputObject(new ObjectUrl(UrlType.Content, idm.Key), idm.Value)).ToDictionary(e => e.Url)); 
            }
            game.Services.RemoveService<IDatabaseFileProviderService>();
            game.Services.AddService(MicrothreadLocalDatabases.ProviderService);
            ContentManager = new ContentManager(Game.Services);

            OnAssetBuilt = AssetBuilt.SelectMany(SelectAssetEvent).ObserveOn(SynchronizationContext.Current);
            subscriptions.Add(OnAssetBuilt.Subscribe(HandleNewAsset));
            subscriptions.Add(OnAssetRemoved.ObserveOn(SynchronizationContext.Current).Subscribe(HandleAssetRemoved));

        }

        private void HandleNewAsset(Tuple<ReloadingAsset, object> obj)
        {
            var url = obj?.Item1?.AssetItem?.Location?.FullPath;

            if (url != null && obj.Item2 != null)
            {
                if (!allAssets.TryGetValue(url, out var assetWrapper))
                {
                    var awt = typeof(AssetWrapper<>);
                    Type[] typeArgs = { obj.Item2.GetType() };
                    var makeme = awt.MakeGenericType(typeArgs);
                    assetWrapper = (AssetWrapperBase)Activator.CreateInstance(makeme);
                    allAssets[url] = assetWrapper;
                }

                assetWrapper.Loading = false;
                assetWrapper.Exists = true;

                //Increase ref count for pending load requests
                assetWrapper.ProcessLoadRequests(ContentManager, url);

                assetWrapper.SetAssetObject(obj.Item2); 
            }
        }

        private void HandleAssetRemoved(string url)
        {
            if (allAssets.TryGetValue(url, out var assetWrapper))
            {
                assetWrapper.Loading = false;
                assetWrapper.Exists = false;
                assetWrapper.SetAssetObject(null);
            }
        }

        public AssetWrapper<T> GetOrCreateAssetWrapper<T>(string url) where T : class
        {
            if (!allAssets.TryGetValue(url, out var assetWrapper))
            {
                assetWrapper = new AssetWrapper<T>();
                allAssets[url] = assetWrapper;
            }
            
            // If the asset is not yet available add a pending load request
            if (!assetWrapper.Exists)
                assetWrapper.AddLoadRequest();

            return (AssetWrapper<T>)assetWrapper;
        }

        public void ResetDependencyCompiler()
            => database.ResetDependencyCompiler();

        private static IObservable<Tuple<ReloadingAsset, object>> SelectAssetEvent(ReloadingAsset ra)
        {
            return ra.Result.Task.ToObservable().Select(o => SelectTaskEvent(ra, o));
        }

        private static Tuple<ReloadingAsset, object> SelectTaskEvent(ReloadingAsset ra, object o)
        {
            return new Tuple<ReloadingAsset, object>(ra, o);
        }

        public void UpdateGameSettings(Game game)
        {
            var configs = game.Settings.Configurations;
            currentRenderingMode = configs.Get<EditorSettings>().RenderingMode;
            currentColorSpace = configs.Get<RenderingSettings>().ColorSpace;
            currentNavigationGroupsHash = configs.Get<NavigationSettings>().ComputeGroupsHash();
        }

        /// <summary>
        /// A dictionary storing the urls used to load an asset, to use the same at unload, in case the asset has been renamed.
        /// </summary>
        private Dictionary<AssetId, string> AssetLoadingTimeUrls { get; } = new Dictionary<AssetId, string>();

        /// <summary>
        /// A dispatcher to the game thread.
        /// </summary>
        private IDispatcherService GameDispatcher { get; }

        /// <summary>
        /// Types that support fast reloading (ie. updating existing object instead of loading a new one and updating references).
        /// </summary>
        // TODO: add an Attribute on Assets to specify if they are fast-reloadable (plugin approach)
        private static ICollection<Type> FastReloadTypes => new Type[0];

        /// <summary>
        /// The <see cref="Game"/> associated with this instance.
        /// </summary>
        private Game Game { get; }

        /// <summary>
        /// Raised when an asset start to be compiled and loaded.
        /// </summary>
        public event EventHandler<ContentLoadEventArgs> AssetLoading;

        /// <summary>
        /// Raised when an asset has been loaded.
        /// </summary>
        public event EventHandler<ContentLoadEventArgs> AssetLoaded;

        public T GetRuntimeObject<T>(AssetItem assetItem) where T : class
        {
            if (assetItem == null) throw new ArgumentNullException(nameof(assetItem));

            var url = GetLoadingTimeUrl(assetItem);
            return !string.IsNullOrEmpty(url) ? ContentManager.Get<T>(url) : default(T);
        }

        public Task<ISyncLockable> ReserveDatabaseSyncLock()
        {
            return database.ReserveSyncLock();
        }

        public Task<IDisposable> LockDatabaseAsynchronously()
        {
            return database.LockAsync();
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
            subscriptions.Dispose();
            database.Dispose();
        }

        private void Cleanup()
        {
        }

        public void BuildAndReloadAssets(IEnumerable<AssetItem> assetsToRebuild)
        {
            var assetList = assetsToRebuild.ToList();
            if (assetList.Count == 0)
                return;

            Game.Script.AddTask(async () =>
            {
                await BuildAndReloadAssetsInternal(assetList);
            });
        }

        public async Task<Dictionary<AssetItem, object>> BuildAndReloadAssetsInternal(List<AssetItem> assetList)
        {
            logger?.Debug($"Starting BuildAndReloadAssets for assets {string.Join(", ", assetList.Select(x => x.Location))}");

            foreach (var ai in assetList)
            {
                if (allAssets.TryGetValue(ai.Location.FullPath, out var assetWrapper))
                {
                    assetWrapper.Loading = true;
                }
            }

            var value = Interlocked.Increment(ref loadingAssetCount);
            AssetLoading?.Invoke(this, new ContentLoadEventArgs(value));
            try
            {
                // Rebuild the assets
                await Task.WhenAll(assetList.Select(x => database.Build(x, BuildDependencyType.Runtime)));

                logger?.Debug("Assets have been built");
                // Unload the previous versions of assets and (re)load the newly build ones.
                var reloadedObjects = await UnloadAndReloadAssets(assetList);

                //Game.TriggerActiveRenderStageReevaluation();
                return reloadedObjects;
            }
            finally
            {
                value = Interlocked.Decrement(ref loadingAssetCount);
                AssetLoaded?.Invoke(this, new ContentLoadEventArgs(value));
                logger?.Debug($"Completed BuildAndReloadAssets for assets {string.Join(", ", assetList.Select(x => x.Location))}");
            }
        }

        private string GetLoadingTimeUrl(AssetItem assetItem)
        {
            return GetLoadingTimeUrl(assetItem.Id) ?? assetItem.Location;
        }

        private string GetLoadingTimeUrl(AssetId assetId)
        {
            string url;
            AssetLoadingTimeUrls.TryGetValue(assetId, out url);
            return url;
        }

        private bool IsCurrentlyLoaded(AssetId assetId, bool loadedManuallyOnly = false)
        {
            string url;
            return AssetLoadingTimeUrls.TryGetValue(assetId, out url) && ContentManager.IsLoaded(url, loadedManuallyOnly);
        }

        private Task<Dictionary<AssetItem, object>> UnloadAndReloadAssets(ICollection<AssetItem> assets)
        {
            var reloadingAssets = new List<ReloadingAsset>();

            // Add assets to assetsToReloadQueue
            lock (assetsToReloadLock)
            {
                foreach (var asset in assets)
                {
                    ReloadingAsset reloadingAsset;

                    // Make sure it is not already in the queue (otherwise reuse it)
                    if (!assetsToReloadMapping.TryGetValue(asset, out reloadingAsset))
                    {
                        assetsToReloadQueue.Enqueue(reloadingAsset = new ReloadingAsset(asset));
                        assetsToReloadMapping.Add(asset, reloadingAsset);
                    }

                    reloadingAssets.Add(reloadingAsset);
                }
            }

            // Ask Game thread to check our collection
            // Note: if there was many requests during same frame, they will be grouped and only first invocation of this method will process all of them in a batch
            CheckAssetsToReload();

            // Wait for all of the currently requested assets to be processed
            return Task.WhenAll(reloadingAssets.Select(x => x.Result.Task))
                .ContinueWith(task =>
                {
                    // Convert to expected output format
                    return reloadingAssets.Where(x => x.Result.Task.Result != null).ToDictionary(x => x.AssetItem, x => x.Result.Task.Result);
                });
        }

        private async void CheckAssetsToReload()
        {
            //return Task.Run(async () =>
            {
                List<ReloadingAsset> assets;

                // Get assets to reload from queue
                lock (assetsToReloadLock)
                {
                    // Nothing left, early exit
                    if (assetsToReloadQueue.Count == 0)
                        return;

                    // Copy locally and clear queue
                    assets = assetsToReloadQueue.ToList();
                    assetsToReloadQueue.Clear();
                    assetsToReloadMapping.Clear();
                }

                // Update the colorspace
                //Game.UpdateColorSpace(currentColorSpace);

                var objToFastReload = new Dictionary<string, object>();

                using (await database.MountInCurrentMicroThread())
                {
                    // First, unload assets
                    foreach (var assetToUnload in assets)
                    {
                        while (ContentManager.IsLoaded(assetToUnload.AssetItem.Location))
                        {
                            if (FastReloadTypes.Contains(assetToUnload.AssetItem.Asset.GetType()) && IsCurrentlyLoaded(assetToUnload.AssetItem.Asset.Id))
                            {
                                // If this type supports fast reload, retrieve the current (old) value via a load
                                var type = AssetRegistry.GetContentType(assetToUnload.AssetItem.Asset.GetType());
                                string url = GetLoadingTimeUrl(assetToUnload.AssetItem);
                                var oldValue = ContentManager.Get(type, url);
                                if (oldValue != null)
                                {
                                    logger?.Debug($"Preparing fast-reload of {assetToUnload.AssetItem.Location}");
                                    objToFastReload.Add(url, oldValue);
                                }
                            }
                            else if (IsCurrentlyLoaded(assetToUnload.AssetItem.Asset.Id, true))
                            {
                                // Unload this object if it has already been loaded.
                                logger?.Debug($"Unloading {assetToUnload.AssetItem.Location}");
                                await UnloadAsset(assetToUnload.AssetItem.Asset.Id);
                            }
                            else break;
                        }
                    }

                    // Process fast-reload objects
                    var nonFastReloadAssets = new List<ReloadingAsset>();
                    foreach (var assetToLoad in assets)
                    {
                        object oldValue;
                        string url = GetLoadingTimeUrl(assetToLoad.AssetItem);
                        if (FastReloadTypes.Contains(assetToLoad.AssetItem.Asset.GetType()) && objToFastReload.TryGetValue(url, out oldValue))
                        {
                            // Fill oldValue with the values from the database without reloading the object.
                            // As a result, no reference needs to be updated.
                            logger?.Debug($"Fast-reloading {assetToLoad.AssetItem.Location}");
                            ReloadContent(oldValue, assetToLoad.AssetItem);
                            var loadedObject = oldValue;

                            // This fast-reloaded content might have been already loaded through private reference, but if we're reloading it here,
                            // it means that we expect a public reference (eg. it has just been referenced publicly). Reload() won't increase public reference count
                            // so we have to do it manually.
                            if (!IsCurrentlyLoaded(assetToLoad.AssetItem.Id, true))
                            {
                                var type = AssetRegistry.GetContentType(assetToLoad.AssetItem.Asset.GetType());
                                LoadContent(type, url);
                            }

                            //await Manager.ReplaceContent(assetToLoad.AssetItem.Asset.Id, loadedObject);

                            assetToLoad.Result.SetResult(loadedObject);
                        }
                        else
                        {
                            nonFastReloadAssets.Add(assetToLoad);
                        }
                    }

                    // Load all async object in a separate task
                    // We avoid Game.Content.LoadAsync, which would wait next frame between every loaded asset
                    var microThread = Scheduler.CurrentMicroThread;
                    var bufferBlock = new BufferBlock<KeyValuePair<ReloadingAsset, object>>();
                    var task = Task.Run(() =>
                    {
                        var initialContext = SynchronizationContext.Current;
                        // This synchronization context gives access to any MicroThreadLocal values. The database to use might actually be micro thread local.
                        SynchronizationContext.SetSynchronizationContext(new MicrothreadProxySynchronizationContext(microThread));

                        foreach (var assetToLoad in nonFastReloadAssets)
                        {
                            var type = AssetRegistry.GetContentType(assetToLoad.AssetItem.Asset.GetType());
                            string url = GetLoadingTimeUrl(assetToLoad.AssetItem);

                            object loadedObject = null;
                            try
                            {
                                loadedObject = LoadContent(type, url);
                            }
                            catch (Exception e)
                            {
                                logger?.Error($"Unable to load asset [{assetToLoad.AssetItem.Location}].", e);
                            }

                            // Post it in BufferBlock so that the game-side loop can process results incrementally
                            bufferBlock.Post(new KeyValuePair<ReloadingAsset, object>(assetToLoad, loadedObject));
                        }

                        bufferBlock.Complete();

                        SynchronizationContext.SetSynchronizationContext(initialContext);
                    });

                    while (await bufferBlock.OutputAvailableAsync())
                    {
                        var item = await bufferBlock.ReceiveAsync();

                        var assetToLoad = item.Key;
                        var loadedObject = item.Value;

                        if (loadedObject != null)
                        {
                            // If it's the first load of this asset, keep its loading url
                            if (!AssetLoadingTimeUrls.ContainsKey(assetToLoad.AssetItem.Asset.Id))
                                AssetLoadingTimeUrls.Add(assetToLoad.AssetItem.Asset.Id, assetToLoad.AssetItem.Location);

                            // Remove assets that were previously loaded but are not anymore from the assetLoadingTimeUrls map.
                            foreach (var loadedUrls in AssetLoadingTimeUrls.Where(x => !ContentManager.IsLoaded(x.Value)).ToList())
                            {
                                AssetLoadingTimeUrls.Remove(loadedUrls.Key);
                            }
                        }

                        AssetBuilt.OnNext(assetToLoad);
                        assetToLoad.Result.SetResult(loadedObject);
                    }

                    // Make sure everything is complete before we return
                    await task;
                }
            }
        }

        public async Task UnloadAsset(AssetId id)
        {
            GameDispatcher.EnsureAccess();

            // Unload this object if it has already been loaded.
            using (await database.LockAsync())
            {
                string url;
                if (AssetLoadingTimeUrls.TryGetValue(id, out url))
                {
                    UnloadContent(url);
                    // Remove assets that were previously loaded but are not anymore from the assetLoadingTimeUrls map.
                    foreach (var loadedUrls in AssetLoadingTimeUrls.Where(x => !ContentManager.IsLoaded(x.Value)).ToList())
                    {
                        AssetLoadingTimeUrls.Remove(loadedUrls.Key);
                    }
                }
            }
        }

        private object LoadContent(Type type, string url)
        {
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Loading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            var result = ContentManager.Load(type, url);
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Loaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            return result;
        }

        public void UnloadContent(string url)
        {
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Unloading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            ////Notify asset removed if reference count is 1 (about to be 0) or asset doesnt exist
            //ContentManager.GetReferenceCounts(url, out var exists, out var publiceRefCount, out var privateRefCount);
            //if (!exists || publiceRefCount <= 1)
            //    AssetRemoved.OnNext(url);

            ContentManager.Unload(url);
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Unloaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
        }

        private void ReloadContent(object obj, AssetItem assetItem)
        {
            var url = assetItem.Location;
#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = debugStats ?? ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Reloading {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
            ContentManager.Reload(obj, url);
            AssetLoadingTimeUrls[assetItem.Id] = url;

#if DEBUG
            if (enableReferenceLogging)
            {
                debugStats = ContentManager.GetStats();
                var entry = debugStats.LoadedAssets.FirstOrDefault(x => x.Url == url);
                logger?.Debug($"Reloaded {url} (Pub: {entry?.PublicReferenceCount ?? 0}, Priv:{entry?.PrivateReferenceCount ?? 0})");
            }
#endif
        }

        /// <summary>
        /// Represents an asset being reloaded asynchronously.
        /// </summary>
        public class ReloadingAsset
        {
            public ReloadingAsset(AssetItem assetItem)
            {
                AssetItem = assetItem;
            }

            /// <summary>
            /// The asset being reloaded.
            /// </summary>
            public AssetItem AssetItem { get; }

            /// <summary>
            /// The task containg the runtime value of the reloaded asset.
            /// </summary>
            public TaskCompletionSource<object> Result { get; } = new TaskCompletionSource<object>();
        }
    }

    public class ContentLoadEventArgs : EventArgs
    {
        public ContentLoadEventArgs(int contentLoadingCount)
        {
            ContentLoadingCount = contentLoadingCount;
        }

        public int ContentLoadingCount { get; }
    }
}
