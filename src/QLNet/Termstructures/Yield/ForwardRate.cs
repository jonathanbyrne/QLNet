using System.Collections.Generic;
using System.Linq;
using QLNet.Extensions;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class ForwardRate : ITraits<YieldTermStructure>
    {
        const double maxRate = 3;
        const double avgRate = 0.05;

        public Date initialDate(YieldTermStructure c) => c.referenceDate(); // start of curve data
        public double initialValue(YieldTermStructure c) => avgRate; // dummy value at reference date
        // update with new guess
        public void updateGuess(List<double> data, double forward, int i)
        {
            data[i] = forward;
            if (i == 1)
                data[0] = forward; // first point is updated as well
        }
        // upper bound for convergence loop
        public int maxIterations() => 30;

        public double discountImpl(Interpolation i, double t)
        {
            var r = zeroYieldImpl(i, t);
            return System.Math.Exp(-r * t);
        }
        public double zeroYieldImpl(Interpolation i, double t)
        {
            if (t.IsEqual(0.0))
                return forwardImpl(i, 0.0);
            else
                return i.primitive(t, true) / t;
        }
        public double forwardImpl(Interpolation i, double t) => i.value(t, true);

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)   // previous iteration value
                return c.data()[i];

            if (i == 1)   // first pillar
                return avgRate;

            // extrapolate
            return forwardImpl(c.interpolation_, c.times()[i]);
        }

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Min();
#if QL_NEGATIVE_RATES
                return r < 0.0 ? r * 2.0 : r / 2.0;
#else
            return r / 2.0;
#endif
            }
#if QL_NEGATIVE_RATES
            // no constraints.
            // We choose as min a value very unlikely to be exceeded.
            return -maxRate;
#else
         return Const.QL_EPSILON;
#endif
        }

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Max();
#if QL_NEGATIVE_RATES

                return r < 0.0 ? r / 2.0 : r * 2.0;
#else
            return r * 2.0;
#endif
            }
            // no constraints.
            // We choose as max a value very unlikely to be exceeded.
            return maxRate;

        }
    }
}