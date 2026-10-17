using Stride.Core;
using Stride.Rendering;

namespace VL.Stride.Rendering
{
    // Stride's source generator doesn't emit keys for string typed params of an .sdfx. Complements the generated part of VLEffectMain.sdfx.
    public partial class VLEffectParameters
    {
        public static readonly PermutationParameterKey<string> MaterialExtensionNameMesh = ParameterKeys.NewPermutation<string>();
    }
}