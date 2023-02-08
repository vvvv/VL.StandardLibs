using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials;
using Stride.Shaders;

namespace VL.Stride.Shaders.ShaderFX
{
    public class BlendOperation : BinaryOperation<Vector4>
    {

        public BlendOperation(BlendOperator blendOperation, IComputeValue<Vector4> left, IComputeValue<Vector4> right)
            : base(blendOperation.GetShaderSourceName(), left, right)
        {
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {

            var leftColor = new ShaderClassSource("Float4ToColor").CreateMixin();
            leftColor.AddComposition(Left, "Value", context, baseKeys);

            var rightColor = new ShaderClassSource("Float4ToColor").CreateMixin();
            rightColor.AddComposition(Right, "Value", context, baseKeys);

            var opMixin = new ShaderClassSource(ShaderName).CreateMixin();
            opMixin.AddComposition("color1", leftColor);
            opMixin.AddComposition("color2", rightColor);

            var result = new ShaderClassSource("ColorToFloat4").CreateMixin();
            result.AddComposition("Value", opMixin);

            return result;
        }
    }

    /// <summary>
    /// Operands of the Blend node.
    /// </summary>
    public enum BlendOperator
    {
        /// <summary>
        /// Add of the two textures.
        /// </summary>
        Add,

        /// <summary>
        /// Average of the two textures.
        /// </summary>
        Average,

        /// <summary>
        /// Color effect from the two textures.
        /// </summary>
        Color,

        /// <summary>
        /// Color burn effect from the two textures.
        /// </summary>
        ColorBurn,

        /// <summary>
        /// Color dodge effect from the two textures.
        /// </summary>
        ColorDodge,

        /// <summary>
        /// Darken effect from the two textures.
        /// </summary>
        Darken,

        /// <summary>
        /// Desaturate effect from the two textures.
        /// </summary>
        Desaturate,

        /// <summary>
        /// Difference of the two textures.
        /// </summary>
        Difference,

        /// <summary>
        /// Divide first texture with the second one.
        /// </summary>
        Divide,

        /// <summary>
        /// Exclusion effect from the two textures.
        /// </summary>
        Exclusion,

        /// <summary>
        /// Hard light effect from the two textures.
        /// </summary>
        HardLight,

        /// <summary>
        /// hard mix effect from the two textures.
        /// </summary>
        HardMix,

        /// <summary>
        /// Hue effect from the two textures.
        /// </summary>
        Hue,

        /// <summary>
        /// Illuminate effect from the two textures.
        /// </summary>
        Illuminate,

        /// <summary>
        /// In effect from the two textures.
        /// </summary>
        In,

        /// <summary>
        /// Inverse effect from the two textures.
        /// </summary>
        Inverse,

        /// <summary>
        /// Lighten effect from the two textures.
        /// </summary>
        Lighten,

        /// <summary>
        /// Linear burn effect from the two textures.
        /// </summary>
        LinearBurn,

        /// <summary>
        /// Linear dodge effect from the two textures.
        /// </summary>
        LinearDodge,

        /// <summary>
        /// Apply mask from second texture to the first one.
        /// </summary>
        Mask,

        /// <summary>
        /// Multiply the two textures.
        /// </summary>
        Multiply,

        /// <summary>
        /// Out effect from the two textures.
        /// </summary>
        Out,

        /// <summary>
        /// Over effect from the two textures.
        /// </summary>
        Over,

        /// <summary>
        /// Overlay effect from the two textures.
        /// </summary>
        Overlay,

        /// <summary>
        /// Pin light effect from the two textures.
        /// </summary>
        PinLight,

        /// <summary>
        /// Saturate effect from the two textures.
        /// </summary>
        Saturate,

        /// <summary>
        /// Saturation effect from the two textures.
        /// </summary>
        Saturation,

        /// <summary>
        /// Screen effect from the two textures.
        /// </summary>
        Screen,

        /// <summary>
        /// Soft light effect from the two textures.
        /// </summary>
        SoftLight,

        /// <summary>
        /// Subtract the two textures.
        /// </summary>
        Subtract,

        /// <summary>
        /// Take color for the first texture but alpha from the second
        /// </summary>
        SubstituteAlpha,

        /// <summary>
        /// Threshold, resulting in a black-white texture for grayscale against a set threshold
        /// </summary>
        Threshold,

        //TODO: lerp, clamp ?
    }

    public static class BlendOperatorExtensions
    {
        /// <summary>
        /// Get the name of the ShaderClassSource corresponding to the operation
        /// </summary>
        /// <param name="blendOperation">The operand.</param>
        /// <returns>The name of the ShaderClassSource.</returns>
        public static string GetShaderSourceName(this BlendOperator blendOperation)
        {
            switch (blendOperation)
            {
                case BlendOperator.Add:
                    return "ComputeColorAdd3ds"; //TODO: change this (ComputeColorAdd?)
                case BlendOperator.Average:
                    return "ComputeColorAverage";
                case BlendOperator.Color:
                    return "ComputeColorColor";
                case BlendOperator.ColorBurn:
                    return "ComputeColorColorBurn";
                case BlendOperator.ColorDodge:
                    return "ComputeColorColorDodge";
                case BlendOperator.Darken:
                    return "ComputeColorDarken3ds"; //"ComputeColorDarkenMaya" //TODO: change this
                case BlendOperator.Desaturate:
                    return "ComputeColorDesaturate";
                case BlendOperator.Difference:
                    return "ComputeColorDifference3ds"; //"ComputeColorDifferenceMaya" //TODO: change this
                case BlendOperator.Divide:
                    return "ComputeColorDivide";
                case BlendOperator.Exclusion:
                    return "ComputeColorExclusion";
                case BlendOperator.HardLight:
                    return "ComputeColorHardLight";
                case BlendOperator.HardMix:
                    return "ComputeColorHardMix";
                case BlendOperator.Hue:
                    return "ComputeColorHue";
                case BlendOperator.Illuminate:
                    return "ComputeColorIlluminate";
                case BlendOperator.In:
                    return "ComputeColorIn";
                case BlendOperator.Inverse:
                    return "ComputeColorInverse";
                case BlendOperator.Lighten:
                    return "ComputeColorLighten3ds"; //"ComputeColorLightenMaya" //TODO: change this
                case BlendOperator.LinearBurn:
                    return "ComputeColorLinearBurn";
                case BlendOperator.LinearDodge:
                    return "ComputeColorLinearDodge";
                case BlendOperator.Mask:
                    return "ComputeColorMask";
                case BlendOperator.Multiply:
                    return "ComputeColorMultiply"; //return "ComputeColorMultiply3ds"; //"ComputeColorMultiplyMaya" //TODO: change this
                case BlendOperator.Out:
                    return "ComputeColorOut";
                case BlendOperator.Over:
                    return "ComputeColorOver3ds"; //TODO: change this to "ComputeColorLerpAlpha"
                case BlendOperator.Overlay:
                    return "ComputeColorOverlay3ds"; //"ComputeColorOverlayMaya" //TODO: change this
                case BlendOperator.PinLight:
                    return "ComputeColorPinLight";
                case BlendOperator.Saturate:
                    return "ComputeColorSaturate";
                case BlendOperator.Saturation:
                    return "ComputeColorSaturation";
                case BlendOperator.Screen:
                    return "ComputeColorScreen";
                case BlendOperator.SoftLight:
                    return "ComputeColorSoftLight";
                case BlendOperator.Subtract:
                    return "ComputeColorSubtract"; // "ComputeColorSubtract3ds" "ComputeColorSubtractMaya" //TODO: change this
                case BlendOperator.SubstituteAlpha:
                    return "ComputeColorSubstituteAlpha";
                case BlendOperator.Threshold:
                    return "ComputeColorThreshold";
                default:
                    throw new ArgumentOutOfRangeException("binaryOperand");
            }
        }
    }
}
