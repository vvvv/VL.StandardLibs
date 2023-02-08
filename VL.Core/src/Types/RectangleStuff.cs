using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.Mathematics
{
    public enum RectangleAnchorHorizontal
    {
        Left = 1 << 0,
        Center = 1 << 1,
        Right = 1 << 2,
    };

    public enum RectangleAnchorVertical
    {
        Top = 1 << 3,
        Center = 1 << 4,
        Bottom = 1 << 5,
    };

    public enum RectangleAnchor
    {
        TopLeft = RectangleAnchorVertical.Top | RectangleAnchorHorizontal.Left,
        TopCenter = RectangleAnchorVertical.Top | RectangleAnchorHorizontal.Center,
        TopRight = RectangleAnchorVertical.Top | RectangleAnchorHorizontal.Right,
        MiddleLeft = RectangleAnchorVertical.Center | RectangleAnchorHorizontal.Left,
        Center = RectangleAnchorVertical.Center | RectangleAnchorHorizontal.Center,
        MiddleRight = RectangleAnchorVertical.Center | RectangleAnchorHorizontal.Right,
        BottomLeft = RectangleAnchorVertical.Bottom | RectangleAnchorHorizontal.Left,
        BottomCenter = RectangleAnchorVertical.Bottom | RectangleAnchorHorizontal.Center,
        BottomRight = RectangleAnchorVertical.Bottom | RectangleAnchorHorizontal.Right,
    };

    public static class RectangleAnchorExtensions
    {
        public static RectangleAnchorHorizontal Horizontal(this RectangleAnchor anchor) => (RectangleAnchorHorizontal)((int)anchor & 7);
        public static RectangleAnchorVertical Vertical(this RectangleAnchor anchor) => (RectangleAnchorVertical)((int)anchor & ~7);
    }
}