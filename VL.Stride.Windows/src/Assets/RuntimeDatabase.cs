using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Assets;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.MicroThreading;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Graphics;

namespace VL.Stride.Assets
{
    public class RuntimeCompilationContext : AssetCompilationContext
    {
    }

    public class RuntimeDatabase : IRuntimeDatabase
    {
        private readonly Dictionary<ObjectUrl, OutputObject> database = new Dictionary<ObjectUrl, OutputObject>();

        public ILogger Logger { get; }

        private readonly AssetBuilderService assetBuilderService;

        public Game Game { get; }

        internal readonly AssetCompilerContext CompilerContext = new AssetCompilerContext { CompilationContext = typeof(AssetCompilationContext) };
        private readonly MicroThreadLock databaseLock = new MicroThreadLock();
        private readonly GameSettingsAsset databaseGameSettings;
        internal AssetDependenciesCompiler AssetDependenciesCompiler = new AssetDependenciesCompiler(typeof(RuntimeCompilationContext));
        private bool isDisposed;

        public void ResetDependencyCompiler()
        {
            AssetDependenciesCompiler = new AssetDependenciesCompiler(typeof(RuntimeCompilationContext));
        }

        public RuntimeDatabase(ILogger logger, AssetBuilderService assetBuilderService, Game game)
        {
            this.Logger = logger;
            this.assetBuilderService = assetBuilderService;
            this.Game = game;

            CompilerContext.Platform = PlatformType.Windows;

            databaseGameSettings = GameSettingsFactory.Create();
            CompilerContext.SetGameSettingsAsset(databaseGameSettings);
            //// TODO: get the best available between 10 and 11
            //databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = GraphicsProfile.Level_11_0;

            UpdateGameSettings(Game);

        }

        public void UpdateGameSettings(Game game)
        {
            var configs = game.Settings.Configurations;
            databaseGameSettings.GetOrCreate<EditorSettings>().RenderingMode = configs.Get<EditorSettings>().RenderingMode;
            databaseGameSettings.GetOrCreate<RenderingSettings>().ColorSpace = configs.Get<RenderingSettings>().ColorSpace;
            databaseGameSettings.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile = configs.Get<RenderingSettings>().DefaultGraphicsProfile;
        }

        internal static RuntimeDatabase Create(Game game)
        {
            var builder = new AssetBuilderService(Path.Combine(PlatformFolders.ApplicationBinaryDirectory, "data"));
            var logger = new LoggerResult();
            return new RuntimeDatabase(logger, builder, game);
        }

        public void Dispose()
        {
            isDisposed = true;
            databaseLock.Dispose();
            CompilerContext.Dispose();
            database.Clear();
            assetBuilderService.Dispose();
        }

        public Task<ISyncLockable> ReserveSyncLock() => databaseLock.ReserveSyncLock();

        public Task<IDisposable> LockAsync() => databaseLock.LockAsync();

        public async Task<IDisposable> MountInCurrentMicroThread()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(RuntimeDatabase));
            if (Scheduler.CurrentMicroThread == null) throw new InvalidOperationException("The database can only be mounted in a micro-thread.");

            var lockObject = await databaseLock.LockAsync();
            // Return immediately if the database was disposed when waiting for the lock
            if (isDisposed)
                return lockObject;

            MicrothreadLocalDatabases.MountDatabase(database.Yield());
            return lockObject;
        }

        public async Task Build(AssetItem asset, BuildDependencyType dependencyType = BuildDependencyType.Runtime)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(RuntimeDatabase));

            var buildUnit = new RuntimeBuildUnit(asset, CompilerContext, AssetDependenciesCompiler);

            try
            {
                assetBuilderService.PushBuildUnit(buildUnit);
                await buildUnit.Wait();
            }
            catch (Exception e)
            {
                Logger?.Error($"An error occurred while building the scene: {e.Message}", e);
                return;
            }

            // Merge build result into the database
            using ((await databaseLock.ReserveSyncLock()).Lock())
            {
                if (isDisposed)
                    return;

                if (buildUnit.Failed)
                {
                    // Build failed => unregister object
                    // 1. If it is first-time scene loading and one of sub-asset failed, it will be in this state, but we don't care
                    // since database will be empty at that point (it won't have any effect)
                    // 2. The second case (the one we actually care about) happens when reloading a recently rebuilt individual asset (i.e. material or texture),
                    // this will actually remove it from database
                    database.Remove(new ObjectUrl(UrlType.Content, asset.Location));
                }

                foreach (var outputObject in buildUnit.OutputObjects)
                {
                    database[outputObject.Key] = outputObject.Value;
                }
            }
        }
    }
}
