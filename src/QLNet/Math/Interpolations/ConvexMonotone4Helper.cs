﻿using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ConvexMonotone4Helper : ISectionHelper
    {
        protected double A_;
        protected double xPrev_, xScaling_, gPrev_, gNext_, fAverage_, eta4_, prevPrimitive_;

        public ConvexMonotone4Helper(double xPrev, double xNext, double gPrev, double gNext,
            double fAverage, double eta4, double prevPrimitive)
        {
            xPrev_ = xPrev;
            xScaling_ = xNext - xPrev;
            gPrev_ = gPrev;
            gNext_ = gNext;
            fAverage_ = fAverage;
            eta4_ = eta4;
            prevPrimitive_ = prevPrimitive;
            A_ = -0.5 * (eta4_ * gPrev_ + (1 - eta4_) * gNext_);
        }

        public double fNext() => fAverage_ + gNext_;

        public virtual double primitive(double x)
        {
            var xVal = (x - xPrev_) / xScaling_;
            double retVal;
            if (xVal <= eta4_)
            {
                retVal = prevPrimitive_ + xScaling_ * (fAverage_ + A_ + (gPrev_ - A_) / (eta4_ * eta4_) *
                    (eta4_ * eta4_ - eta4_ * xVal + 1.0 / 3.0 * xVal * xVal)) * xVal;
            }
            else
            {
                retVal = prevPrimitive_ + xScaling_ * (fAverage_ * xVal + A_ * xVal + (gPrev_ - A_) * (1.0 / 3.0 * eta4_) +
                                                       (gNext_ - A_) / ((1 - eta4_) * (1 - eta4_)) *
                                                       (1.0 / 3.0 * xVal * xVal * xVal - eta4_ * xVal * xVal + eta4_ * eta4_ * xVal - 1.0 / 3.0 * eta4_ * eta4_ * eta4_));
            }

            return retVal;
        }

        public virtual double value(double x)
        {
            var xVal = (x - xPrev_) / xScaling_;
            if (xVal <= eta4_)
            {
                return fAverage_ + A_ + (gPrev_ - A_) * (eta4_ - xVal) * (eta4_ - xVal) / (eta4_ * eta4_);
            }

            return fAverage_ + A_ + (gNext_ - A_) * (xVal - eta4_) * (xVal - eta4_) / ((1 - eta4_) * (1 - eta4_));
        }
    }
}
