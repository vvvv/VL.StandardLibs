using System;
using System.Collections.Generic;
using System.Text;
using Stride.Engine;

namespace VL.Stride.Scripts
{
    public interface ISyncScript
    {
        void Start(ScriptComponent component);

        void ScriptUpdate();

        void Cancel();

        void PriorityUpdated();
    }
}
