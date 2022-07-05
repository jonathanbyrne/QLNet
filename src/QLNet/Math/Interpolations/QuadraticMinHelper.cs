using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class QuadraticMinHelper : ISectionHelper
    {
        private double a_, b_, c_;
        private double fAverage_, fPrev_, fNext_, xScaling_, xRatio_;
        private double primitive1_, primitive2_;
        private bool splitRegion_;
        private double x1_, x2_, x3_, x4_;

        public QuadraticMinHelper(double xPrev, double xNext, double fPrev, double fNext, double fAverage, double prevPrimitive)
        {
            splitRegion_ = false;
            x1_ = xPrev;
            x4_ = xNext;
            primitive1_ = prevPrimitive;
            fAverage_ = fAverage;
            fPrev_ = fPrev;
            fNext_ = fNext;
            a_ = 3 * fPrev_ + 3 * fNext_ - 6 * fAverage_;
            b_ = -(4 * fPrev_ + 2 * fNext_ - 6 * fAverage_);
            c_ = fPrev_;
            var d = b_ * b_ - 4 * a_ * c_;
            xScaling_ = x4_ - x1_;
            xRatio_ = 1.0;
            if (d > 0)
            {
                double aAv = 36;
                var bAv = -24 * (fPrev_ + fNext_);
                var cAv = 4 * (fPrev_ * fPrev_ + fPrev_ * fNext_ + fNext_ * fNext_);
                var dAv = bAv * bAv - 4.0 * aAv * cAv;
                if (dAv >= 0.0)
                {
                    splitRegion_ = true;
                    var avRoot = (-bAv - System.Math.Sqrt(dAv)) / (2 * aAv);

                    xRatio_ = fAverage_ / avRoot;
                    xScaling_ *= xRatio_;

                    a_ = 3 * fPrev_ + 3 * fNext_ - 6 * avRoot;
                    b_ = -(4 * fPrev_ + 2 * fNext_ - 6 * avRoot);
                    c_ = fPrev_;
                    var xRoot = -b_ / (2 * a_);
                    x2_ = x1_ + xRatio_ * (x4_ - x1_) * xRoot;
                    x3_ = x4_ - xRatio_ * (x4_ - x1_) * (1 - xRoot);
                    primitive2_ =
                        primitive1_ + xScaling_ * (a_ / 3 * xRoot * xRoot + b_ / 2 * xRoot + c_) * xRoot;
                }
            }
        }

        public double fNext() => fNext_;

        public double primitive(double x)
        {
            var xVal = (x - x1_) / (x4_ - x1_);
            if (splitRegion_)
            {
                if (x < x2_)
                {
                    xVal /= xRatio_;
                }
                else if (x < x3_)
                {
                    return primitive2_;
                }
                else
                {
                    xVal = 1.0 - (1.0 - xVal) / xRatio_;
                }
            }

            return primitive1_ + xScaling_ * (a_ / 3 * xVal * xVal + b_ / 2 * xVal + c_) * xVal;
        }

        public double value(double x)
        {
            var xVal = (x - x1_) / (x4_ - x1_);
            if (splitRegion_)
            {
                if (x <= x2_)
                {
                    xVal /= xRatio_;
                }
                else if (x < x3_)
                {
                    return 0.0;
                }
                else
                {
                    xVal = 1.0 - (1.0 - xVal) / xRatio_;
                }
            }

            return c_ + b_ * xVal + a_ * xVal * xVal;
        }
    }
}
