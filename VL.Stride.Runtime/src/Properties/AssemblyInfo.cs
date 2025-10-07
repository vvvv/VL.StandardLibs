global using VL.Core.Import;
global using Microsoft.Extensions.Logging;
using VL.Stride.Textures;
using VL.Stride;
using VL.Stride.Rendering;

[assembly: IncludeForeign]
[assembly: ImportType(typeof(TextureToSkImage), NamespacePrefixToStrip = "VL")]
[assembly: ImportNamespace("VL.Stride.Rendering.PostFX", Category = "Stride.Rendering.PostFX")]
[assembly: ImportType(typeof(SkiaRenderer))]
[assembly: ImportType(typeof(ModelReader))]