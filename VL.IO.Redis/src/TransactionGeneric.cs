using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.IO.Redis
{
    public static class TransactionHelper
    {
        public static IDisposable CreateTransaction(Spread<object> parameter, Type ChannelType)
        {

            return (IDisposable)Activator.CreateInstance(typeof(Transaction<,,,>).MakeGenericType(new Type[] { ChannelType, typeof(RedisValue), typeof(RedisValue), typeof(bool) }), parameter.GetInternalArray());
        }
    }


    public class Transaction<TInput, TInputSerialized, TGetResult, TSetResult> : IDisposable
    {
        readonly IDisposable disposable = null;

        public Transaction(
            IChannel<TInput> channel,
            Func<ITransaction, RedisKey, Task<TGetResult>> RedisChangedCommand,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TSetResult>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges,
            Func<TInput, TInputSerialized> serialize,
            Func<TSetResult, bool> DeserializeSet,
            Func<TGetResult, TInput> DeserializeGet)
        {
            var onRedisChangeNotificationOrFirstFrame = OnRedisChangeNotificationOrFirstFrame(channel, RedisChangedCommand);

            var onChannelChange = SerializeSetAndPushChanges(channel, onRedisChangeNotificationOrFirstFrame, serialize, ChannelChangedCommand, pushChanges);

            var deserializeResult = Deserialize(channel, DeserializeSet, DeserializeGet);

            disposable = onChannelChange.CombineLatest(deserializeResult, (c, d) => d).Subscribe();
        }

        private IObservable<RedisCommandQueue> OnRedisChangeNotificationOrFirstFrame(
            IChannel<TInput> channel,
            Func<ITransaction, RedisKey, Task<TGetResult>> RedisChangedCommand)
        {
            bool firstFrame = true;

            return channel.Components.OfType<RedisBinding>().FirstOrDefault().AfterFrame
                .Select((queue) =>
                {
                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();

                    if (queue.Transaction != null && model != null)
                    {
                        if
                        (
                            (model.BindingType == RedisBindingType.Receive || model.BindingType == RedisBindingType.SendAndReceive) &&
                            (
                                (queue.ReceivedChanges.Contains(model.Key) && queue.ReceivedChanges.Count > 0) ||
                                (firstFrame && model.Initialisation == Initialisation.Redis)
                            )
                            ||
                            model.BindingType == RedisBindingType.AlwaysReceive
                        )
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    RedisChangedCommand(tran, model.Key).ContinueWith(t => new KeyValuePair<Guid, object>(model.getID, (object)t.Result)),
                                    Enumerable.Empty<RedisKey>()
                                )
                            );
                            firstFrame = false;
                        }
                    }

                    return queue;
                }
            );
        }

        private IObservable<ValueTuple<RedisCommandQueue, TInput>> SerializeSetAndPushChanges(
            IChannel<TInput> channel,
            IObservable<RedisCommandQueue> queue,
            Func<TInput, TInputSerialized> serialize,
            Func<ITransaction, KeyValuePair<RedisKey, TInputSerialized>, Task<TSetResult>> ChannelChangedCommand,
            Optional<Func<RedisKey, IEnumerable<RedisKey>>> pushChanges)
        {
            return ReactiveExtensions.
                WithLatestWhenNew(channel, queue, (c, q) =>
                {
                    return ValueTuple.Create(q, c);
                }).
                Select((input) =>
                {
                    var queue = input.Item1;

                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();
                    var value = input.Item2;

                    if (queue.Transaction != null && model != null && channel.LatestAuthor != "RedisOther")
                    {
                        if (model.BindingType == RedisBindingType.Send || model.BindingType == RedisBindingType.SendAndReceive)
                        {
                            queue.Cmds.Enqueue
                            (
                                (tran) => ValueTuple.Create
                                (
                                    ChannelChangedCommand(tran, KeyValuePair.Create(model.Key, serialize(value))).ContinueWith(t => new KeyValuePair<Guid, object>(model.setID, (object)t.Result)),
                                    pushChanges.HasValue ? pushChanges.Value(model.Key) : Enumerable.Empty<RedisKey>()
                                )
                            );
                        }
                    }
                    return input;
                }
            );
        }

        private IObservable<ValueTuple<bool, bool, bool>> Deserialize(
            IChannel<TInput> channel,
            Func<TSetResult, bool> DeserializeSet,
            Func<TGetResult, TInput> DeserializeGet)
        {
            var beforeFrameResult = channel.Components.OfType<RedisBinding>().FirstOrDefault().BeforFrame
                .Select(t =>
                {
                    var dict = t;
                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();

                    bool OnSuccessfulWrite = false;
                    bool OnSuccessfulRead = false;
                    TInput result = default(TInput);

                    if (dict != null && model != null)
                    {
                        if (model.CollisionHandling == CollisionHandling.RedisWins)
                        {
                            if (dict.TryGetValue(model.getID, out var getValue))
                            {
                                result = DeserializeGet((TGetResult)getValue);
                                channel.SetObjectAndAuthor(result, "RedisOther");
                                OnSuccessfulRead = true;
                            }
                            else
                            {
                                if (dict.TryGetValue(model.setID, out var setValue))
                                {
                                    OnSuccessfulWrite = DeserializeSet((TSetResult)setValue);
                                }

                            }
                        }
                        else
                        {
                            if (dict.TryGetValue(model.setID, out var setValue))
                            {
                                OnSuccessfulWrite = DeserializeSet((TSetResult)setValue);
                            }
                            else
                            {
                                if (dict.TryGetValue(model.getID, out var getValue))
                                {
                                    result = DeserializeGet((TGetResult)getValue);
                                    channel.SetObjectAndAuthor(result, "RedisOther");
                                    OnSuccessfulRead = true;
                                }
                            }
                        }
                    }


                    return ValueTuple.Create(OnSuccessfulWrite, OnSuccessfulRead, false, result);
                })
                .Publish()
                .RefCount();

            var nextFrameChannelUpdates = channel
                .Where(
                (value) =>
                {
                    var model = channel.Components.OfType<RedisBinding>().FirstOrDefault();
                    return channel.LatestAuthor != "RedisOther" && model != null && model.CollisionHandling == CollisionHandling.RedisWins;
                })
                .WithLatestFrom(beforeFrameResult,
                (value, resultTuple) =>
                {
                    bool OnSuccessfulRead = false;
                    bool OnRedisOverWrite = false;

                    if (resultTuple.Item2)
                    {
                        channel.SetObjectAndAuthor(resultTuple.Item4, "RedisOther");
                        OnSuccessfulRead = true;
                        OnRedisOverWrite = true;
                    }

                    return ValueTuple.Create(false, OnSuccessfulRead, OnRedisOverWrite, default(TInput));
                })
                .Where(T => T.Item3);

            return beforeFrameResult
                    .Merge(nextFrameChannelUpdates)
                    .Select(t =>
                    {
                        var result = channel.Components.OfType<RedisResult>().FirstOrDefault();
                        if (result != null)
                        {
                            result.OnSuccessfulWrite = t.Item1;
                            result.OnSuccessfulRead = t.Item2;
                            result.OnRedisOverWrite = t.Item3;
                        }
                        return ValueTuple.Create(t.Item1, t.Item2, t.Item3);
                    });
        }

        public void Dispose()
        {
            disposable?.Dispose();
        }
    }
}
