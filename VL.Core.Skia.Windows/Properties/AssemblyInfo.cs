using System.Runtime.CompilerServices;
using System.Windows.Forms;
using VL.Core.Windows;

[assembly: InternalsVisibleTo("VL.Stride")]
[assembly: InternalsVisibleTo("VL.Stride.Runtime")]
[assembly: InternalsVisibleTo("SkiaGraphEditor")]

// Moved to VL.CoreLib.Windows with the window of the window contract (they need no Skia): assemblies built against this one keep working
[assembly: TypeForwardedTo(typeof(WindowsFormsInterop))]
[assembly: TypeForwardedTo(typeof(WindowsFormsExtensions))]
[assembly: TypeForwardedTo(typeof(Win32CustomTitleBar))]
