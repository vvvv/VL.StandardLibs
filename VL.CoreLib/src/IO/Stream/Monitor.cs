using System;
using System.Reactive.Linq;
using VL.Core;
using VL.Core.Import;
using VL.Lib.IO;
using VL.Lib.Reactive;

[assembly: ImportType(typeof(Monitor<>), Name = "Monitor (Reactive)", Category = "IO.Advanced")]

namespace VL.Lib.IO
{
    /// <summary>
    /// Monitors the inner data streams.
    /// The "In Progress" outputs returns true whenever one of the inner streams get activated.
    /// The "OnCompleted" output bangs whenever one of the inner streams terminate successfully.
    /// </summary>
    [ProcessNode]
    public class Monitor<T>
    {
        IObservable<IObservable<Chunk<T>>> FInput;
        IObservable<IObservable<Chunk<T>>> FOutput = ObservableNodes.Never<IObservable<Chunk<T>>>();
        float FProgress;
        bool FInProgress;
        bool FOnCompleted;

        [return: Pin(Name = "Output")]
        public IObservable<IObservable<Chunk<T>>> Update(
            IObservable<IObservable<Chunk<T>>> input, out float progress, out bool inProgress, out bool onCompleted)
        {
            if (input != FInput)
            {
                FInput = input;
                FOutput = FInput.Select(chunks =>
                {
                    FInProgress = true;
                    return chunks.Do(c =>
                        {
                            FProgress = c.Progress;
                        },
                        e =>
                        {
                            RuntimeGraph.ReportException(e);
                        },
                        () =>
                        {
                            FOnCompleted = true;
                        }).
                        Finally(() =>
                        {
                            FInProgress = false;
                        });
                });
            }

            progress = FProgress;
            inProgress = FInProgress;
            onCompleted = FOnCompleted;

            FOnCompleted = false;

            return FOutput;
        }
    }
}
