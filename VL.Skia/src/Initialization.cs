using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using SkiaSharp;
using System.Collections.Immutable;

[assembly: AssemblyInitializer(typeof(VL.Skia.Initialization))]

namespace VL.Skia
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        protected override void RegisterServices(IVLFactory factory)
        {
            ServiceRegistry.Current.RegisterService<IRefCounter<SKImage>>(SKObjectRefCounter.Default);
            ServiceRegistry.Current.RegisterService<IRefCounter<SKPicture>>(SKObjectRefCounter.Default);

            // Using the node factory API allows us to keep thing internal
            factory.RegisterNodeFactory("VL.Skia.Nodes", f =>
            {
                var graphicsContextNode = f.NewNodeDescription(
                    name: nameof(GRContext),
                    category: "Graphics.Skia.Advanced", 
                    fragmented: true,
                    init: bc =>
                {
                    return bc.Node(
                        inputs: new[] { bc.Pin("Resource Cache Limit", typeof(int), RenderContext.ResourceCacheLimit, "The maximum number of bytes of video memory that can be held in the cache") },
                        outputs: new[] { bc.Pin("Output", typeof(GRContext)) },
                        summary: "Allows to retrieve and configure the underlying backend 3D API context of the current thread",
                        newNode: ibc =>
                        {
                            var renderContext = RenderContext.ForCurrentThread();
                            var grContext = renderContext.SkiaContext;
                            return ibc.Node(
                                inputs: new[] { ibc.Input(v => grContext.SetResourceCacheLimit(v), RenderContext.ResourceCacheLimit) },
                                outputs: new[] { ibc.Output(() => grContext) },
                                dispose: () => renderContext.Dispose());
                        });
                });
                return NodeBuilding.NewFactoryImpl(ImmutableArray.Create(graphicsContextNode));
            });
        }

        sealed class SKObjectRefCounter : IRefCounter<SKObject>
        {
            public static readonly SKObjectRefCounter Default = new SKObjectRefCounter();

            private SKObjectRefCounter()
            {
            }

            public void Init(SKObject resource)
            {
                resource?.RefCounted();
            }

            public void AddRef(SKObject resource)
            {
                resource?.AddRef();
            }

            public void Release(SKObject resource)
            {
                resource?.Release();
            }
        }
    }
}