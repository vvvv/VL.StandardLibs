using System;
using System.Collections.Generic;
using System.Text;

namespace VL.ImGui
{
    public class GenerateNodeAttribute : Attribute
    {
        public string? Name { get; set; }

        public string? Category { get; set; }

        public string? Tags { get; set; }

        public bool Fragmented { get; set; }

        public bool Button { get; set; }

        public bool GenerateRetained = true;

        public bool GenerateImmediate = true;

        public bool IsStylable = true;
    }

    public class PinAttribute : Attribute
    {
        public int Priority { get; set; }
    }
}
