using System.Collections.Generic;

namespace VL.Video.CaptureControl
{
    interface IControls<TName>
    {
        IEnumerable<Property<TName>> GetProperties();
    }
}
