using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Termstructures.Yield;
using QLNet.Time;

namespace QLNet.Termstructures.Credit
{
    /// <summary>
    ///     Default-density-curve traits
    /// </summary>
    [PublicAPI]
    public class DefaultDensity : ITraits<DefaultProbabilityTermStructure>
    {
        private const double avgHazardRate = 0.01;
        private const double maxHazardRate = 1.0;

        public double discountImpl(Interpolation i, double t) => i.value(t, true);

        public double forwardImpl(Interpolation i, double t) => throw new NotSupportedException();

        public double guess(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData) // previous iteration value
            {
                return c.data()[i];
            }

            if (i == 1) // first pillar
            {
                return avgHazardRate;
            }

            // extrapolate
            var d = c.dates()[i];
            return ((DefaultProbabilityTermStructure)c).defaultDensity(d, true);
        }

        public Date initialDate(DefaultProbabilityTermStructure c) => c.referenceDate();

        public double initialValue(DefaultProbabilityTermStructure c) => avgHazardRate;

        public int maxIterations() => 30;

        public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Max();
                return r * 2.0;
            }

            // no constraints.
            // We choose as max a value very unlikely to be exceeded.
            return maxHazardRate;
        }

        public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
        {
            if (validData)
            {
                var r = c.data().Min();
                return r / 2.0;
            }

            return Const.QL_EPSILON;
        }

        public void updateGuess(List<double> data, double density, int i)
        {
            data[i] = density;
            if (i == 1)
            {
                data[0] = density; // first point is updated as well
            }
        }

        public double zeroYieldImpl(Interpolation i, double t) => throw new NotSupportedException();
    }
}
