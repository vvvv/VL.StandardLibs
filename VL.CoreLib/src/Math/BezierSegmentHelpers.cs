using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Lib.Bezier
{
    public static class BezierHelpers
    {

        // https://stackoverflow.com/questions/51879836/cubic-bezier-curves-get-y-for-given-x-special-case-where-x-of-control-points

        static bool approximately(double a, double b)
        {
            return Math.Abs(a - b) < 0.000001;
        }

        // Find the roots for a cubic polynomial with bernstein coefficients
        // {pa, pb, pc, pd}. The function will first convert those to the
        // standard polynomial coefficients, and then run through Cardano's
        // formula for finding the roots of a depressed cubic curve.
        static double[] findRoots(double x, double x0, double x1, double x2, double x3)
        {
            double
              pa = x0,
              pb = x1,
              pc = x2,
              pd = x3,
              pa3 = 3 * pa,
              pb3 = 3 * pb,
              pc3 = 3 * pc,
              a = -pa + pb3 - pc3 + pd,
              b = pa3 - 2 * pb3 + pc3,
              c = -pa3 + pb3,
              d = pa - x;

            // Fun fact: any Bezier curve may (accidentally or on purpose)
            // perfectly model any lower order curve, so we want to test 
            // for that: lower order curves are much easier to root-find.
            if (approximately(a, 0))
            {
                // this is not a cubic curve.
                if (approximately(b, 0))
                {
                    // in fact, this is not a quadratic curve either.
                    if (approximately(c, 0))
                    {
                        // in fact in fact, there are no solutions.
                        return new double[] { };
                    }
                    // linear solution:
                    return new double[] { -d / c };
                }
                // quadratic solution:
                double
                  q = Math.Sqrt(c * c - 4 * b * d),
                  b2 = 2 * b;
                return new double[]{
                  (q - c) / b2,
                  (-c - q) / b2
                };
            }

            // At this point, we know we need a cubic solution,
            // and the above a/b/c/d values were technically
            // a pre-optimized set because a might be zero and
            // that would cause the following divisions to error.
            {
                b /= a;
                c /= a;
                d /= a;

                double
                  b3 = b / 3,
                  p = (3 * c - b * b) / 3,
                  p3 = p / 3,
                  q = (2 * b * b * b - 9 * b * c + 27 * d) / 27,
                  q2 = q / 2,
                  discriminant = q2 * q2 + p3 * p3 * p3,
                  u1, v1;

                // case 1: three real roots, but finding them involves complex
                // maths. Since we don't have a complex data type, we use trig
                // instead, because complex numbers have nice geometric properties.
                if (discriminant < 0)
                {
                    double
                      mp3 = -p / 3,
                      r = Math.Sqrt(mp3 * mp3 * mp3),
                      t = -q / (2 * r),
                      cosphi = t < -1 ? -1 : t > 1 ? 1 : t,
                      phi = Math.Acos(cosphi),
                      cbrtr = Math.Cbrt(r),
                  t1 = 2 * cbrtr;
                    return new double[]{
                      t1 * Math.Cos(phi / 3) - b3,
                      t1 * Math.Cos((phi + double.Tau) / 3) - b3,
                      t1 * Math.Cos((phi + 2 * double.Tau) / 3) - b3
                  };
                }

                // case 2: three real roots, but two form a "double root",
                // and so will have the same resultant value. We only need
                // to return two values in this case.
                else if (discriminant == 0)
                {
                    u1 = q2 < 0 ? Math.Cbrt(-q2) : -Math.Cbrt(q2);
                    return new double[]{
                      2 * u1 - b3,
                      -u1 - b3
                    };
                }

                // case 3: one real root, 2 complex roots. We don't care about
                // complex results so we just ignore those and directly compute
                // that single real root.
                else
                {
                    double sd = Math.Sqrt(discriminant);
                    u1 = Math.Cbrt(-q2 + sd);
                    v1 = Math.Cbrt(q2 + sd);
                    return new double[] { u1 - v1 - b3 };
                }
            }
        }


        public static double InverseBezier(double x, double x0, double x1, double x2, double x3)
        {
            double[] roots = findRoots(x, x0, x1, x2, x3);

            if (roots.Length > 0)
            {
                foreach (double r in roots)
                {
                    if (r < 0 || r > 1) continue;
                    return r;
                }
            }

            return 0;
        }
    }
}

