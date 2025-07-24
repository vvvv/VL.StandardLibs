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
                var properties = new List<object>();

                if (value.Key.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.Key), value.Key));

                if (value.Initialization.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.Initialization), value.Initialization));

                if (value.BindingType.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.BindingType), value.BindingType));
                
                if (value.CollisionHandling.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.CollisionHandling), value.CollisionHandling));
                
                if (value.SerializationFormat.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.SerializationFormat), value.SerializationFormat));
                
                if (value.Expiry.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.Expiry), value.Expiry));
                
                if (value.When.HasValue)
                    properties.Add(context.Serialize(nameof(BindingModel.When), value.When));
                
                return properties.ToArray();
            }

            public BindingModel Deserialize(SerializationContext context, object content, Type type)
            {
                return new BindingModel(
                    context.Deserialize<Optional<string>>(content, nameof(BindingModel.Key)),
                    context.Deserialize<Optional<Initialization>>(content, nameof(BindingModel.Initialization)),
                    context.Deserialize<Optional<BindingDirection>>(content, nameof(BindingModel.BindingType)),
                    context.Deserialize<Optional<CollisionHandling>>(content, nameof(BindingModel.CollisionHandling)),
                    context.Deserialize<Optional<SerializationFormat>>(content, nameof(BindingModel.SerializationFormat)),
                    context.Deserialize<Optional<TimeSpan>>(content, nameof(BindingModel.Expiry)),
                    context.Deserialize<Optional<When>>(content, nameof(BindingModel.When)));
            }
        }
    }
}
