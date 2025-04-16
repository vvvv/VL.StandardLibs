#nullable enable
using Stride.Rendering.Images;
using VL.Lib.Collections;

namespace VL.Stride.Rendering.PostFX;

[ProcessNode(Name = "ColorTransformGroup")]
public class ColorTransformGroupNode
{
    private readonly ColorTransformGroup _colorTransformGroup = new ColorTransformGroup();
    private Spread<ColorTransform?>? _transforms;

    [return: Pin(Name = "Output")]
    public ColorTransformGroup Update([Pin(Name = "Input", PinGroupKind = Model.PinGroupKind.Collection, PinGroupDefaultCount = 2)] Spread<ColorTransform?> transforms)
    {
        if (transforms == _transforms)
            return _colorTransformGroup;

        _transforms = transforms;

        _colorTransformGroup.Transforms.Clear();
        foreach (var t in transforms)
        {
            if (t is not null)
                _colorTransformGroup.Transforms.Add(t);
        }

        return _colorTransformGroup;
    }
}
