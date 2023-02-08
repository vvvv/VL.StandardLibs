using Stride.Core;
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.Stride.Engine
{
    public class ColliderShapeDescSpread : IColliderShapeDesc
    {
        public Spread<IColliderShapeDesc> ColliderShapeDescs { get; set; } = Spread<IColliderShapeDesc>.Empty;

        public ColliderShape CreateShape(IServiceRegistry registry)
        {
            throw new NotImplementedException();
        }

        public bool Match(object obj)
        {
            throw new NotImplementedException();
        }
    }
}
