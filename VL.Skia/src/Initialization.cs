using VL.Core;
using VL.Core.CompilerServices;
using VL.Lib.Basics.Resources;
using SkiaSharp;
using System.Collections.Immutable;
using System;

[assembly: AssemblyInitializer(typeof(VL.Skia.Initialization))]

namespace VL.Skia
{
    public sealed class Initialization : AssemblyInitializer<Initialization>
    {
        public override void Configure(AppHost appHost)
        {
            appHost.Services.RegisterService<IRefCounter<SKImage>>(SKObjectRefCounter.Default);
            appHost.Services.RegisterService<IRefCounter<SKPicture>>(SKObjectRefCounter.Default);

            // Using the node factory API allows us to keep thing internal
            appHost.RegisterNodeFactory("VL.Skia.Nodes", f =>
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

            appHost.Factory.RegisterSerializer<SKTypeface, SKTypefaceSerializer>(new SKTypefaceSerializer());
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

        sealed class SKTypefaceSerializer : ISerializer<SKTypeface>
        {
            public SKTypeface Deserialize(SerializationContext context, object content, Type type)
            {
                if (content is null)
                    return SKTypeface.Default;

                try
                {
                    return SKTypeface.FromFamilyName(
                        context.Deserialize<string>(content, nameof(SKTypeface.FromFamilyName)),
                        context.Deserialize<int>(content, nameof(SKTypeface.FontWeight)),
                        context.Deserialize<int>(content, nameof(SKTypeface.FontWidth)),
                        context.Deserialize<SKFontStyleSlant>(content, nameof(SKTypeface.FontSlant)));
                }
                catch
                {
                    return SKTypeface.Default;
                }
            }

            public object Serialize(SerializationContext context, SKTypeface value)
            {
                if (value is null || value == SKTypeface.Default)
                    return null;

                return new object[]
                {
                    context.Serialize(nameof(SKTypeface.FamilyName), value.FamilyName),
                    context.Serialize(nameof(SKTypeface.FontWeight), value.FontWeight),
                    context.Serialize(nameof(SKTypeface.FontWidth), value.FontWidth),
                    context.Serialize(nameof(SKTypeface.FontSlant), value.FontSlant),
                };
            }
        }
    }
}