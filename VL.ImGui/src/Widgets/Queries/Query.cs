using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.ImGui.Widgets
{
    internal abstract class Query : Widget
    {
        protected override sealed bool HasItemState => false;
    }
}
