using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Core.Reactive;

namespace VL.IO.Redis.Internal
{
    public interface IRedisBinding : IBinding
    {
        public BindingModel Model { get; }
    }
}
