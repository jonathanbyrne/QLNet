using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class CumulativeGammaDistribution
    {
        protected double a_;

        public CumulativeGammaDistribution(double a)
        {
            a_ = a;
            Utils.QL_REQUIRE(a > 0.0, () => "invalid parameter for gamma distribution");
        }

        public double value(double x)
        {
            if (x <= 0.0)
            {
                return 0.0;
            }

            var gln = GammaFunction.logValue(a_);

            if (x < a_ + 1.0)
            {
                var ap = a_;
                var del = 1.0 / a_;
                var sum = del;
                for (var n = 1; n <= 100; n++)
                {
                    ap += 1.0;
                    del *= x / ap;
                    sum += del;
                    if (System.Math.Abs(del) < System.Math.Abs(sum) * 3.0e-7)
                    {
                        return sum * System.Math.Exp(-x + a_ * System.Math.Log(x) - gln);
                    }
                }
            }
            else
            {
                var b = x + 1.0 - a_;
                var c = double.MaxValue;
                var d = 1.0 / b;
                var h = d;
                for (var n = 1; n <= 100; n++)
                {
                    var an = -1.0 * n * (n - a_);
                    b += 2.0;
                    d = an * d + b;
                    if (System.Math.Abs(d) < Const.QL_EPSILON)
                    {
                        d = Const.QL_EPSILON;
                    }

                    c = b + an / c;
                    if (System.Math.Abs(c) < Const.QL_EPSILON)
                    {
                        c = Const.QL_EPSILON;
                    }

                    d = 1.0 / d;
                    var del = d * c;
                    h *= del;
                    if (System.Math.Abs(del - 1.0) < Const.QL_EPSILON)
                    {
                        return 1.0 - h * System.Math.Exp(-x + a_ * System.Math.Log(x) - gln);
                    }
                }
            }

            Utils.QL_FAIL("too few iterations");
            return 0.0;
        }
    }
}
