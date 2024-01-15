using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using static System.FormattableString;

namespace VL.Lib.Color
{
    public static class ColorNodes
    {
        public static readonly Color4 One = new Color4(1, 1, 1, 1);

        public static readonly Color4 Zero = new Color4(0, 0, 0, 0);

        /// <summary>
        /// Joins a color from its components
        /// </summary>
        public static Color4 Join(float red = 1, float green = 1, float blue = 1, float alpha = 1)
        {
            return new Color4(red, green, blue, alpha);
        }

        /// <summary>
        /// Joins a color from a Vector3 and alpha
        /// </summary>
        public static Color4 JoinRGBAlpha(ref Vector3 RGB, float alpha = 1)
        {
            return new Color4(RGB.X, RGB.Y, RGB.Z, alpha);
        }

        /// <summary>
        /// Splits a color into its components
        /// </summary>
        public static void Split(ref Color4 input, out float red, out float green, out float blue, out float alpha)
        {
            red = input.R;
            green = input.G;
            blue = input.B;
            alpha = input.A;
        }

        /// <summary>
        /// Overrides the red component of the color
        /// </summary>
        /// <param name="input"></param>
        /// <param name="red"></param>
        /// <returns></returns>
        public static Color4 SetRed(ref Color4 input, float red)
        {
            return new Color4(red, input.G, input.B, input.A);
        }

        /// <summary>
        /// Overrides the green component of the color
        /// </summary>
        /// <param name="input"></param>
        /// <param name="green"></param>
        /// <returns></returns>
        public static Color4 SetGreen(ref Color4 input, float green)
        {
            return new Color4(input.R, input.G, green, input.A);
        }

        /// <summary>
        /// Overrides the blue component of the color
        /// </summary>
        /// <param name="input"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        public static Color4 SetBlue(ref Color4 input, float blue)
        {
            return new Color4(input.R, blue, input.B, input.A);
        }

        /// <summary>
        /// Overrides the alpha component of the color
        /// </summary>
        /// <param name="input"></param>
        /// <param name="alpha"></param>
        /// <returns></returns>
        public static Color4 SetAlpha(ref Color4 input, float alpha = 1)
        {
            return new Color4(input.R, input.G, input.B, alpha);
        }

        public static Color4 Lerp(ref Color4 input, ref Color4 input2, float scalar)
        {
            return new Color4(
                input.R + (input2.R - input.R) * scalar,
                input.G + (input2.G - input.G) * scalar,
                input.B + (input2.B - input.B) * scalar,
                input.A + (input2.A - input.A) * scalar);
            //input.X * (1 - scalar) + input2.X * scalar better?
        }

        /// <summary>
        /// Scales the RGB values, alpha will stay the same
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="scalar"></param>
        public static void Scale(ref Color4 input, out Color4 output, float scalar = 1)
        {
            output.R = input.R * scalar;
            output.G = input.G * scalar;
            output.B = input.B * scalar;
            output.A = input.A;
        }

        /// <summary>
        /// Scales the RGB values, alpha will stay the same
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="scalar"></param>
        public static void DivScale(ref Color4 input, out Color4 output, float scalar = 1)
        {
            scalar = 1 / scalar;
            output.R = input.R * scalar;
            output.G = input.G * scalar;
            output.B = input.B * scalar;
            output.A = input.A;
        }

        /// <summary>
        /// Computes the premultiplied value of the provided color.
        /// </summary>
        /// <param name="value">The non-premultiplied value.</param>
        /// <param name="result">The premultiplied result.</param>
        public static void Premultiply(ref Color4 value, out Color4 result)
        {
            result.R = value.R * value.A;
            result.G = value.G * value.A;
            result.B = value.B * value.A;
            result.A = value.A;
        }
    }
}
