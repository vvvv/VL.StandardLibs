using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using System.Collections.Immutable;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Headless;
using VL.Skia;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;

[assembly: AssemblyInitializer(typeof(VL.AvaloniaUI.Initialization))]

namespace VL.AvaloniaUI
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        // Must be called by user thread!
        public static Application InitAvalonia()
        {
            return Instance ??= AppBuilder.Configure<App>()
                .UseWin32()
                // Clipboard not working with headless - we should port that part from the Win32 platform. But keep headless for now to not have to deal with ANGLE init/shutdown issues.
                //.UseHeadless(headlessDrawing: false)
                .UseCustomSkia()
                .LogToTrace()
                .SetupWithLifetime(new WinFormsLifetime())
                .Instance;
        }
        private static Application Instance;

        sealed class WinFormsLifetime : IControlledApplicationLifetime
        {
            private readonly RenderContext renderContext;

            public WinFormsLifetime()
            {
                renderContext = RenderContext.ForCurrentThread();
                System.Windows.Forms.Application.ApplicationExit += (s, e) => Shutdown();
            }

            public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
            public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

            public void Shutdown(int exitCode = 0)
            {
                Exit?.Invoke(this, new ControlledApplicationLifetimeExitEventArgs(exitCode));
                renderContext.Release();
            }
        }

        protected override void RegisterServices(IVLFactory factory)
        {
            // Using the node factory API allows us to keep thing internal
            factory.RegisterNodeFactory("VL.AvaloniaUI.Nodes", f =>
            {
                var nodes = ImmutableArray.CreateBuilder<IVLNodeDescription>();

                var avaloniaLayer = f.NewNodeDescription(
                    name: nameof(AvaloniaLayer),
                    category: "Avalonia",
                    fragmented: true,
                    init: bc =>
                    {
                        return bc.NewNode(
                            inputs: new[] { bc.Pin(nameof(AvaloniaLayer.Content), typeof(object), summary: "The content to display") },
                            outputs: new[] { bc.Pin("Output", typeof(ILayer)) },
                            summary: "Hosts UI built with Avalonia in VL.Skia",
                            newNode: ibc =>
                            {
                                var layer = new AvaloniaLayer();
                                return ibc.Node(
                                    inputs: new[] { ibc.Input<object>(v => layer.Content = v) },
                                    outputs: new[] { ibc.Output(() => layer) },
                                    dispose: () => layer.Dispose());
                            });
                    });
                nodes.Add(avaloniaLayer);

                nodes.Add(NodeDescription.New(f, () => new StackPanel(), n =>
                {
                    n.AddInput("Children", x => x.Children);
                    n.AddObservableInput(StackPanel.OrientationProperty);
                    n.AddObservableInput(StackPanel.SpacingProperty);
                }));

                nodes.Add(NodeDescription.New(f, 
                    () => new Button(),
                    n =>
                    {
                        n.AddObservableInput(Button.ContentProperty);
                        n.AddObservableOutput(Button.ClickEvent);
                    }));

                nodes.Add(NodeDescription.New(f,
                    () => new TextBox(),
                    n =>
                    {
                        n.AddObservableInAndOut(TextBox.TextProperty);
                    }));

                nodes.Add(NodeDescription.New(f,
                    () => new TextBlock(),
                    n =>
                    {
                        n.AddObservableInAndOut(TextBlock.TextProperty);
                    }));

                return NodeBuilding.NewFactoryImpl(nodes.ToImmutable());
            });
        }

        sealed class App : Application
        {
            public override void Initialize()
            {
                //Styles.Add(new Avalonia.Themes.Default.DefaultTheme());
                Styles.Add(new Avalonia.Themes.Fluent.FluentTheme(baseUri: null) { Mode = Avalonia.Themes.Fluent.FluentThemeMode.Light });
                base.Initialize();
            }

            public override void RegisterServices()
            {
                base.RegisterServices();
            }

            public override void OnFrameworkInitializationCompleted()
            {
                base.OnFrameworkInitializationCompleted();
            }
        }

    }
}