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
    }
}
