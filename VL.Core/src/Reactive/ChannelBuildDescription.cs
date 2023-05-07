using System;

namespace VL.Core.Reactive
{
    public record ChannelBuildDescription(string Name, string TypeName)
    {
        public Type FetchType
        { 
            get
            {
                var typeRegistry = ServiceRegistry.Global.GetService<TypeRegistry>();
                return typeRegistry.GetTypeByName(TypeName) ?? typeof(object);
            }
        }
    }
}
