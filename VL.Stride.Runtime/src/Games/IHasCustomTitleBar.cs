using Stride.Graphics;
using Stride.Rendering;

namespace VL.Stride.Games;

internal interface IHasCustomTitleBar
{
    void DrawTitleBarButtons(RenderDrawContext renderDrawContext);
}
