// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using System.Linq;

namespace VL.Stride.Rendering.Images
{
    public static partial class AmbientOcclusionWithOrthoRawAOKeys
    {
        public static readonly PermutationParameterKey<int> Count = ParameterKeys.NewPermutation<int>();
        public static readonly PermutationParameterKey<bool> IsOrtho = ParameterKeys.NewPermutation<bool>();
    }
}
