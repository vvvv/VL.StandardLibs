// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace VL.Stride.Rendering.Images
{
    /// <summary>
    /// Applies an ambient occlusion effect to a scene. Ambient occlusion is a technique which fakes occlusion for objects close to other opaque objects.
    /// It takes as input a color-buffer where the scene was rendered, with its associated depth-buffer.
    /// You also need to provide the camera configuration you used when rendering the scene.
    /// </summary>
    [DataContract("AmbientOcclusionWithOrtho")]
    public class AmbientOcclusionWithOrtho : AmbientOcclusion
    {
        private ImageEffectShader aoRawImageEffect;
        private ImageEffectShader blurH;
        private ImageEffectShader blurV;
        private string nameGaussianBlurH;
        private string nameGaussianBlurV;
        private float[] offsetsWeights;

        private ImageEffectShader aoApplyImageEffect;

        public AmbientOcclusionWithOrtho()
        {
            //Enabled = false;

            NumberOfSamples = 13;
            ParamProjScale = 0.5f;
            ParamIntensity = 0.2f;
            ParamBias = 0.01f;
            ParamRadius = 1f;
            NumberOfBounces = 2;
            BlurScale = 1.85f;
            EdgeSharpness = 3f;
            TempSize = TemporaryBufferSize.SizeFull;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            aoApplyImageEffect = ToLoadAndUnload(new ImageEffectShader("ApplyAmbientOcclusionWithOrthoShader"));

            aoRawImageEffect = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionWithOrthoRawAOEffect"));
            aoRawImageEffect.Initialize(Context);

            blurH = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionWithOrthoBlurEffect"));
            blurV = ToLoadAndUnload(new ImageEffectShader("AmbientOcclusionWithOrthoBlurEffect", true));
            blurH.Initialize(Context);
            blurV.Initialize(Context);

            // Setup Horizontal parameters
            blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.VerticalBlur, false);
            blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.VerticalBlur, true);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var originalColorBuffer = GetSafeInput(0);
            var originalDepthBuffer = GetSafeInput(1);

            var outputTexture = GetSafeOutput(0);

            var renderView = context.RenderContext.RenderView;

            //---------------------------------
            // Ambient Occlusion
            //---------------------------------

            var tempWidth = (originalColorBuffer.Width * (int)TempSize) / (int)TemporaryBufferSize.SizeFull;
            var tempHeight = (originalColorBuffer.Height * (int)TempSize) / (int)TemporaryBufferSize.SizeFull;
            var aoTexture1 = NewScopedRenderTarget2D(tempWidth, tempHeight, PixelFormat.R8_UNorm, 1);
            var aoTexture2 = NewScopedRenderTarget2D(tempWidth, tempHeight, PixelFormat.R8_UNorm, 1);


            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOKeys.Count, NumberOfSamples > 0 ? NumberOfSamples : 9);
            
            // check whether the projection matrix is orthographic
            var isOrtho = renderView.Projection.M44 == 1;
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOKeys.IsOrtho, isOrtho);
            blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.IsOrtho, isOrtho);
            blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.IsOrtho, isOrtho);

            Vector2 zProj;
            if (isOrtho)
            {
                zProj = new Vector2(renderView.NearClipPlane, renderView.FarClipPlane - renderView.NearClipPlane);
            }
            else
            {
                zProj = CameraKeys.ZProjectionACalculate(renderView.NearClipPlane, renderView.FarClipPlane);
            }

            // Set Near/Far pre-calculated factors to speed up the linear depth reconstruction
            aoRawImageEffect.Parameters.Set(CameraKeys.ZProjection, ref zProj);


            Vector4 screenSize = new Vector4(originalColorBuffer.Width, originalColorBuffer.Height, 0, 0);
            screenSize.Z = screenSize.X / screenSize.Y;
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ScreenInfo, screenSize);

            Vector4 projInfo;
            if (isOrtho)
            {
                // The ortho scale to map the xy coordinates
                float scaleX = 1 / renderView.Projection.M11;
                float scaleY = 1 / renderView.Projection.M22;

                // Constant factor to map the ProjScale parameter to the ortho scale
                float projZScale = Math.Max(scaleX, scaleY) * 4;

                projInfo = new Vector4(scaleX, scaleY, projZScale, 0);
            }
            else
            {
                // Projection info used to reconstruct the View space position from linear depth
                var p00 = renderView.Projection.M11;
                var p11 = renderView.Projection.M22;
                var p02 = renderView.Projection.M13;
                var p12 = renderView.Projection.M23;

                projInfo = new Vector4(
                    -2.0f / (screenSize.X * p00),
                    -2.0f / (screenSize.Y * p11),
                    (1.0f - p02) / p00,
                    (1.0f + p12) / p11);
            }

            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ProjInfo, ref projInfo);

            //**********************************
            // User parameters
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ParamProjScale, ParamProjScale);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ParamIntensity, ParamIntensity);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ParamBias, ParamBias);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ParamRadius, ParamRadius);
            aoRawImageEffect.Parameters.Set(AmbientOcclusionWithOrthoRawAOShaderKeys.ParamRadiusSquared, ParamRadius * ParamRadius);

            aoRawImageEffect.SetInput(0, originalDepthBuffer);
            aoRawImageEffect.SetOutput(aoTexture1);
            aoRawImageEffect.Draw(context, "AmbientOcclusionWithOrthoRawAO");

            for (int bounces = 0; bounces < NumberOfBounces; bounces++)
            {
                if (offsetsWeights == null)
                {
                    offsetsWeights = new[]
                    {
                        //  0.356642f, 0.239400f, 0.072410f, 0.009869f,
                        //  0.398943f, 0.241971f, 0.053991f, 0.004432f, 0.000134f, // stddev = 1.0
                            0.153170f, 0.144893f, 0.122649f, 0.092902f, 0.062970f, // stddev = 2.0
                        //  0.111220f, 0.107798f, 0.098151f, 0.083953f, 0.067458f, 0.050920f, 0.036108f, // stddev = 3.0
                    };

                    nameGaussianBlurH = string.Format("AmbientOcclusionWithOrthoBlurH{0}x{0}", offsetsWeights.Length);
                    nameGaussianBlurV = string.Format("AmbientOcclusionWithOrthoBlurV{0}x{0}", offsetsWeights.Length);
                }

                // Set Near/Far pre-calculated factors to speed up the linear depth reconstruction
                blurH.Parameters.Set(CameraKeys.ZProjection, ref zProj);
                blurV.Parameters.Set(CameraKeys.ZProjection, ref zProj);

                // Update permutation parameters
                blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.Count, offsetsWeights.Length);
                blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.BlurScale, BlurScale);
                blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.EdgeSharpness, EdgeSharpness);
                blurH.EffectInstance.UpdateEffect(context.GraphicsDevice);

                blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.Count, offsetsWeights.Length);
                blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.BlurScale, BlurScale);
                blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurKeys.EdgeSharpness, EdgeSharpness);
                blurV.EffectInstance.UpdateEffect(context.GraphicsDevice);

                // Update parameters
                blurH.Parameters.Set(AmbientOcclusionWithOrthoBlurShaderKeys.Weights, offsetsWeights);
                blurV.Parameters.Set(AmbientOcclusionWithOrthoBlurShaderKeys.Weights, offsetsWeights);

                // Horizontal pass
                blurH.SetInput(0, aoTexture1);
                blurH.SetInput(1, originalDepthBuffer);
                blurH.SetOutput(aoTexture2);
                blurH.Draw(context, nameGaussianBlurH);

                // Vertical pass
                blurV.SetInput(0, aoTexture2);
                blurV.SetInput(1, originalDepthBuffer);
                blurV.SetOutput(aoTexture1);
                blurV.Draw(context, nameGaussianBlurV);
            }

            aoApplyImageEffect.SetInput(0, originalColorBuffer);
            aoApplyImageEffect.SetInput(1, aoTexture1);
            aoApplyImageEffect.SetOutput(outputTexture);
            aoApplyImageEffect.Draw(context, "AmbientOcclusionWithOrthoApply");
        }
    }
}
