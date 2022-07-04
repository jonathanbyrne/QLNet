using System.Collections.Generic;

namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class ConvexMonotone : IInterpolationFactory
    {
        private double quadraticity_, monotonicity_;
        private bool forcePositive_;

        public ConvexMonotone() : this(0.3, 0.7, true) { }
        public ConvexMonotone(double quadraticity, double monotonicity, bool forcePositive)
        {
            quadraticity_ = quadraticity;
            monotonicity_ = monotonicity;
            forcePositive_ = forcePositive;
        }

        public Interpolation interpolate(List<double> xBegin, int size, List<double> yBegin) => new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_, false);

        public Interpolation localInterpolate(List<double> xBegin, int size, List<double> yBegin, int localisation,
            ConvexMonotoneInterpolation prevInterpolation, int finalSize)
        {
            var length = size;
            if (length - localisation == 1)   // the first time this
            {
                // function is called
                return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_, forcePositive_,
                    length != finalSize);
            }

            var interp = prevInterpolation;
            return new ConvexMonotoneInterpolation(xBegin, size, yBegin, quadraticity_, monotonicity_,
                forcePositive_, length != finalSize, interp.getExistingHelpers());
        }

        public bool global => true;

        public int requiredPoints => 2;

        public int dataSizeAdjustment => 1;
    }
}