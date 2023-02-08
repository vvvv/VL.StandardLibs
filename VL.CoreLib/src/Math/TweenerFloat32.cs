/*
 * 
 * the c# vvvv math library
 * 
 * 
 */

using System;
using VL.Core;

namespace VL.Lib.Mathematics
{
    public enum TweenerTransition
    {
        Linear,
        Sine,
        Quad,
        Cubic,
        Quart,
        Quint,
        Expo,
        Circular,
        Elastic,
        Back,
        Bounce
    }

    public enum TweenerMode
    {
        In,
        Out,
        InOut,
        OutIn
    }

    //public class Tweener
    //{
    //    Func<float, float> FFunction;
    //    TweenerTransition FTransition;
    //    TweenerMode FMode;

    //    public void SetTransition(TweenerTransition transition, TweenerMode mode)
    //    {
    //        if (transition != FTransition || mode != FMode)
    //        {
    //            switch (transition)
    //            {
    //                case TweenerTransition.Linear:
    //                    FFunction = x => x;
    //                    break;
    //                case TweenerTransition.Sine:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.SinusoidalEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.SinusoidalEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.SinusoidalEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.SinusoidalEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Quad:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.QuadEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.QuadEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.QuadEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.QuadEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Cubic:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.CubicEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.CubicEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.CubicEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.CubicEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Quart:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.QuarticEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.QuarticEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.QuarticEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.QuarticEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Quint:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.QuinticEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.QuinticEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.QuinticEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.QuinticEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Expo:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.ExponentialEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.ExponentialEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.ExponentialEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.ExponentialEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Circular:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.CircularEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.CircularEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.CircularEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.CircularEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Elastic:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.ElasticEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.ElasticEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.ElasticEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.ElasticEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Back:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.BackEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.BackEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.BackEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.BackEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //                case TweenerTransition.Bounce:
    //                    switch (mode)
    //                    {
    //                        case TweenerMode.In:
    //                            FFunction = TweenerFloat32.BounceEaseIn;
    //                            break;
    //                        case TweenerMode.Out:
    //                            FFunction = TweenerFloat32.BounceEaseOut;
    //                            break;
    //                        case TweenerMode.InOut:
    //                            FFunction = TweenerFloat32.BounceEaseInOut;
    //                            break;
    //                        case TweenerMode.OutIn:
    //                            FFunction = TweenerFloat32.BounceEaseOutIn;
    //                            break;
    //                    }
    //                    break;
    //            }

    //            FTransition = transition;
    //            FMode = mode;
    //        }
    //    }

    //    public float Tween(float value)
    //    {
    //        return FFunction(value);
    //    }

    //    public void GetTransition(out TweenerTransition transition, out TweenerMode mode)
    //    {
    //        transition = FTransition;
    //        mode = FMode;
    //    }
    //}

    /// <summary>
    /// Tweener routines, interpolation functions for a value in the range [0..1] in various shapes
    /// 
    /// Code by west
    /// </summary>
    public static class TweenerFloat32
    {
        // -= QUADRATIC EASING =-

        /// <summary>
        /// QUADRATIC EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuadEaseIn(float X)
        {
            return X * X;
        }

        /// <summary>
        /// QUADRATIC EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuadEaseOut(float X)
        {
            return -(X * (X - 2));
        }

        /// <summary>
        /// QUADRATIC EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuadEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuadEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuadEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// QUADRATIC EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuadEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuadEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuadEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= CUBIC EASING =-

        /// <summary>
        /// CUBIC EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CubicEaseIn(float X)
        {
            return X * X * X;
        }

        /// <summary>
        /// CUBIC EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CubicEaseOut(float X)
        {
            X = X - 1;
            return (X * X * X) + 1;
        }

        /// <summary>
        /// CUBIC EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CubicEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.CubicEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.CubicEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// CUBIC EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CubicEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.CubicEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.CubicEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= QUARTIC EASING =-

        /// <summary>
        /// QUARTIC EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuarticEaseIn(float X)
        {
            return X * X * X * X;
        }

        /// <summary>
        /// QUARTIC EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuarticEaseOut(float X)
        {
            X = X - 1;
            X = (X * X * X * X) - 1;
            return X * -1;
        }

        /// <summary>
        /// QUARTIC EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuarticEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuarticEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuarticEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// QUARTIC EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>		
        public static float QuarticEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuarticEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuarticEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= QUINTYIC EASING =-

        /// <summary>
        /// QUINTYIC EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuinticEaseIn(float X)
        {
            return X * X * X * X * X;
        }

        /// <summary>
        /// QUINTYIC EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuinticEaseOut(float X)
        {
            X = X - 1;
            X = X * X * X * X * X;
            return X + 1;
        }

        /// <summary>
        /// QUINTYIC EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuinticEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuinticEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuinticEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// QUINTYIC EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float QuinticEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.QuinticEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.QuinticEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= SINUSOIDAL EASING =-

        /// <summary>
        /// SINUSOIDAL EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float SinusoidalEaseIn(float X)
        {
            return -1 * (float)Math.Cos(X * (Math.PI / 2)) + 1;
        }

        /// <summary>
        /// SINUSOIDAL EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float SinusoidalEaseOut(float X)
        {
            X = (float)Math.Sin(X * (Math.PI / 2));
            return X;
        }

        /// <summary>
        /// SINUSOIDAL EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float SinusoidalEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.SinusoidalEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.SinusoidalEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// SINUSOIDAL EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float SinusoidalEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.SinusoidalEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.SinusoidalEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= Exponential Easing =-

        /// <summary>
        /// EXPONENTIAL EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ExponentialEaseIn(float X)
        {
            return (float)Math.Pow(2, 10 * (X - 1)) - 0.001f;
        }

        /// <summary>
        /// EXPONENTIAL EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ExponentialEaseOut(float X)
        {
            return 1.001f * (-(float)Math.Pow(2, -10 * X) + 1);
        }

        /// <summary>
        /// EXPONENTIAL EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ExponentialEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.ExponentialEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.ExponentialEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// EXPONENTIAL EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ExponentialEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.ExponentialEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.ExponentialEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= CIRCULAR EASING =-

        /// <summary>
        /// CIRCULAR EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CircularEaseIn(float X)
        {
            return -1 * ((float)Math.Sqrt(1 - (X * X)) - 1);
        }

        /// <summary>
        /// CIRCULAR EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CircularEaseOut(float X)
        {
            return (float)Math.Sqrt(1 - (X - 1) * (X - 1));
        }

        /// <summary>
        /// CIRCULAR EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float CircularEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.CircularEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.CircularEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// CIRCULAR EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>	
        /// <returns>Shaped value</returns>			
        public static float CircularEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.CircularEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.CircularEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= ELASTIC EASING =-

        /// <summary>
        /// ELASTIC EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ElasticEaseIn(float X)
        {
            return (-1 * (float)Math.Pow(2, 10 * (X - 1)) * (float)Math.Sin(((X - 1) - 0.075f) * (2 * Math.PI) / 0.3f));
        }

        /// <summary>
        /// ELASTIC EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ElasticEaseOut(float X)
        {
            return 1 * (float)Math.Pow(2, -10 * X) * (float)Math.Sin((X - 0.075f) * (2 * Math.PI) / 0.3f) + 1;
        }

        /// <summary>
        /// ELASTIC EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ElasticEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.ElasticEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.ElasticEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// ELASTIC EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float ElasticEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.ElasticEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.ElasticEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= BACK EASING =-

        /// <summary>
        /// BACK EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BackEaseIn(float X)
        {
            return X * X * ((1.7016f + 1) * X - 1.7016f);
        }

        /// <summary>
        /// BACK EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BackEaseOut(float X)
        {
            return (X - 1) * (X - 1) * ((1.7016f + 1) * (X - 1) + 1.7016f) + 1;
        }

        /// <summary>
        /// BACK EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BackEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.BackEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.BackEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// BACK EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BackEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.BackEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.BackEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        // -= BOUNCE EASING =- 

        /// <summary>
        /// BOUNCE EASE IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BounceEaseIn(float X)
        {
            X = 1 - X;
            if (X < 1 / 2.75f)
                X = 7.5625f * X * X;
            else if (X < 2 / 2.75)
            {
                X = X - (1.5f / 2.75f);
                X = 7.5625f * X * X + 0.75f;
            }
            else if (X < 2.5 / 2.75f)
            {
                X = X - (2.25f / 2.75f);
                X = 7.5625f * X * X + 0.9375f;
            }
            else
            {
                X = X - (2.625f / 2.75f);
                X = 7.5625f * X * X + 0.984375f;
            }
            return 1 - X;
        }

        /// <summary>
        /// BOUNCE EASE OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BounceEaseOut(float X)
        {
            if (X < 1 / 2.75f)
                X = 7.5625f * X * X;
            else if (X < 2 / 2.75)
            {
                X = X - (1.5f / 2.75f);
                X = 7.5625f * X * X + 0.75f;
            }
            else if (X < 2.5f / 2.75f)
            {
                X = X - (2.25f / 2.75f);
                X = 7.5625f * X * X + 0.9375f;
            }
            else
            {
                X = X - (2.625f / 2.75f);
                X = 7.5625f * X * X + 0.984375f;
            }
            return X;
        }

        /// <summary>
        /// BOUNCE EASE IN/OUT
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BounceEaseInOut(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.BounceEaseIn(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.BounceEaseOut(X);
                return (X / 2) + 0.5f;
            }
        }

        /// <summary>
        /// BOUNCE EASE OUT/IN
        /// </summary>
        /// <param name="X">Value in the range [0..1]</param>
        /// <returns>Shaped value</returns>
        public static float BounceEaseOutIn(float X)
        {
            if (X <= 0.5f)
            {
                X = X * 2;
                X = TweenerFloat32.BounceEaseOut(X);
                return X / 2;
            }
            else
            {
                X = (X - 0.5f) * 2;
                X = TweenerFloat32.BounceEaseIn(X);
                return (X / 2) + 0.5f;
            }
        }

        public static float Tween(float value, TweenerTransition transition, TweenerMode mode)
        {
            switch (transition)
            {
                case TweenerTransition.Linear:
                    return value;
                case TweenerTransition.Sine:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return SinusoidalEaseIn(value);
                        case TweenerMode.Out:
                            return SinusoidalEaseOut(value);
                        case TweenerMode.InOut:
                            return SinusoidalEaseInOut(value);
                        case TweenerMode.OutIn:
                            return SinusoidalEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Quad:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return QuadEaseIn(value);
                        case TweenerMode.Out:
                            return QuadEaseOut(value);
                        case TweenerMode.InOut:
                            return QuadEaseInOut(value);
                        case TweenerMode.OutIn:
                            return QuadEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Cubic:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return CubicEaseIn(value);
                        case TweenerMode.Out:
                            return CubicEaseOut(value);
                        case TweenerMode.InOut:
                            return CubicEaseInOut(value);
                        case TweenerMode.OutIn:
                            return CubicEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Quart:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return QuarticEaseIn(value);
                        case TweenerMode.Out:
                            return QuarticEaseOut(value);
                        case TweenerMode.InOut:
                            return QuarticEaseInOut(value);
                        case TweenerMode.OutIn:
                            return QuarticEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Quint:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return QuinticEaseIn(value);
                        case TweenerMode.Out:
                            return QuinticEaseOut(value);
                        case TweenerMode.InOut:
                            return QuinticEaseInOut(value);
                        case TweenerMode.OutIn:
                            return QuinticEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Expo:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return ExponentialEaseIn(value);
                        case TweenerMode.Out:
                            return ExponentialEaseOut(value);
                        case TweenerMode.InOut:
                            return ExponentialEaseInOut(value);
                        case TweenerMode.OutIn:
                            return ExponentialEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Circular:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return CircularEaseIn(value);
                        case TweenerMode.Out:
                            return CircularEaseOut(value);
                        case TweenerMode.InOut:
                            return CircularEaseInOut(value);
                        case TweenerMode.OutIn:
                            return CircularEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Elastic:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return ElasticEaseIn(value);
                        case TweenerMode.Out:
                            return ElasticEaseOut(value);
                        case TweenerMode.InOut:
                            return ElasticEaseInOut(value);
                        case TweenerMode.OutIn:
                            return ElasticEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Back:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return BackEaseIn(value);
                        case TweenerMode.Out:
                            return BackEaseOut(value);
                        case TweenerMode.InOut:
                            return BackEaseInOut(value);
                        case TweenerMode.OutIn:
                            return BackEaseOutIn(value);
                    }
                    break;
                case TweenerTransition.Bounce:
                    switch (mode)
                    {
                        case TweenerMode.In:
                            return BounceEaseIn(value);
                        case TweenerMode.Out:
                            return BounceEaseOut(value);
                        case TweenerMode.InOut:
                            return BounceEaseInOut(value);
                        case TweenerMode.OutIn:
                            return BounceEaseOutIn(value);
                    }
                    break;
            }

            return value;
        }
    }
}
