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
    public interface ICustomRegionPatch<T> : IDisposable
    {
        /// <summary>
        /// Updates the patch state by calling what the user patched
        /// If the context is immutable it will return a new instance of the type when necessary
        /// </summary>
        /// <param name="inputs">The inputs from the inside perspective</param>
        /// <param name="incomingLinks">The values traveling along the links that cross the region boundaries. If you don't connect anything it will autoconnect to CustomRegion.IncomingLinkValues.</param>
        /// <param name="outputs">
        /// The outputs from the inside perspective. Only outputs with assigned links on that moment get written.
        /// Use in combination with <paramref name="initialOutputs"/> to pass values from one moment to another.
        /// </param>
        /// <param name="initialOutputs">The initial ouput values.</param>
        /// <param name="update">Use to make the actual method calls on <typeparamref name="T"/>.</param>
        /// <returns></returns>
        public ICustomRegionPatch<T> Update(IReadOnlyList<object> inputs, out Spread<object> outputs, IReadOnlyList<object> incomingLinks, Spread<object> initialOutputs, Action<T> update);
    }

    /// <summary>
    /// Represents the application of your region by the user, the values that flow into the region and outof. 
    /// It also allows you to instanciate what's inside: the patch of the user. 
    /// </summary>
    public interface ICustomRegion<T>
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
        public ICustomRegionPatch<T> CreateRegionPatch(NodeContext Context, IReadOnlyList<object> initialInputs, out Spread<object> initialOutputs);

        /// <summary>
        /// Happens when users are patching or on fresh start
        /// </summary>
        public bool PatchHasChanged { get; }
    }

    // For backward compatibility
    /// <inheritdoc cref="ICustomRegionPatch{T}"/>
    public interface ICustomRegionPatch : ICustomRegionPatch<IPatchWithUpdate>
    {
        /// <inheritdoc cref="ICustomRegionPatch{T}.Update"/>
        ICustomRegionPatch Update(IReadOnlyList<object> inputs, out Spread<object> outputs, IReadOnlyList<object> incomingLinks)
        {
            return (ICustomRegionPatch)Update(inputs, out outputs, incomingLinks, Spread<object>.Empty, p => p.Update());
        }
    }

    // For backward compatibility
    public interface IPatchWithUpdate
    {
        void Update();
    }

    // For backward compatibility
    /// <inheritdoc cref="ICustomRegion{T}"/>
    public interface ICustomRegion : ICustomRegion<IPatchWithUpdate>
    {
        /// <inheritdoc cref="ICustomRegion{TPatch}.Inputs" />
        new Spread<BorderControlPointDescription> Inputs { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.Outputs" />
        new Spread<BorderControlPointDescription> Outputs { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.IncomingLinks" />
        new Spread<IncomingLinkDescription> IncomingLinks { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.InputValues" />
        new Spread<object> InputValues { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.OutputValues" />
        new IReadOnlyList<object> OutputValues { set; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.IncomingLinkValues" />
        new Spread<object> IncomingLinkValues { get; }

        /// <inheritdoc cref="ICustomRegion{TPatch}.CreateRegionPatch(NodeContext, IReadOnlyList{object}, out Spread{object})" />/>
        new ICustomRegionPatch CreateRegionPatch(NodeContext Context, IReadOnlyList<object> initialInputs, out Spread<object> initialOutputs);

        /// <inheritdoc cref="ICustomRegion{TPatch}.PatchHasChanged" />/>
        new bool PatchHasChanged { get; }
    }
}