using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace VL.Core.Reactive
{
    [DataContract]
    [Serializable]
    public struct PublicChannelDescription()
    {
        public PublicChannelDescription(string Name, string TypeName)
            : this()
        {
            this.Name = Name ?? throw new ArgumentNullException(nameof(Name));
            this.TypeName = TypeName ?? throw new ArgumentNullException(nameof(TypeName));
        }

        public string Name { get; set; }

        public string TypeName { get; set; }

        /// <summary>
        /// Returns object for patched types. Must be used when building the pin description.
        /// </summary>
        public Type GetCompileTimeType(TypeRegistry typeRegistry)
        {
            var type = GetRuntimeType(typeRegistry);

            // Is Patched? 
            if (type.CustomAttributes.Any(c => c.AttributeType.Name == "ElementAttribute"))
                return typeof(object);

            return type;
        }

        /// <summary>
        /// Returns the type itself. Must not be called during compilation.
        /// </summary>
        public Type GetRuntimeType(TypeRegistry typeRegistry)
        {
            return typeRegistry.GetTypeByName(TypeName)?.ClrType ?? typeof(object);
        }
    }


    sealed class PublicChannelDescriptionSerializer : ISerializer<PublicChannelDescription>
    {
        public object Serialize(SerializationContext context, PublicChannelDescription value)
        {
            return new object[] {
                context.Serialize("Name", value.Name),
                context.Serialize("Type", value.TypeName)
            };
        }

        public PublicChannelDescription Deserialize(SerializationContext context, object content, Type type)
        {
            return new PublicChannelDescription(
                context.Deserialize<string>(content, "Name"),
                context.Deserialize<string>(content, "Type"));
        }
    }
}
