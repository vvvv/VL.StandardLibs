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
                return new object[]
                {
                    context.Serialize(nameof(BindingModel.Key), value.Key),
                    context.Serialize(nameof(BindingModel.Initialization), value.Initialization),
                    context.Serialize(nameof(BindingModel.BindingType), value.BindingType),
                    context.Serialize(nameof(BindingModel.CollisionHandling), value.CollisionHandling),
                    context.Serialize(nameof(BindingModel.SerializationFormat), value.SerializationFormat)
                };
            }

            public BindingModel Deserialize(SerializationContext context, object content, Type type)
            {
                return new BindingModel(
                    context.Deserialize<string>(content, nameof(BindingModel.Key)),
                    context.Deserialize<Initialization>(content, nameof(BindingModel.Initialization)),
                    context.Deserialize<RedisBindingType>(content, nameof(BindingModel.BindingType)),
                    context.Deserialize<CollisionHandling>(content, nameof(BindingModel.CollisionHandling)),
                    context.Deserialize<SerializationFormat>(content, nameof(BindingModel.SerializationFormat)));
            }
        }
    }
}
