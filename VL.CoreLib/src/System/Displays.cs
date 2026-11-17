#nullable enable
using System.Linq;
using Stride.Core.Mathematics;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.Windowing;

// The displays of the system, as the window contract has them (IPlatformServices.Displays): DisplayInfo with its members, and how a patch finds one
[assembly: ImportType(typeof(DisplayInfo), Category = "System.Windowing")]
[assembly: ImportType(typeof(Displays), Category = "System.Windowing")]

namespace VL.Lib.Windowing
{
    /// <summary>
    /// The displays (monitors) of the system, from the <see cref="IPlatformServices"/> of the app. Bounds and working areas are in pixels of the virtual screen; the scale
    /// is pixels per DIP. A host which does not know its displays (the console, the X11 host knows one) gives fewer or none.
    /// </summary>
    public static class Displays
    {
        static IDisplays Current => PlatformServices.Default?.Displays ?? NoDisplays.Instance;

        /// <summary>The display which contains the point, or the nearest one. None when the app knows no displays.</summary>
        /// <param name="pixel">A point of the virtual screen, in pixels.</param>
        public static Optional<DisplayInfo> FromPoint(Vector2 pixel)
            => Current.FromPoint(new Int2((int)System.MathF.Round(pixel.X), (int)System.MathF.Round(pixel.Y))) is { } display ? display : default(Optional<DisplayInfo>);

        /// <summary>The primary display. None when the app knows no displays.</summary>
        public static Optional<DisplayInfo> Primary => Current.Primary is { } display ? display : default(Optional<DisplayInfo>);

        /// <summary>All displays of the system.</summary>
        public static Spread<DisplayInfo> All => Current.All.ToSpread();
    }
}
