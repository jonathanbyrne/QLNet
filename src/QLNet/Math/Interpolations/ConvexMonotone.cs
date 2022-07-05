using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ConvexMonotone : IInterpolationFactory
    {
        private bool forcePositive_;
        private double quadraticity_, monotonicity_;

        public ConvexMonotone() : this(0.3, 0.7, true)
        {
        }

        public ConvexMonotone(double quadraticity, double monotonicity, bool forcePositive)
        {
            quadraticity_ = quadraticity;
            monotonicity_ = monotonicity;
            forcePositive_ = forcePositive;
        }

        public int dataSizeAdjustment => 1;

        public bool global => true;

        public int requiredPoints => 2;

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_, false);

        public Interpolation localInterpolate(List<double> xBegin, int size, List<double> yBegin, int localisation,
            ConvexMonotoneInterpolation prevInterpolation, int finalSize)
        {
            var length = size;
            if (length - localisation == 1) // the first time this
            {
                // function is called
                return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_,
                    length != finalSize);
            }

            var interp = prevInterpolation;
            return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_,
                forcePositive_, length != finalSize, interp.getExistingHelpers());
        }
    }
}
