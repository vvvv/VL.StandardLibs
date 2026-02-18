using Stride.Core.Mathematics;
using Stride.Games;

namespace VL.Stride.Games
{
    public static class WindowExtensions
    {
        public static void BringToFront(this GameWindow window)
        {
            VLGame.BringToFront(window);
        }

        public static void SetTitleBarInteractionWith(this GameWindow window, int value)
        {
            VLGame.SetTitleBarInteractionWidth(window, value);
        }

        /// <summary>
        /// The WinForms based implementation differs from the SDL based one.
        /// </summary>
        public static Int2 GetWindowPositionInScreenCoordinates(this GameWindow window) => VLGame.GetWindowPositionInScreenCoordinates(window);
    }
}
