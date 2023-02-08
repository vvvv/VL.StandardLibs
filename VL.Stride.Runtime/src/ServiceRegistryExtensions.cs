using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VL.Core;
using VL.Lib.Basics.Resources;

[assembly: InternalsVisibleTo("VL.Stride")]
[assembly: InternalsVisibleTo("VL.Stride.Windows")]

namespace VL.Stride
{
    internal static class ServiceRegistryExtensions
    {
        public static void RegisterProvider<T>(this ServiceRegistry services, Func<Game, IResourceProvider<T>> providerFactory)
        {
            services.RegisterService(ResourceProvider.Defer(() => services.GetService<IResourceProvider<Game>>().Bind(providerFactory)));
        }
    }
}
