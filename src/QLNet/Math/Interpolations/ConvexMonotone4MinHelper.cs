﻿namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class ConvexMonotone4MinHelper : ConvexMonotone4Helper
    {
        private bool splitRegion_;
        private double xRatio_, x2_, x3_;

        public ConvexMonotone4MinHelper(double xPrev, double xNext, double gPrev, double gNext,
            double fAverage, double eta4, double prevPrimitive)
            : base(xPrev, xNext, gPrev, gNext, fAverage, eta4, prevPrimitive)
        {

            splitRegion_ = false;
            if (A_ + fAverage_ <= 0.0)
            {
                splitRegion_ = true;
                var fPrev = gPrev_ + fAverage_;
                var fNext = gNext_ + fAverage_;
                var reqdShift = (eta4_ * fPrev + (1 - eta4_) * fNext) / 3.0 - fAverage_;
                var reqdPeriod = reqdShift * xScaling_ / (fAverage_ + reqdShift);
                var xAdjust = xScaling_ - reqdPeriod;
                xRatio_ = xAdjust / xScaling_;

                fAverage_ += reqdShift;
                gNext_ = fNext - fAverage_;
                gPrev_ = fPrev - fAverage_;
                A_ = -(eta4_ * gPrev_ + (1.0 - eta4) * gNext_) / 2.0;
                x2_ = xPrev_ + xAdjust * eta4_;
                x3_ = xPrev_ + xScaling_ - xAdjust * (1.0 - eta4_);
            }
        }

        public override double value(double x)
        {
            if (!splitRegion_)
                return base.value(x);

            var xVal = (x - xPrev_) / xScaling_;
            if (x <= x2_)
            {
                xVal /= xRatio_;
                return fAverage_ + A_ + (gPrev_ - A_) * (eta4_ - xVal) * (eta4_ - xVal) / (eta4_ * eta4_);
            }
            else if (x < x3_)
            {
                return 0.0;
            }
            else
            {
                xVal = 1.0 - (1.0 - xVal) / xRatio_;
                return fAverage_ + A_ + (gNext_ - A_) * (xVal - eta4_) * (xVal - eta4_) / ((1 - eta4_) * (1 - eta4_));
            }
        }

        public override double primitive(double x)
        {
            if (!splitRegion_)
                return base.primitive(x);

            var xVal = (x - xPrev_) / xScaling_;
            if (x <= x2_)
            {
                xVal /= xRatio_;
                return prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ + A_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                    (eta4_ * eta4_ - eta4_ * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal;
            }
            else if (x <= x3_)
            {
                return prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ * eta4_ + A_ * eta4_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                    (1.0 / 3.0 * eta4_ * eta4_ * eta4_));
            }
            else
            {
                xVal = 1.0 - (1.0 - xVal) / xRatio_;
                return prevPrimitive_ + xScaling_ * xRatio_ * (fAverage_ * xVal + A_ * xVal + (gPrev_ - A_) * (1.0 / 3.0 * eta4_) +
                                                               (gNext_ - A_) / ((1.0 - eta4_) * (1.0 - eta4_)) *
                                                               (1.0 / 3.0 * xVal * xVal * xVal - eta4_ * xVal * xVal + eta4_ * eta4_ * xVal - 1.0 / 3.0 * eta4_ * eta4_ * eta4_));
            }
        }
    }
}