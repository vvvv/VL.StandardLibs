using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using VL.Core;
using System.Globalization;

namespace VL.Lib.Mathematics
{
    /// <summary>
    /// A circle with center and radius
    /// </summary>
    public struct Circle
    {
        public Vector2 Center;

        public float Radius;

        public float RadiusSquared
        {
            get
            {
                return Radius * Radius;
            }
        }

        public static Circle Empty;

        static Circle()
        {
            Empty = new Circle();
        }

        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Center:{0} Radius:{1}", Center, Radius);
        }
        
        #region Equals and GetHashCode implementation
        public override bool Equals(object obj)
        {
            return (obj is Circle) && Equals((Circle)obj);
        }

        
        public bool Equals(Circle other)
        {
            return MathUtil.NearEqual(other.Radius, Radius) &&
                   MathUtil.NearEqual(other.Center.X, Center.X) &&
                   MathUtil.NearEqual(other.Center.Y, Center.Y);
        }

        public override int GetHashCode()
        {
            unchecked 
            {
                int result = Center.GetHashCode();
                result = (result * 397) ^ Radius.GetHashCode();
                return result;
            }
            
        }

        public static bool operator ==(Circle lhs, Circle rhs) {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Circle lhs, Circle rhs) {
            return !(lhs == rhs);
        }

        #endregion
    }
}
