using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.Parallel;

[assembly: ImportType(typeof(ForEach<,>), Category = "Experimental.Control.Parallel")]

namespace VL.Lib.Parallel
{
    /// <summary>
    /// A loop region with one input and one output which runs in parallel. The returned spread builder is always the same and will be re-used by the loop.
    /// </summary>
    [ProcessNode]
    public class ForEach<TState, TOutput> : IDisposable
        where TState : class
    {
        readonly SpreadBuilder<TState> _states = new SpreadBuilder<TState>();
        readonly SpreadBuilder<TOutput> _outputs = new SpreadBuilder<TOutput>();

        public SpreadBuilder<TOutput> Update<TInput>(IReadOnlyList<TInput> input, Func<TState> create, Func<TState, TInput, int, Tuple<TState, TOutput>> update)
        {
            var result = _outputs;
            var states = _states;

            // Cleanup old slices
            var inputCount = input.Count;
            for (int i = inputCount; i < states.Count; i++)
            {
                if (states[i] is IDisposable d)
                    d.Dispose();
            }

            states.Count = inputCount;
            result.Count = inputCount;

            if (inputCount == 0)
                return result;

            var size = Math.Max(inputCount / (2 * Environment.ProcessorCount), 64);
            System.Threading.Tasks.Parallel.ForEach(Partitioner.Create(0, inputCount, size),
                (range, loopState, local) =>
                {
                    for (int i = range.Item1; i < range.Item2; i++)
                    {
                        var sliceState = states.ElementAtOrDefault(i) ?? create();
                        (states[i], result[i]) = update(sliceState, input[i], i);
                    }
                });

            //System.Threading.Tasks.Parallel.ForEach(input,
            //    (item, loopState, il) =>
            //    {
            //        int i = (int)il;
            //        var sliceState = states.ElementAtOrDefault(i) ?? create();
            //        (states[i], result[i]) = update(sliceState, item, i);
            //    });

            return result;
        }

        public void Dispose()
        {
            foreach (var sliceState in _states.OfType<IDisposable>())
                sliceState.Dispose();
        }
    }
}
