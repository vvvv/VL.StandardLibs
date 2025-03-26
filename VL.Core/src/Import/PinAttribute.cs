#nullable enable
using System;
using VL.Model;

namespace VL.Core.Import
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class PinAttribute : Attribute
    {
        private PinGroupEditModes? pinGroupEditMode;

        public string? Name { get; set; }

        public PinVisibility Visibility { get; set; }

        public PinExpositionMode Exposition { get; set; }

        public PinGroupKind PinGroupKind { get; set; }

        public int PinGroupDefaultCount { get; set; }

        public PinGroupEditModes PinGroupEditMode
        {
            get => pinGroupEditMode.GetValueOrDefault();
            set => PinGroupEditMode = value;
        }

        public PinGroupEditModes? GetPinGroupEditMode() => pinGroupEditMode;
    }
}
