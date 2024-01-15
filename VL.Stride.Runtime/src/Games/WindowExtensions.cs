using Stride.Games;
using System;
using System.Reflection;

namespace VL.Stride.Games
{
    public static class WindowExtensions
    {
        public static void BringToFront(this GameWindow window)
        {
            try
            { 
                var field = Type.GetType("Stride.Games.GameWindowSDL, Stride.Games")?.GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var sdlWindow = field.GetValue(window) as GameFormSDL;
                    if (sdlWindow != null)
                        sdlWindow.BringToFront();
                    return;
                }
            }
            catch { }

            try
            {
                var field = Type.GetType("Stride.Games.GameWindowWinforms, Stride.Games")?.GetField("form", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    var winformsWindow = (dynamic)field.GetValue(window);
                    if (winformsWindow != null)
                        winformsWindow.Activate();
                    return;
                }
            }
            catch { }
        }
    }
}
