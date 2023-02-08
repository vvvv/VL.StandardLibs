using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace MyGame
{
    public enum MovementDirection
    {
        Clockwise,
        CounterClockwise
    }
    
    public class SphereController : SyncScript
    {
    
        // Declared public member fields and properties will show in the game studio
        // and can be accessed from VL
        public float Speed = 1;
        public MovementDirection Direction = MovementDirection.Clockwise;
        
        public override void Start()
        {
            // Initialization of the script.
        }

        float angle;
        public override void Update()
        {
            if (Direction == MovementDirection.Clockwise) 
                angle += Speed * 0.01f;
            else
                angle -= Speed * 0.01f;
                
            Entity.Transform.Position = new Vector3((float)Math.Cos(angle), 0.5f, (float)Math.Sin(angle));
        }
    }
}
