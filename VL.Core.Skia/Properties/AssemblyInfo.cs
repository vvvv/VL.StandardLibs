using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

// All the general informaton including the version info is generated during build
// and can be found in AssemblyInfo.generated.cs
// In order to change the version info look into public-vl/VL.Build/VL.props

// This sets the default COM visibility of types in the assembly to invisible.
// If you need to expose a type to COM, use [ComVisible(true)] on that type.
[assembly: ComVisible(false)]
[assembly: InternalsVisibleTo("VL.Skia")]
[assembly: InternalsVisibleTo("VL.Stride")]
[assembly: InternalsVisibleTo("VL.Stride.Runtime")]
[assembly: InternalsVisibleTo("VL.Stride.Runtime.Nodes")]
