global using System.Collections.Immutable;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.Reactive.Disposables;
global using VL.Core.Import;
global using VL.Lib.Collections;
global using VL.Lib.IO;
global using VL.Lib.IO.Notifications;
global using VL.Model;
using System.Runtime.CompilerServices;

[assembly: ImportAsIs(Namespace = "VL.Core", Category = "System")]
[assembly: InternalsVisibleTo("VL.UI.Forms")]