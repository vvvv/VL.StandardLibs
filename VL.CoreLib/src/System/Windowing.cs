using VL.Core.Import;
using VL.Core.Windowing;

// The window contract as types of VL. The types live in VL.Core; they are imported here because VL.CoreLib is the library patches reference.
[assembly: ImportType(typeof(IAppWindow), Category = "System.Windowing")]
[assembly: ImportType(typeof(WindowState), Category = "System.Windowing")]
[assembly: ImportType(typeof(WindowBorderStyle), Category = "System.Windowing")]
[assembly: ImportType(typeof(WindowCursor), Category = "System.Windowing")]
[assembly: ImportType(typeof(WindowFrameMargins), Category = "System.Windowing")]
