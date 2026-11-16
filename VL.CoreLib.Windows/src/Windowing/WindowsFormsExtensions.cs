namespace System.Windows.Forms;

using System.Drawing;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Vector2 = Stride.Core.Mathematics.Vector2;
using RectangleF = Stride.Core.Mathematics.RectangleF;

public static class WindowsFormsExtensions
{
    public static float LogicalToDeviceScalingFactor(this Control control)
    {
        return control.DeviceDpi / 96f;
    }

    public static int DeviceToLogicalUnits(this Control control, int value)
    {
        var factor = 96d / control.DeviceDpi;
        return (int)Math.Round(factor * value);
    }

    public static Point DeviceToLogicalUnits(this Control control, Point value)
    {
        return new Point(control.DeviceToLogicalUnits(value.X), control.DeviceToLogicalUnits(value.Y));
    }

    public static Size DeviceToLogicalUnits(this Control control, Size value)
    {
        return new Size(control.DeviceToLogicalUnits(value.Width), control.DeviceToLogicalUnits(value.Height));
    }

    public static Vector2 DeviceToLogicalUnits(this Control control, Vector2 value)
    {
        return new Vector2(control.DeviceToLogicalUnits((int)value.X), control.DeviceToLogicalUnits((int)value.Y));
    }

    public static Rectangle DeviceToLogicalUnits(this Control control, Rectangle value)
    {
        return new Rectangle(
            control.DeviceToLogicalUnits(value.X),
            control.DeviceToLogicalUnits(value.Y),
            control.DeviceToLogicalUnits(value.Width),
            control.DeviceToLogicalUnits(value.Height));
    }

    public static RectangleF DeviceToLogicalUnits(this Control control, RectangleF value)
    {
        return new RectangleF(
            control.DeviceToLogicalUnits((int)value.X),
            control.DeviceToLogicalUnits((int)value.Y),
            control.DeviceToLogicalUnits((int)value.Width),
            control.DeviceToLogicalUnits((int)value.Height));
    }

    public static double LogicalToDeviceUnits(this Control control, double value) => value * (control.DeviceDpi / 96d);

    public static float LogicalToDeviceUnits(this Control control, float value) => value * (control.DeviceDpi / 96f);

    public static Point LogicalToDeviceUnits(this Control control, Point value)
    {
        return new Point(control.LogicalToDeviceUnits(value.X), control.LogicalToDeviceUnits(value.Y));
    }

    public static PointF LogicalToDeviceUnits(this Control control, PointF value)
    {
        return new PointF(control.LogicalToDeviceUnits(value.X), control.LogicalToDeviceUnits(value.Y));
    }

    public static Vector2 LogicalToDeviceUnits(this Control control, Vector2 value)
    {
        return new Vector2(control.LogicalToDeviceUnits(value.X), control.LogicalToDeviceUnits(value.Y));
    }

    public static RectangleF LogicalToDeviceUnits(this Control control, RectangleF value)
    {
        return new RectangleF(
            control.LogicalToDeviceUnits(value.X),
            control.LogicalToDeviceUnits(value.Y),
            control.LogicalToDeviceUnits(value.Width),
            control.LogicalToDeviceUnits(value.Height));
    }

    public static Rectangle LogicalToDeviceUnits(this Control control, Rectangle value)
    {
        return new Rectangle(
            control.LogicalToDeviceUnits(value.X),
            control.LogicalToDeviceUnits(value.Y),
            control.LogicalToDeviceUnits(value.Width),
            control.LogicalToDeviceUnits(value.Height));
    }
}
