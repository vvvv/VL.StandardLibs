using System;
using System.Collections.Immutable;
using VL.Core;
using VL.Model;

namespace VL.Lang.PublicAPI
{
    public interface ISolution
    {
        public static ISolution Dummy { get; } = new DummySolution();

        /// <summary>
        /// Returns a <see cref="PinGroupBuilder"/> which can be used to modify the given pin group.
        /// The builder expects all current pins to get added and once finished will synchronize the internal pin group.
        /// </summary>
        /// <param name="nodeId">The unique id of the node application.</param>
        /// <param name="pinGroup">The name of the pin group the returned builder should modify.</param>
        /// <param name="isInput">Whether or not the group is an input our output group.</param>
        /// <returns>A <see cref="PinGroupBuilder"/> to modify the given pin group.</returns>
        [Obsolete("Please use ISolution.ModifyPinGroup2")]
        PinGroupBuilder ModifyPinGroup(uint nodeId, string pinGroup, bool isInput);

        /// <summary>
        /// Sets the value of the given pin.
        /// </summary>
        /// <param name="node">The unique id of the node application.</param>
        /// <param name="pin">The name of the input pin on which the value should get set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>A new solution with the value stored in the pin of the given node.</returns>
        [Obsolete("Please use ISolution.SetPinValue2")]
        ISolution SetPinValue(uint node, string pin, object value);

        [Obsolete("Please use ISolution.SetPinValue2")]
        ISolution SetPinValue(ImmutableStack<uint> stack, string pin, object value);

        /// <summary>
        /// Make this solution the current one.
        /// </summary>
        void Confirm(SolutionUpdateKind solutionUpdateKind = SolutionUpdateKind.Default);

        /// <summary>
        /// Returns a <see cref="PinGroupBuilder"/> which can be used to modify the given pin group.
        /// The builder expects all current pins to get added and once finished will synchronize the internal pin group.
        /// </summary>
        /// <param name="nodeId">The unique id of the node application.</param>
        /// <param name="pinGroup">The name of the pin group the returned builder should modify.</param>
        /// <param name="isInput">Whether or not the group is an input our output group.</param>
        /// <returns>A <see cref="PinGroupBuilder"/> to modify the given pin group.</returns>
        PinGroupBuilder ModifyPinGroup(UniqueId nodeId, string pinGroup, bool isInput);

        /// <summary>
        /// Sets the value of the given pin.
        /// </summary>
        /// <param name="node">The unique id of the node application.</param>
        /// <param name="pin">The name of the input pin on which the value should get set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>A new solution with the value stored in the pin of the given node.</returns>
        ISolution SetPinValue(UniqueId node, string pin, object value);

        ISolution SetPinValue(ImmutableStack<UniqueId> stack, string pin, object value);

        sealed class DummySolution : ISolution
        {
            public void Confirm(SolutionUpdateKind solutionUpdateKind = SolutionUpdateKind.Default)
            {
            }

            public PinGroupBuilder ModifyPinGroup(uint nodeId, string pinGroup, bool isInput)
            {
                return DummyPinGroupBuilder.Instance;
            }

            public PinGroupBuilder ModifyPinGroup(UniqueId nodeId, string pinGroup, bool isInput)
            {
                return DummyPinGroupBuilder.Instance;
            }

            public ISolution SetPinValue(uint node, string pin, object value)
            {
                return this;
            }

            public ISolution SetPinValue(ImmutableStack<uint> stack, string pin, object value)
            {
                return this;
            }

            public ISolution SetPinValue(UniqueId node, string pin, object value)
            {
                return this;
            }

            public ISolution SetPinValue(ImmutableStack<UniqueId> stack, string pin, object value)
            {
                return this;
            }
        }

        sealed class DummyPinGroupBuilder : PinGroupBuilder
        {
            public static readonly PinGroupBuilder Instance = new DummyPinGroupBuilder();

            public override void Add(string name, string type)
            {
            }

            public override ISolution Commit()
            {
                return ISolution.Dummy;
            }
        }
    }
}
