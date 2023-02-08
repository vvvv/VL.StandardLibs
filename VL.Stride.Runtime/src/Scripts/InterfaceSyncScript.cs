using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace VL.Stride.Scripts
{
    public class InterfaceSyncScript : SyncScript
    {
        private ISyncScript patchScript;

        public ISyncScript PatchScript
        {
            get => patchScript;
            
            set
            {
                if (value != patchScript)
                {
                    // Cancel current script
                    patchScript?.Cancel();

                    // Assign to field
                    patchScript = value;

                    // Call Start() if the entity is in the scene graph
                    if (value != null && Entity != null && Entity.Scene != null)
                    {
                        patchScript.Start(this);
                    }
                }
            }
        }

        public override void Start()
        {
            patchScript?.Start(this);
        }

        public override void Update()
        {
            patchScript?.ScriptUpdate();
        }

        protected override void PriorityUpdated()
        {
            patchScript?.PriorityUpdated();
        }

        public override void Cancel()
        {
            patchScript?.Cancel();
        }
    }
}
