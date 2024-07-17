using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Core.CompilerServices;
using VL.IO.Redis;

[assembly:AssemblyInitializer(typeof(_VL_.IO.Redis.Initializer))]

// By using _VL_ we can hide this class from direct import (we should find a nicer way probably)
namespace _VL_.IO.Redis
{
    public sealed class Initializer : AssemblyInitializer<Initializer>
    {
        public override void Configure(AppHost appHost)
        {
            appHost.Factory.RegisterSerializer<BindingModel, BindingModelSerializer>(BindingModelSerializer.Instance);

            base.Configure(appHost);
        }

        // TODO: Really? How about we support some standard attributes...
        sealed class BindingModelSerializer : ISerializer<BindingModel>
        {
            public static readonly BindingModelSerializer Instance = new BindingModelSerializer();

            public object Serialize(SerializationContext context, BindingModel value)
            {
                var properties = new List<object>()
                {
                    context.Serialize(nameof(BindingModel.Key), value.Key),
                    context.Serialize(nameof(BindingModel.Initialization), value.Initialization),
                    context.Serialize(nameof(BindingModel.BindingType), value.BindingType),
                    context.Serialize(nameof(BindingModel.CollisionHandling), value.CollisionHandling)
                };
                if (value.SerializationFormat.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.SerializationFormat), value.SerializationFormat));
                if (value.Expiry.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.Expiry), value.Expiry));
                if (value.When != default)
                    properties.Add(context.Serialize(nameof(BindingModel.When), value.When));
                return properties.ToArray();
            }

            public BindingModel Deserialize(SerializationContext context, object content, Type type)
            {
                return new BindingModel(
                    context.Deserialize<string>(content, nameof(BindingModel.Key)),
                    context.Deserialize<Initialization>(content, nameof(BindingModel.Initialization)),
                    context.Deserialize<BindingDirection>(content, nameof(BindingModel.BindingType)),
                    context.Deserialize<CollisionHandling>(content, nameof(BindingModel.CollisionHandling)),
                    context.Deserialize<SerializationFormat?>(content, nameof(BindingModel.SerializationFormat)),
                    context.Deserialize<TimeSpan?>(content, nameof(BindingModel.Expiry)),
                    context.Deserialize<When>(content, nameof(BindingModel.When)));
            }
        }
    }
}
