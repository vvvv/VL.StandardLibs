using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO.Notifications;

namespace VL.Lib.IO
{
    /// <summary>
    /// Implement this interface on your gui plugin if this has a notion of a projection space.
    /// You can use the aspect ratio of your window or viewport to do the math or have an explicit aspect ratio transform input.
    /// </summary>
    public interface IProjectionSpace
    {
        /// <summary>
        /// Transforms a position in pixels into 
        /// * a position in normalized projection space ((bottom, left = -1, -1) .. (top, right = 1, 1)) 
        /// * a position in projection space (typically one that respects the aspect ratio of the window)
        /// so you might need to "undo" the last 2 or 3 transformations in that chain:
        ///             World T.          View T.          Proj T.          AspectR. T.           Crop T.        Viewport Placement
        /// Object Space  -->  World Space  -->  View Space  -->  PROJ SPACE  -->  NORM PROJ SPACE  -->  Viewport Space  -->  Pixel Space
        /// </summary>
        void MapFromPixels(INotificationWithPosition notification, out Vector2 inNormalizedProjection, out Vector2 inProjection);
    }

    /// <summary>
    /// Implement this to support the "Position in World" of several input device nodes.
    /// It is supposed to give you a position in the same space that gets used by placing primitives.
    /// World space can be defined in different ways for a node set:
    /// * undoing the camera transformation gets a position from projection space to world space
    /// * undoing all downstream transformations (that a module can't be aware of) - like a camera transfomation - is taking you to world space
    /// </summary>
    public interface IWorldSpace2d
    {
        Vector2 MapFromPixels(INotificationWithPosition notification);
    }

    public class SpaceHelpers
    {
        public static void DoMapFromPixels(Vector2 inPixels, Vector2 clientArea, out Vector2 inNormalizedProjection, out Vector2 inProjection)
        {
            inNormalizedProjection = new Vector2(
                -1 + 2 * (inPixels.X / (clientArea.X - 1)),
                 1 - 2 * (inPixels.Y / (clientArea.Y - 1)));

            if (clientArea.X < clientArea.Y)
                inProjection = new Vector2(inNormalizedProjection.X, inNormalizedProjection.Y * clientArea.Y / clientArea.X);
            else
                inProjection = new Vector2(inNormalizedProjection.X * clientArea.X / clientArea.Y, inNormalizedProjection.Y);
        }

        public static void MapFromPixels(INotificationWithPosition notification, 
            out Vector2 inNormalizedProjection, out Vector2 inProjection, out Vector2 inWorld)
        {
            if (notification.Sender is IProjectionSpace)
                (notification.Sender as IProjectionSpace).MapFromPixels(notification, out inNormalizedProjection, out inProjection);
            else
                DoMapFromPixels(notification.Position, notification.ClientArea, out inNormalizedProjection, out inProjection);

            if (notification.Sender is IWorldSpace2d)
                inWorld = (notification.Sender as IWorldSpace2d).MapFromPixels(notification);
            else
                inWorld = inProjection;
        }
    }
}
