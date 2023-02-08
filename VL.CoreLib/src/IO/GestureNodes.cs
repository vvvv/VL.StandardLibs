using System;
using Stride.Core.Mathematics;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public static class GestureNodes
    {
        // One of the fields in GESTUREINFO structure is type of Int64 (8 bytes).
        // The relevant gesture information is stored in lower 4 bytes. This
        // bit mask is used to get 4 lower bytes from this argument.
        const Int64 ULL_ARGUMENTS_BIT_MASK = 0x00000000FFFFFFFF;

        public static void Position(this GestureNotification arg, out Vector2 position)
        {
            position = arg.Position;
        }

        public static void ClientArea(this GestureNotification arg, out Vector2 clientArea)
        {
            clientArea = arg.ClientArea;
        }

        public static void Arguments(this GestureNotification arg, out double arguments)
        {
            arguments = (double)(arg.Arguments & ULL_ARGUMENTS_BIT_MASK);
        }
    }
}
