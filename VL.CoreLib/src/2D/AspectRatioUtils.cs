using System;
using Stride.Core.Mathematics;

namespace VL.Lib.Mathematics
{
    public enum SizeMode
    {
        Size, //should be called NoCorrection
        FitIn,
        FitOut,
        AutoWidth,
        AutoHeight,
        BalancedArea,
        OriginalSize, //should be called ReferenceSize
    };

    public static class AspectRatioUtils
    {
        public static Vector2 FixAspectRatio(Vector2 input, Vector2 referenceSize, SizeMode mode)
        {
            switch (mode)
            {
                case SizeMode.Size:
                    return input; // output doesn't depend on aspect ratio of content.
                case SizeMode.FitIn:
                    return new Vector2(
                        Math.Min(input.X, input.Y * referenceSize.X / referenceSize.Y),
                        Math.Min(input.Y, input.X * referenceSize.Y / referenceSize.X)
                        );
                case SizeMode.FitOut:
                    return new Vector2(
                        Math.Max(input.X, input.Y * referenceSize.X / referenceSize.Y),
                        Math.Max(input.Y, input.X * referenceSize.Y / referenceSize.X)
                        );                
                case SizeMode.AutoWidth:
                    return new Vector2(
                        input.Y * referenceSize.X / referenceSize.Y,
                        input.Y
                        );
                case SizeMode.AutoHeight:
                    return new Vector2(
                        input.X,
                        input.X * referenceSize.Y / referenceSize.X
                        );
                case SizeMode.BalancedArea:
                    // A Area; c = content/reference size, u = user specified size, f = final size
                    // A = uw * uh (user specified size which shall be the final size as well) 
                    // A = fw * fh 
                    // cw / ch = fw / fh

                    // ==>

                    // fh = A / fw; 
                    // cw / ch = fw / (A/fw) = sqr(fw) / A
                    // fw = sqrt(A * cw / ch)
                    var A = Math.Abs(input.X * input.Y);
                    var fw = (float)Math.Sqrt(A * referenceSize.X / referenceSize.Y);
                    return new Vector2(Math.Sign(input.X) * fw, Math.Sign(input.Y) * A / fw);

                case SizeMode.OriginalSize:
                    return referenceSize;
                default:
                    throw new NotImplementedException();
            }
        }

        public static void FixAspectRatio(ref RectangleF rectangle, ref Vector2 referenceSize, SizeMode mode, RectangleAnchor anchor, out RectangleF output)
        {
            RectangleNodes.Split(ref rectangle, anchor, out var position, out var size);
            size = FixAspectRatio(size, referenceSize, mode);
            RectangleNodes.Join(ref position, ref size, anchor, out output);
        }

        // Used by TextureViewer - shall we make this public?
        public static Vector2 ClampSizePreservingAspectRatio(Vector2 input, Vector2 maxSize)
        {
            float width = input.X;
            float height = input.Y;
            float maxWidth = maxSize.X;
            float maxHeight = maxSize.Y;

            // Early exit if already within bounds
            if (width <= maxWidth && height <= maxHeight)
                return new Vector2(width, height);

            float aspect = width / height;

            // Scale down to fit within max bounds while preserving aspect ratio
            float widthRatio = maxWidth / width;
            float heightRatio = maxHeight / height;
            float scale = Math.Min(widthRatio, heightRatio);

            float clampedWidth = width * scale;
            float clampedHeight = clampedWidth / aspect;

            // Ensure after scaling that we don't exceed max bounds
            if (clampedWidth > maxWidth)
            {
                clampedWidth = maxWidth;
                clampedHeight = clampedWidth / aspect;
            }
            if (clampedHeight > maxHeight)
            {
                clampedHeight = maxHeight;
                clampedWidth = clampedHeight * aspect;
            }

            return new Vector2(clampedWidth, clampedHeight);
        }
    }
}
