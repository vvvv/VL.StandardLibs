using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.ImGui
{
    public class PointerViewPort : PointerDeviceBase
    {
        public PointerViewPort(ImputSourceViewPort source) 
        {
            Source = source;
            Id = Guid.NewGuid();
        }
        public override string Name => "ViewPort Mouse";

        public override Guid Id { get; }

        public override IInputSource Source { get; }
    }
}
