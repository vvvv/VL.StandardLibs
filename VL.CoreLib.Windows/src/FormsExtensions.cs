using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VL.CoreLib.Windows
{
    public static class FormsExtensions
    {
        private static Rectangle GetCenteredBoundsOnPrimaryScreen(int width = 600, int height = 400)
        {
            var area = Screen.PrimaryScreen.WorkingArea;
            var centerX = (area.Right + area.Left) / 2;
            var centerY = (area.Top + area.Bottom) / 2;
            return new Rectangle(centerX - width / 2, centerY - height / 2, width, height);
        }

        public static Rectangle GetBoundsThatAreOnSomeScreen(Rectangle bounds)
        {
            if (!IsVisbleOnSomeScreen(bounds))
                bounds = GetCenteredBoundsOnPrimaryScreen(bounds.Width, bounds.Height);
            return bounds;
        }

        public static bool IsVisbleOnSomeScreen(Rectangle bounds)
        {
            return Screen.AllScreens.Any(s => s.Bounds.IntersectsWith(bounds));
        }
    }
}
