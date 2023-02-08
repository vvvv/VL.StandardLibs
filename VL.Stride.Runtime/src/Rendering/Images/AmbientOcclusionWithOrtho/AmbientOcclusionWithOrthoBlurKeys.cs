// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using System.Linq;

namespace VL.Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="AmbientOcclusionWithOrthoBlur"/> and AmbientOcclusionWithOrthoBlurEffect sdfx
    /// </summary>
    internal static class AmbientOcclusionWithOrthoBlurKeys
    {
        public static readonly PermutationParameterKey<int> Count = ParameterKeys.NewPermutation<int>(9);

        public static readonly PermutationParameterKey<bool> VerticalBlur = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<float> BlurScale = ParameterKeys.NewPermutation<float>(2f);

        public static readonly PermutationParameterKey<float> EdgeSharpness = ParameterKeys.NewPermutation<float>(4f);

        public static readonly PermutationParameterKey<bool> IsOrtho = ParameterKeys.NewPermutation<bool>();
    }
}
