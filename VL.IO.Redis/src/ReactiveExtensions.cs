using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace VL.IO.Redis
{
    public static class ReactiveExtensions
    {
        /// <summary>
        /// will act like tihs
        /// 
        /// f--x---x---x---x-------x---x---x-
        /// ----\-----------\-------\--------
        /// s----y-----------y---y---y-------
        /// -----|-----------|-------|-------
        /// r----x-----------x-------x-------
        /// -----y-----------y-------y-------
        ///            
        /// http://introtorx.com/Content/v1.0.10621.0/17_SequencesOfCoincidence.html#Join
        /// https://stackoverflow.com/questions/13319241/combine-two-observables-but-only-when-the-first-obs-is-immediately-preceded-by?rq=3
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        public static IObservable<TResult> WithLatestWhenNew<TFirst, TSecond, TResult>(IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TSecond, TResult> selector)
        {

            var left = first.Publish().RefCount();
            var rigth = second.Publish().RefCount();

            return Observable.Join(
                left,
                rigth,
                // leftDurationSellector
                _ => left.Any().Merge(rigth.Any()),
                // rightDurationSellector
                _ => Observable.Empty<Unit>(),
                // resultSelector
                (l, r) => { return selector.Invoke(l, r); }

                );
        }

        /// <summary>
        /// will act like tihs
        /// 
        /// f--x---x---x---x---x---x---x--
        /// ---|---|---|---|---|---|---|--
        /// s---123---45------------------
        /// ---|---|---|---|---|---|---|--
        /// r--x---x---x---x---x---x---x--
        /// -------3---4---5--------------
        /// use with scan for second to collect Changes               
        /// </summary>
        /// <typeparam name="TFirst"></typeparam>
        /// <typeparam name="TSecond"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <param name="select"></param>
        /// <param name="WithLatestFromSecondWhenFirst"></param>
        /// <returns></returns>
        public static IObservable<TResult> SelectOrWithLatestFrom<TFirst, TSecond, TResult>(IObservable<TFirst> first, IObservable<TSecond> second, Func<TFirst, TResult> select, Func<TResult, TSecond, TResult> WithLatestFromSecondWhenFirst)
        {
            var secondRef = second.Publish().RefCount();
            //  var firstTransformedRef = first.WithLatestFrom(second.StartWith(new[] { default(TSecond) })).Select((t) => select(t.Item1,t.Item2)).Publish().RefCount();
            var firstTransformedRef = first.Select(select).Publish().RefCount();

            return Observable.Join(
                secondRef,
                firstTransformedRef,
                // leftDurationSellector
                _ => secondRef.Any().Merge(firstTransformedRef.Any()),
                // rightDurationSellector
                _ => Observable.Empty<Unit>(),
                // resultSelector
                (l, r) => { return WithLatestFromSecondWhenFirst(r, l); }
                )
                .Merge(firstTransformedRef)
                .Buffer(firstTransformedRef)
                .Select(l => l.FirstOrDefault())
                .Publish()
                .RefCount();
        }
    }
}
