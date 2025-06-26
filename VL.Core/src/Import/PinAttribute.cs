#nullable enable
using System;
using VL.Model;

namespace VL.Core.Import
{
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class PinAttribute : Attribute
    {
        private PinGroupEditModes? pinGroupEditMode;
        private PinVisibility? visibility;
        private PinExpositionMode? exposition;

        public string? Name { get; set; }

        public PinVisibility Visibility
        {
            get => visibility.GetValueOrDefault(PinVisibility.Visible);
            set => visibility = value;
        }

        public PinVisibility? GetVisibility() => visibility;

        public PinExpositionMode Exposition
        {
            get => exposition.GetValueOrDefault(PinExpositionMode.Local);
            set => exposition = value;
        }

        public PinExpositionMode? GetExposition() => exposition;

        public PinGroupKind PinGroupKind { get; set; }

        public int PinGroupDefaultCount { get; set; }

        public PinGroupEditModes PinGroupEditMode
        {
            get => pinGroupEditMode.GetValueOrDefault();
            set => pinGroupEditMode = value;
        }

        public PinGroupEditModes? GetPinGroupEditMode() => pinGroupEditMode;

        /// <summary>
        /// For internal use only.
        /// </summary>
        public bool IsState { get; set; }
    }
}
