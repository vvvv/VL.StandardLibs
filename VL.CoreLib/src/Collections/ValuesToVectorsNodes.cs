using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using VL.Core;

namespace VL.Lib.Collections.Spread
{
    public static class ValuesToVectorsNodes
    {
        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static SpreadBuilder<Vector2> ValuesToVectors2D(this SpreadBuilder<Vector2> builder, IEnumerable<float> values)
        {
            var iterator = values.GetEnumerator();

            while (iterator.MoveNext())
            {
                var vector = new Vector2();

                vector.X = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.Y = iterator.Current;

                builder.Add(vector);
            }

            return builder;
        }

        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static Spread<Vector2> ValuesToVectors2D(IEnumerable<float> values)
        {
            return ValuesToVectors2D(new SpreadBuilder<Vector2>(), values).ToSpread();
        }

        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static SpreadBuilder<Vector3> ValuesToVectors3D(this SpreadBuilder<Vector3> builder, IEnumerable<float> values)
        {
            var iterator = values.GetEnumerator();

            while (iterator.MoveNext())
            {
                var vector = new Vector3();

                vector.X = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.Y = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.Z = iterator.Current;

                builder.Add(vector);
            }

            return builder;
        }

        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static Spread<Vector3> ValuesToVectors3D(IEnumerable<float> values)
        {
            return ValuesToVectors3D(new SpreadBuilder<Vector3>(), values).ToSpread();
        }

        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static SpreadBuilder<Vector4> ValuesToVectors4D(this SpreadBuilder<Vector4> builder, IEnumerable<float> values)
        {
            var iterator = values.GetEnumerator();

            while (iterator.MoveNext())
            {
                var vector = new Vector4();

                vector.X = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.Y = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.Z = iterator.Current;

                if (!iterator.MoveNext()) break;
                vector.W = iterator.Current;

                builder.Add(vector);
            }

            return builder;
        }

        /// <summary>
        /// Converts a sequence of values to vectors, if the count of the sequence is not divisible by the dimension the remainig value gets omitted
        /// </summary>
        public static Spread<Vector4> ValuesToVectors4D(IEnumerable<float> values)
        {
            return ValuesToVectors4D(new SpreadBuilder<Vector4>(), values).ToSpread();
        }

    }
}
