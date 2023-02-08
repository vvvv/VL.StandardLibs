using Stride.Games;
using System;
using System.Reflection;

namespace VL.Stride.Games
{
    public static class WindowExtensions
    {
        public static void BringToFront(this GameWindow window)
        {
            var field = Type.GetType("Stride.Games.GameWindowSDL, Stride.Games")?.GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                var sdlWindow = field.GetValue(window) as GameFormSDL;
                if (sdlWindow != null)
                    sdlWindow.BringToFront();
            }
        }
    }
}
