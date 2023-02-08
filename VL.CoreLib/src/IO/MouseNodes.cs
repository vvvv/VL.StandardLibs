using Stride.Core.Mathematics;
using System;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public static class MouseNodes
    {
        public static void Position(this MouseNotification arg, out Vector2 position)
        {
            position.X = arg.Position.X;
            position.Y = arg.Position.Y;
        }

        public static void ClientArea(this MouseNotification arg, out Vector2 clientArea)
        {
            clientArea.X = arg.ClientArea.X;
            clientArea.Y = arg.ClientArea.Y;
        }

        public static MouseButtons FromMouseButtonName(string input)
        {
            MouseButtons result;
            if (Enum.TryParse<MouseButtons>(input, out result))
                return result;
            return MouseButtons.None;
        }

        public static string ToMouseButtonName(this MouseButtons input)
        {
            return input.ToString();
        }
    }
}
