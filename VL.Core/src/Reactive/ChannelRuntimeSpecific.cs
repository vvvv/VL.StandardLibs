using System;
using System.Diagnostics.CodeAnalysis;
using VL.Core;
using VL.Lib.Reactive;

namespace VL.Lib.Reactive
{
    interface IDummyChannel { }

    internal sealed class DummyChannel<T> : Channel<T>, IDummyChannel
    {
        public static readonly IChannel<T> Instance = new DummyChannel<T>();

        public DummyChannel()
            : this(default(T)) // We do not want the VL default, T could be the whole user application stored in an unmanaged static reference -> disaster
        {
        }

        public DummyChannel(T? value)
        {
            this.value = value;
            Enabled = false;
        }

        public override bool AcceptsValue => false;
    }

    sealed class ChannelMonadicTypeFilter : IMonadicTypeFilter
    {
        public bool Accepts(TypeDescriptor typeDescriptor)
        {
            if (typeDescriptor.ClrType == null)
                return true; // let's not restrict generic patches with explicit type arguments. Auto-lifting shall work with the idea that the type might be ok.

            return !typeDescriptor.ClrType.IsAssignableTo(typeof(IChannel)); // T shall not be a channel itself - at least not when auto lifting. explicit usage ok.
        }
    }

    public static partial class ChannelHelpers
    {
        public static IChannel<T> Dummy<T>() => DummyChannel<T>.Instance;

        public static readonly IChannel<object> DummyNonGeneric = new DummyChannel<object>("ceci n'est pas une pipe");

        public static bool IsValid([NotNullWhen(true)] this IChannel? c)
            => c is not null && c is not IDummyChannel;

        [Obsolete("No longer needed", error: true)]
        public static bool IsSystemGenerated(this IChannel channel) => false;

    }

}
