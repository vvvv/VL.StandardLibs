using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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

    internal sealed class IgnoreMemberAttribute : Attribute
    {
        public IgnoreMemberAttribute()
        {
        }
    }

    public interface ICustomRegionPatchMarker;

    public interface IPatchWithInputBCPs
    {
        /// <summary>
        /// The inputs the user placed on the region border from the inside perspective.
        /// </summary>
        public IReadOnlyList<object> Inputs { set; }
    }

    public interface IPatchWithOutputBCPs
    {
        /// <summary>
        /// The outputs the user placed on the region border from the inside perspective.
        /// </summary>
        public Spread<object> Outputs { get; }
    }

    public interface IPatchWithIncomingLinks
    {
        /// <summary>
        /// The values traveling along the links that cross the region boundaries. Defaults to CustomRegion.IncomingLinkValues.
        /// </summary>
        public IReadOnlyList<object> IncomingLinks { set; }
    }

    public abstract class TypeMarker
    {
        public sealed class T : TypeMarker;
        public sealed class T1 : TypeMarker;
        public sealed class T2 : TypeMarker;
        public sealed class T3 : TypeMarker;
        public sealed class T4 : TypeMarker;
        public sealed class TValue : TypeMarker;
        public sealed class TKey : TypeMarker;
    }

    /// <summary>
    /// Implement on your custom region patch to support accumulators.
    /// If you want to constraint the type of the accumulator to for example "Gpu&lt;T&gt;" use a <see cref="TypeMarker"/> like "Gpu&lt;TypeMarker.T&gt;".
    /// </summary>
    public interface IPatchWithAccumulators<T> : IPatchWithInputBCPs, IPatchWithOutputBCPs
    {
        [Browsable(false)]
        void DeclareAccumulators(out T accumulatorType) => throw new InvalidOperationException("Only for readability in patch");
    }

    /// <summary>
    /// Implement on your custom region patch to support input splicers.
    /// Use <see cref="TypeMarker"/> to put the outer and inner type into a relationship. For example outer = "IReadOnlyList&lt;TypeMarker.T&gt;" and inner = "IReadOnlyList&lt;TypeMarker.T&gt;".
    /// </summary>
    public interface IPatchWithInputSplicers<TOuter, TInner> : IPatchWithInputBCPs
    {
        [Browsable(false)]
        void DeclareInputSplicers(TOuter outerType, TInner innerType) => throw new InvalidOperationException("Only for readability in patch");
    }
    public interface IPatchWithOutputSplicers<TInner, TOuter> : IPatchWithOutputBCPs
    {
        [Browsable(false)]
        void DeclareOutputSplicers(out TInner innerType, out TOuter outerType) => throw new InvalidOperationException("Only for readability in patch");
    }
    public interface IPatchWithNeutralInputBCPs<T> : IPatchWithInputBCPs
    {
        [Browsable(false)]
        void DeclareNeutralInputBCPs(T inputType) => throw new InvalidOperationException("Only for readability in patch");
    }
    public interface IPatchWithNeutralOutputBCPs<T> : IPatchWithOutputBCPs
    {
        [Browsable(false)]
        void DeclareNeutralOutputBCPs(out T outputType) => throw new InvalidOperationException("Only for readability in patch");
    }

    /// <summary>
    /// This represents the user patch inside the region
    /// You may create and manage several patch states by calling CreateRegionPatch
    /// </summary>
    public interface ICustomRegionPatch : ICustomRegionPatchMarker, IPatchWithInputBCPs, IPatchWithOutputBCPs, IPatchWithIncomingLinks
    {
        public void Update();

        /// <summary>
        /// Updates the patch state by calling what the user patched
        /// If the context is immutable it will return a new instance of the type when necessary
        /// </summary>
        /// <param name="inputs">The inputs from the inside perspective</param>
        /// <param name="incomingLinks">The values traveling along the links that cross the region boundaries. If you don't connect anything it will autoconnect to CustomRegion.IncomingLinkValues.</param>
        /// <param name="outputs">The outputs from the inside perspective</param>
        /// <returns></returns>
        [IgnoreMember]
        public ICustomRegionPatch Update(IReadOnlyList<object> inputs, out Spread<object> outputs, IReadOnlyList<object> incomingLinks);

        /// <inheritdoc cref="IPatchWithInputBCPs.Inputs" />
        [IgnoreMember]
        public new IReadOnlyList<object> Inputs { set; }

        /// <inheritdoc cref="IPatchWithIncomingLinks.IncomingLinks" />
        [IgnoreMember]
        public new IReadOnlyList<object> IncomingLinks { set; }

        /// <inheritdoc cref="IPatchWithOutputBCPs.Outputs" />
        [IgnoreMember]
        public new Spread<object> Outputs { get; }
    }

    /// <summary>
    /// Represents the application of your region by the user, the values that flow into the region and outof. 
    /// It also allows you to instanciate what's inside: the patch of the user. 
    /// </summary>
    public interface ICustomRegion<out TPatch>
        where TPatch : ICustomRegionPatchMarker
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
        public TPatch CreateRegionPatch(NodeContext Context, IReadOnlyList<object> initialInputs, out Spread<object> initialOutputs);

        /// <summary>
        /// Happens when users are patching or on fresh start
        /// </summary>
        public bool PatchHasChanged { get; }
    }

    // For backward compatibility
    public interface ICustomRegion : ICustomRegion<ICustomRegionPatch>
    {
        /// <inheritdoc cref="ICustomRegion{TPatch}.Inputs" />
        public new Spread<BorderControlPointDescription> Inputs { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.Outputs" />
        public new Spread<BorderControlPointDescription> Outputs { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.IncomingLinks" />
        public new Spread<IncomingLinkDescription> IncomingLinks { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.InputValues" />
        public new Spread<object> InputValues { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.OutputValues" />
        public new IReadOnlyList<object> OutputValues { set; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.IncomingLinkValues" />
        public new Spread<object> IncomingLinkValues { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.CreateRegionPatch(NodeContext, IReadOnlyList{object}, out Spread{object})" />/>
        public new ICustomRegionPatch CreateRegionPatch(NodeContext Context, IReadOnlyList<object> initialInputs, out Spread<object> initialOutputs);

        /// <inheritdoc cref="ICustomRegion{TPatch}.PatchHasChanged" />/>
        public new bool PatchHasChanged { get; }
    }
}