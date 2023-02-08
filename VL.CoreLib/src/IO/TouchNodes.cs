using Stride.Core.Mathematics;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    public static class TouchNodes
    {
        public static void Position(this TouchNotification arg, out Vector2 position)
        {
            position = arg.Position;
        }

        public static void ClientArea(this TouchNotification arg, out Vector2 clientArea)
        {
            clientArea = arg.ClientArea;
        }

        public static void ContactArea(this TouchNotification arg, out Vector2 contactArea)
        {
            contactArea = arg.ContactArea;
        }
    }
}
