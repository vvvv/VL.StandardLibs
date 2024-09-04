using Stride.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace VL.ImGui.Stride
{
    public static class InputManagerExtension
    {
        public static void RemoveInputSource(this InputManager inputManager, IInputSource inputSource)
        {
            IList<IInputSource> remainingSources = new List<IInputSource>();

            foreach (var source in inputManager.Sources)
            {
                bool equal = false;
                foreach (var key in source.Devices.Keys)
                {
                    equal |= inputSource.Devices.Keys.Contains(key);
                }
                if (!equal)
                {
                    remainingSources.Add(source);
                }
            }
            inputManager.Sources.Clear();
            inputManager.Sources.AddRange(remainingSources);
        }
    }
}
