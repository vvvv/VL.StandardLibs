#nullable enable
using System;
using System.Linq;
using VL.Core.Diagnostics;

namespace VL.Core
{
    public static class VLNodeDescriptionExtensions
    {
        public static bool IsValid(this IVLNodeDescription description) => !description.Messages.Any(m => m.Type == MessageType.Error);
    }
}
#nullable restore