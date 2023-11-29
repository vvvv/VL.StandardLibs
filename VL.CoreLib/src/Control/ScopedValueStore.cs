using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using VL.Core;
using VL.Core.PublicAPI;
using VL.Lang;
using VL.Lib.Basics.Resources;

namespace VL.Lib.Control
{
    /// <summary>
    /// This implementation doesn't work with immutable dictionaries as we don't want no allocations.
    /// The outer context could have more or less keys depending on the call stack.
    /// Let's not pollute the dictionary, let's not consolidate data -> 
    /// * We only need to clear the dictionary on Configurate, on update we just overwrite all values of the last frame. 
    ///   We can guarantee that there are no key value pairs of a different call stack.
    /// * Lookup is a bit more work since there are potentially multiple data layers.
    /// </summary>
    public class ScopedValueStore
    {
        /// <summary>
        /// Doesn't contain the data of "outer" scopedValueStores active in this call stack
        /// </summary>
        struct DataLayer
        {
            public Dictionary<Type, object> anonymousValues = new();
            public Dictionary<string, object> namedValues = new();

            public DataLayer()
            {
            }
        }

        [ThreadStatic]
        static Stack<DataLayer> stack;
        private static bool WarnIfStackIsNull(NodeContext nodeContext)
        {
            if (stack == null || stack.IsEmpty())
            {
                Warn(nodeContext, $"Node is meant to be used in a ScopedValues region.");
                return true;
            }
            return false;
        }

        private static void Warn(NodeContext nodeContext, string message)
        {
            ResourceProvider.NewPooledSystemWide(nodeContext.Path,
                _ =>
                {
                    var messages = new CompositeDisposable();
                    foreach (var id in nodeContext.Path.Stack)
                    {
                        IVLRuntime.Current?.AddPersistentMessage(new Message(id, MessageSeverity.Warning, message))
                            .DisposeBy(messages);
                    }
                    return messages;
                }, delayDisposalInMilliseconds: 1000)
                .GetHandle()
                .Dispose(); // messages will stick for some seconds
        }

        public static T LookupAnon<T>(NodeContext nodeContext, bool warn = true)
        {
            if (WarnIfStackIsNull(nodeContext)) 
                return default;
            var t = typeof(T);
            foreach (var dataLayer in stack)
            {
                if (dataLayer.anonymousValues.TryGetValue(t, out var obj))
                    return (T)obj;
            }
            if (warn)
                Warn(nodeContext, $"Didn't find anonymous data for the inferred type.");
            return default;
        }


        public static T LookupByName<T>(NodeContext nodeContext, string key, bool warn = true)
        {
            if (WarnIfStackIsNull(nodeContext))
                return default;
            foreach (var dataLayer in stack)
            {
                if (dataLayer.namedValues.TryGetValue(key, out var obj))
                    return (T)obj;
            }
            if (warn)
                Warn(nodeContext, $"Didn't find data for {key}.");
            return default;
        }

        DataLayer dataLayer = new();
        IReadOnlyList<BorderControlPointDescription> inputs;

        public void Configurate(IReadOnlyList<BorderControlPointDescription> inputs)
        {
            if (inputs != this.inputs)
            {
                dataLayer.anonymousValues.Clear();
                dataLayer.namedValues.Clear();
                this.inputs = inputs;
            }
        }

        public void ActivateScope(IReadOnlyList<object> inputValues)
        {
            if (inputs.Count != inputValues.Count)
                throw new ArgumentException("ScopedValueStore issue");

            if (stack == null)
                stack = new();

            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                var inputValue = inputValues[i];

                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    dataLayer.anonymousValues[input.TypeInfo] = inputValue;
                }
                else
                {
                    dataLayer.namedValues[input.Name] = inputValue;
                }
            }
            stack.Push(dataLayer);
        }

        public void DeactivateScope()
        {
            stack.Pop();
        }
    }
}
