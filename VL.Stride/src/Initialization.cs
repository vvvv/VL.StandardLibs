using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using VL.Stride.Assets;
using VL.Stride.Games;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using System.Windows.Forms;
using System;
using VL.Lib.Animation;
using StrideApp = Stride.Graphics.SDL.Application;
using System.Runtime.InteropServices;
using Silk.NET.SDL;
using Stride.Core;
using ServiceRegistry = VL.Core.ServiceRegistry;
using VL.Stride.Core;
using Stride.Core.Diagnostics;
using System.Threading;
using VL.Stride.Input;
using Microsoft.Extensions.DependencyInjection;
using VL.Lib.Basics.Video;

[assembly: AssemblyInitializer(typeof(VL.Stride.Lib.Initialization))]

namespace VL.Stride.Lib
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        private static int s_init;

        public Initialization()
        {
            if (UseSDL)
            {
                // Use .NET standard native library loading mechanism (before Silk.NET or Stride try their custom ones which don't work for us)
                // Only this overload raises the AssemblyLoadContext.ResolvingUnmanagedDll event
                NativeLibrary.Load("SDL2.dll", typeof(Initialization).Assembly, default);
            }

            // In our deployment the dll is not beside the exe (what Silk.NET expects) and sadly Silk.NET is not using Load but instead uses TryLoad
            // which doesn't go through the resolve event.
            NativeLibrary.Load("openxr_loader.dll", typeof(Initialization).Assembly, default);
        }

        // Remove once tested enough
        bool UseSDL = true;

        public override void Configure(AppHost appHost)
        {
            if (Interlocked.CompareExchange(ref s_init, 1, 0) == 0)
            {
                // Logging in Stride is static - all messages go through the static GlobalLogger.GlobalMessageLogged event.
                // That event does not tell us from which game a message originated. Therefor hookup our logging system once and use a null listener in each game.
                var loggerFactory = AppHost.Global.LoggerFactory;
                var defaultLogger = AppHost.Global.DefaultLogger;
                GlobalLogger.GlobalMessageLogged += new LogBridge(loggerFactory, defaultLogger);
            }

            var services = appHost.Services.RegisterService<VLGame>(_ =>
            {
                var game = new VLGame(appHost.NodeFactoryRegistry);

                // Check for --debug-gpu commandline flag to load debug graphics device
                if (Array.Exists(Environment.GetCommandLineArgs(), argument => argument == "--debug-gpu"))
                    game.GraphicsDeviceManager.DeviceCreationFlags |= DeviceCreationFlags.Debug;

                var assetBuildService = new AssetBuilderServiceScript();
                game.Services.AddService(assetBuildService);
                game.Services.AddService(appHost.Factory);

                var gameStartedHandler = default(EventHandler);
                gameStartedHandler = (s, e) =>
                {
                    game.Script.Add(assetBuildService);
                    Game.GameStarted -= gameStartedHandler;
                };
                Game.GameStarted += gameStartedHandler;

                MessageFilter messageFilter = default;
                GameContext gameContext;
                if (UseSDL && appHost.IsUser /* SDL assumes one main thread, so let's not use it when game is created inside of editor */)
                {
                    gameContext = new GameContextSDL(null, 0, 0, isUserManagingRun: true);
                    // SDL_PumpEvents shall not run the message loop (Translate/Dispatch) - already done by windows forms
                    // This calls also needs to be done after the Stride loaded the native SDL library - otherwise crash
                    Sdl.GetApi().SetHint(Sdl.HintWindowsEnableMessageloop, "0");
                    // Stride sets this flag (doesn't say why). Let's reset it as it is quite common for our render windows to not have focus.
                    Sdl.GetApi().SetHint(Sdl.HintMouseFocusClickthrough, "0");
                    // Add a message filter which intercepts WM_CHAR messages the Windows Forms loop would otherwise drop because it doesn't know the SDL created windows.
                    Application.AddMessageFilter(messageFilter = new MessageFilter());
                }
                else
                {
                    gameContext = new GameContextWinforms(null, 0, 0, isUserManagingRun: true);
                }

                game.Run(gameContext); // Creates the window

                // Clear the default scene system. We use a frame based system where scene systems as well as graphics renderer get enqueued.
                game.SceneSystem.SceneInstance = null;
                game.SceneSystem.GraphicsCompositor = null;

                // Make sure the main window doesn't block the main loop
                game.GraphicsDevice.Presenter.PresentInterval = PresentInterval.Immediate;

                // Give input devices of default window lowest priority so that the input manager prefers the ones from our windows
                foreach (var s in game.Input.Sources)
                    s.SetPriority(int.MinValue);

                var frameClock = Clocks.FrameClock;
                frameClock.GetFrameFinished().Subscribe(ffm =>
                {
                    // Re-install the app host since we're outside of the runtime instance scope here!
                    using var _ = appHost.MakeCurrentIfNone();

                    try
                    {
                        game.ElapsedUserTime = ffm.LastInterval;
                        gameContext.RunCallback();
                    }
                    catch (Exception e)
                    {
                        RuntimeGraph.ReportException(e);
                    }
                }).DisposeBy(game);

                game.BeforeDestroy += (s, e) =>
                {
                    // Stride doesn't seem to remove the script on it's own. Do it manually to ensure the cancellation token gets set.
                    game.Script.Remove(assetBuildService);
                    // And run the scheduler one last time to actually trigger the cancellation
                    game.Script.Scheduler.Run();

                    // Remove the message filter
                    if (messageFilter != null)
                        Application.RemoveMessageFilter(messageFilter);

                    game.Services.GetService<RenderDocManager>()?.RemoveHooks();
                };

                return game;
            });
            appHost.Services.RegisterService<Game>(s => s.GetRequiredService<VLGame>());
            if (appHost.IsUser)
                appHost.Services.RegisterService<IGraphicsDeviceProvider>(s => s.GetRequiredService<VLGame>());

            // Older code paths (like CEF) use obsolete IVLFactory.CreateService(NodeContext => IResourceProvider<Game>)
            appHost.Services.RegisterService<IResourceProvider<Game>>(s => ResourceProvider.Return(s.GetRequiredService<Game>()));
            appHost.Factory.RegisterService<NodeContext, IResourceProvider<Game>>(ctx => services.GetGameProvider());

            services.RegisterProvider(game =>
            {
                game.Window.Visible = true;
                return ResourceProvider.Return(game.Window, disposeAction: (window) =>
                {
                    window.Visible = false;
                });
            });
        }

        sealed class MessageFilter : IMessageFilter
        {
            bool IMessageFilter.PreFilterMessage(ref Message m)
            {
                if (m.Msg >= 256 && m.Msg <= 264)
                {
                    // For these message types the Windows Forms main loop will look for a Control with the given HWND.
                    // If it can't find one it will not do the Translate/Dispatch call -> the SDL windows never receive any text input.
                    if (m.HWnd != IntPtr.Zero && Control.FromHandle(m.HWnd) is null)
                    {
                        // Is it a SDL window?
                        foreach (var window in StrideApp.Windows)
                        {
                            if (window.Handle == m.HWnd)
                            {
                                TranslateMessage(ref m);
                                DispatchMessage(ref m);

                                StrideApp.ProcessEvents();

                                return true;
                            }
                        }
                    }
                }

                // The WndProc of SDL only creates and enqueues SDL events - those need to be dequed and processed
                StrideApp.ProcessEvents();

                // Let the normal main loop continue to do its work
                return false;
            }

            [DllImport("user32.dll")]
            static extern bool TranslateMessage([In] ref Message m);

            [DllImport("user32.dll")]
            static extern IntPtr DispatchMessage([In] ref Message m);
        }
    }
}