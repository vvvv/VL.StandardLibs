using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VL.Core.Import;

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bf34c3b4-b883-4d01-8558-4bfc2131878b")]

[assembly: InternalsVisibleTo("VL.Core.Tests")]
[assembly: InternalsVisibleTo("VL.Lang.Tests")]

// Most of the types in this assembly are still imported via type forwarding in VL
[assembly: IncludeForeign]