using System;
using System.Linq;
using System.Reflection;

namespace VL.Core.Reactive
{
    record ChannelBuildDescription(string Name, string TypeName, TypeRegistry TypeRegistry)
    {
        /// <summary>
        /// Returns object for patched types. Must bu used when building the pin description.
        /// </summary>
        public Type CompileTimeType
        { 
            get
            {
                var type = TypeRegistry.GetTypeByName(TypeName)?.ClrType ?? typeof(object);
                // Is Patched?
                if (type.CustomAttributes.Any(c => c.AttributeType.Name == "ElementAttribute"))
                    return typeof(object);
                return type;
            }
        }

        /// <summary>
        /// Returns the type itself. Must not be called during compilation.
        /// </summary>
        public Type GetRuntimeType(AppHost appHost)
        {
            return appHost.TypeRegistry.GetTypeByName(TypeName)?.ClrType ?? typeof(object);
        }
    }
}
