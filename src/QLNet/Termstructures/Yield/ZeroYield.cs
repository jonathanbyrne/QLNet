using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class ZeroYield : ITraits<YieldTermStructure>
    {
        private const double avgRate = 0.05;
        private const double maxRate = 3;

        public double discountImpl(Interpolation i, double t)
        {
            var r = zeroYieldImpl(i, t);
            return System.Math.Exp(-r * t);
        }

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData) // previous iteration value
            {
                return c.data()[i];
            }

            if (i == 1) // first pillar
            {
                return avgRate;
            }

            // extrapolate
            return zeroYieldImpl(c.interpolation_, c.times()[i]);
        }

        public Date initialDate(YieldTermStructure c) => c.referenceDate(); // start of curve data

        public double initialValue(YieldTermStructure c) => avgRate; // value at reference date

        public int maxIterations() => 30; // upper bound for convergence loop

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

            return maxRate;
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

        // update with new guess
        public void updateGuess(List<double> data, double rate, int i)
        {
            data[i] = rate;
            if (i == 1)
            {
                data[0] = rate; // first point is updated as well
            }
        }

        public double zeroYieldImpl(Interpolation i, double t) => i.value(t, true);
    }
}
