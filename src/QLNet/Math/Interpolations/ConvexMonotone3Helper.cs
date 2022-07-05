using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ConvexMonotone3Helper : ISectionHelper
    {
        private double xPrev_, xScaling_, gPrev_, gNext_, fAverage_, eta3_, prevPrimitive_;

        public ConvexMonotone3Helper(double xPrev, double xNext,
            double gPrev, double gNext,
            double fAverage, double eta3,
            double prevPrimitive)
        {
            xPrev_ = xPrev;
            xScaling_ = xNext - xPrev;
            gPrev_ = gPrev;
            gNext_ = gNext;
            fAverage_ = fAverage;
            eta3_ = eta3;
            prevPrimitive_ = prevPrimitive;
        }

        public double fNext() => fAverage_ + gNext_;

        public double primitive(double x)
        {
            var xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta3_)
            {
                return prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gNext_ * xVal + (gPrev_ - gNext_) / (eta3_ * eta3_) *
                    (1.0 / 3.0 * xVal * xVal * xVal - eta3_ * xVal * xVal + eta3_ * eta3_ * xVal));
            }

            return prevPrimitive_ + xScaling_ * (fAverage_ * xVal + gNext_ * xVal + (gPrev_ - gNext_) / (eta3_ * eta3_) *
                (1.0 / 3.0 * eta3_ * eta3_ * eta3_));
        }

        public double value(double x)
        {
            var xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta3_)
            {
                return fAverage_ + gNext_ + (gPrev_ - gNext_) / (eta3_ * eta3_) * (eta3_ - xVal) * (eta3_ - xVal);
            }

            return fAverage_ + gNext_;
        }
    }
}
