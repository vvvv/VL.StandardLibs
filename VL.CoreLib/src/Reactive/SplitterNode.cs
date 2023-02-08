using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using VL.Core;

namespace VL.Lib.Reactive
{
    /// <summary>
    /// Outputs the input observable and observables for OnError and OnCompleted.
    /// </summary>
    /// <remarks>
    /// The OnError and OnCompleted observables only work if the orignal output is connected.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class SplitterNode<T>
    {
        IObservable<T> FInput;
        IObservable<T> FCurrent;
        Subject<Unit> FOnCompleted = new Subject<Unit>();
        Subject<Exception> FOnError = new Subject<Exception>();

        public void Update(IObservable<T> input, out IObservable<T> original, out IObservable<Exception> onError, out IObservable<Unit> onCompleted)
        {
            onError = FOnError;
            onCompleted = FOnCompleted;

            if (input != FInput)
            {
                //only for change check
                FInput = input;

                FCurrent = input.Do(
                        _ =>
                        {
                            //no op, input observable gets passed to output
                        },
                        exception =>
                        {
                            FOnError.OnNext(exception);
                        },
                        () =>
                        {
                            FOnCompleted.OnNext(Unit.Default);
                        });
            }

            original = FCurrent;
        }
    }
}
