using System;
using System.Collections;
using System.Collections.Generic;
using VL.Core;
using VL.Lib.Collections;

namespace VL.Core.PublicAPI
{
    public struct BorderControlPointDescription
    {
        public string Name;
        public Type TypeInfo;
        public int Index;
        public bool IsSplicer;

        public BorderControlPointDescription(string name, Type typeInfo, int index, bool isSplicer)
        {
            Name = name;
            TypeInfo = typeInfo;
            Index = index;
            IsSplicer = isSplicer;
        }

        public override string ToString()
        {
            return $"{Index}: {TypeInfo} {Name}";
        }
    }

    public struct IncomingLinkDescription
    {
        public Type TypeInfo;
        public int Index;

        public IncomingLinkDescription(Type typeInfo, int index)
        {
            TypeInfo = typeInfo;
            Index = index;
        }

        public override string ToString()
        {
            return $"{Index}: {TypeInfo}";
        }
    }


    /// <summary>
    /// This represents the user patch inside the region
    /// You may create and manage several patch states by calling CreateRegionPatch
    /// </summary>
    public interface ICustomRegionPatch
    {
        /// <summary>
        /// Updates the patch state by calling what the user patched
        /// If the context is immutable it will return a new instance of the type when necessary
        /// </summary>
        /// <param name="inputs">The inputs from the inside perspective</param>
        /// <param name="incomingLinks">The values traveling along the links that cross the region boundaries. If you don't connect anything it will autoconnect to CustomRegion.IncomingLinkValues.</param>
        /// <param name="outputs">The outputs from the inside perspective</param>
        /// <returns></returns>
        public ICustomRegionPatch Update(IReadOnlyList<object> inputs, out Spread<object> outputs, IReadOnlyList<object> incomingLinks);
    }

    /// <summary>
    /// Represents the application of your region by the user, the values that flow into the region and outof. 
    /// It also allows you to instanciate what's inside: the patch of the user. 
    /// </summary>
    public interface ICustomRegion
    {
        /// <summary>
        /// The inputs from an outside perspective
        /// </summary>
        public Spread<BorderControlPointDescription> Inputs { get; }

        /// <summary>
        /// The outputs from an outside perspective
        /// </summary>
        public Spread<BorderControlPointDescription> Outputs { get; }

        /// <summary>
        /// Incoming links that cross the region boundaries
        /// </summary>
        public Spread<IncomingLinkDescription> IncomingLinks { get; }

        /// <summary>
        /// Retrieves the untouched inputs. 
        /// Your region might want to change some values before feeding it to the patch.
        /// </summary>
        public Spread<object> InputValues { get; }

        /// <summary>
        /// After updating the custom region patch and altering the values you finally need to write the outputs.
        /// These may be different from the values that you got from the patch update call.
        /// </summary>
        public IReadOnlyList<object> OutputValues { set; }

        /// <summary>
        /// These are the values that travel along links that cross the region boundaries. 
        /// </summary>
        public Spread<object> IncomingLinkValues { get; }

        /// <summary>
        /// Create a patch state
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="initialInputs"></param>
        /// <param name="initialOutputs"></param>
        /// <returns></returns>
        public ICustomRegionPatch CreateRegionPatch(NodeContext Context, IReadOnlyList<object> initialInputs, out Spread<object> initialOutputs);

        /// <summary>
        /// Happens when users are patching or on fresh start
        /// </summary>
        public bool PatchHasChanged { get; }
    }

#nullable enable

    /// <summary>
    /// Implemented by the region designer. <typeparamref name="TInlay"/> defines how the patch inlay looks like and will be implemented by the user.
    /// </summary>
    /// <remarks>
    /// We currently assume that the class implementing this interface has an operation called "Update".
    /// In its current state input control points are assumed to operate on the "Update" operation 
    /// while output control points other than splicers and accumulators can be used from multiple moments.
    /// This restriction might be lifted in the future.
    /// </remarks>
    public interface IRegion<TInlay>
    {
        /// <summary>
        /// Sets the factory method used to create instances of user patched <typeparamref name="TInlay"/>.
        /// </summary>
        /// <param name="patchInlayFactory">A function that returns a new instance of <typeparamref name="TInlay"/>.</param>
        void SetPatchInlayFactory(Func<TInlay> patchInlayFactory);

        /// <summary>
        /// Called for each input (including links) that is connected to the region from the outside.
        /// </summary>
        /// <param name="description">The control point or link for which a value is passed.</param>
        /// <param name="outerValue">The value passed to the region.</param>
        void AcknowledgeInput(in InputDescription description, object? outerValue);

        /// <summary>
        /// Called for each output that is connected to the region from the outside.
        /// </summary>
        /// <param name="description">The control point for which a value is needed.</param>
        /// <param name="outerValue">The value retrieved from the region.</param>
        void RetrieveOutput(in OutputDescription description, out object? outerValue);

        /// <summary>
        /// Called from within the patch inlay to retrieve the inner value for a given control point or link.
        /// </summary>
        /// <param name="description">The control point or link for which a value is requested.</param>
        /// <param name="patchInlay">The patch inlay which asks for the value.</param>
        /// <param name="innerValue">The value which shall be passed to the inlay.</param>
        void RetrieveInput(in InputDescription description, TInlay patchInlay, out object? innerValue);

        /// <summary>
        /// Called from within the patch inlay to acknowledge the output value for a given output description.
        /// </summary>
        /// <param name="description">The control point for which a value is to be acknowledged.</param>
        /// <param name="patchInlay">The patch inlay which writes the value.</param>
        /// <param name="innerValue">The value the patch inlay produced.</param>
        void AcknowledgeOutput(in OutputDescription description, TInlay patchInlay, object? innerValue);
    }

    /// <summary>
    /// Describes an input to a region passed either via a control point or a link.
    /// </summary>
    /// <param name="Id">The unique identifier for the input.</param>
    /// <param name="OuterType">The type as seen from the outside of the region.</param>
    /// <param name="InnerType">The inner as seen from inside the region.</param>
    /// <param name="Name">The optional name of the input. Defaults to <see langword="null"/> if not specified.</param>
    /// <param name="IsLink">Whether or not this input is passed to the region via a link.</param>
    /// <param name="IsSplicer">Whether or not this input is a splicer thereby having different in- and output types.</param>
    /// <param name="AccumulatorId">If set this input refers to an accumulator.</param>
    public record struct InputDescription(string Id, Type OuterType, Type InnerType, string? Name = null, bool IsLink = false, bool IsSplicer = false, string? AccumulatorId = null)
    {
        /// <summary>
        /// Whether or not this control point is an accumulator.
        /// </summary>
        public bool IsAccumulator => AccumulatorId != null;
    }

    /// <summary>
    /// Describes an output from a region retrieved a via a control point.
    /// </summary>
    /// <param name="Id">The unique identifier for the output.</param>
    /// <param name="OuterType">The type as seen from outside of the region.</param>
    /// <param name="InnerType">The type as seen from inside the region.</param>
    /// <param name="Name">The optional name of the output. Defaults to <see langword="null"/> if not specified.</param>
    /// <param name="IsSplicer">Whether or not the output is a splicer thereby having different in- and output types.</param>
    /// <param name="AccumulatorId">If set this output refers to an accumulator.</param>
    public record struct OutputDescription(string Id, Type OuterType, Type InnerType, string? Name = null, bool IsSplicer = false, string? AccumulatorId = null)
    {
        /// <summary>
        /// Whether or not this control point is an accumulator.
        /// </summary>
        public bool IsAccumulator => AccumulatorId != null;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class RegionAttribute : Attribute
    {
        /// <summary>
        /// What control points the region shall support.
        /// </summary>
        public ControlPointType SupportedBorderControlPoints { get; init; }

        /// <summary>
        /// The type constraint to apply on the control points. 
        /// In case of splicers the system will try to align the inner type (e.g. float) with the inner most argument (and also right most) of the outer (e.g. Spread{float} or Dictionary{string, float}).
        /// </summary>
        public string? TypeConstraint { get; init; }

        /// <summary>
        /// Whether or not the type constraint is a base type.
        /// </summary>
        public bool TypeConstraintIsBaseType { get; init; } = false;
    }

    [Flags]
    public enum ControlPointType
    {
        None = 0,
        Border = 1 << 1,
        Accumulator = 1 << 2,
        Splicer = 1 << 3,
    };
}