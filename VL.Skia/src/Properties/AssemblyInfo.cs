using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core.Import;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e6326b67-09ba-4670-a020-33465b0718ba")]

[assembly: InternalsVisibleTo("VL.Stride.Windows")]

[assembly: IncludeForeign]
[assembly: ImportType(typeof(VL.Skia.FromSharedHandle))]
[assembly: ImportType(typeof(Graphics.Skia.FormBoundsNotification))]
[assembly: ImportType(typeof(Graphics.Skia.SkiaRendererNode))]