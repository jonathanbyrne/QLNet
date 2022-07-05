using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class MoroInverseCumulativeNormal : IValue
    {
        private const double a0_ = 2.50662823884;
        private const double a1_ = -18.61500062529;
        private const double a2_ = 41.39119773534;
        private const double a3_ = -25.44106049637;
        private const double b0_ = -8.47351093090;
        private const double b1_ = 23.08336743743;
        private const double b2_ = -21.06224101826;
        private const double b3_ = 3.13082909833;
        private const double c0_ = 0.3374754822726147;
        private const double c1_ = 0.9761690190917186;
        private const double c2_ = 0.1607979714918209;
        private const double c3_ = 0.0276438810333863;
        private const double c4_ = 0.0038405729373609;
        private const double c5_ = 0.0003951896511919;
        private const double c6_ = 0.0000321767881768;
        private const double c7_ = 0.0000002888167364;
        private const double c8_ = 0.0000003960315187;
        private double average_, sigma_;

        public MoroInverseCumulativeNormal(double average, double sigma)
        {
            average_ = average;
            sigma_ = sigma;

            QLNet.Utils.QL_REQUIRE(sigma_ > 0.0, () => "sigma must be greater than 0.0 (" + sigma_ + " not allowed)");
        }

        // function
        public double value(double x)
        {
            QLNet.Utils.QL_REQUIRE(x > 0.0 && x < 1.0, () => "MoroInverseCumulativeNormal(" + x + ") undefined: must be 0<x<1");

            double result;
            var temp = x - 0.5;

            if (System.Math.Abs(temp) < 0.42)
            {
                // Beasley and Springer, 1977
                result = temp * temp;
                result = temp *
                         (((a3_ * result + a2_) * result + a1_) * result + a0_) /
                         ((((b3_ * result + b2_) * result + b1_) * result + b0_) * result + 1.0);
            }
            else
            {
                // improved approximation for the tail (Moro 1995)
                if (x < 0.5)
                {
                    result = x;
                }
                else
                {
                    result = 1.0 - x;
                }

                result = System.Math.Log(-System.Math.Log(result));
                result = c0_ + result * (c1_ + result * (c2_ + result * (c3_ + result *
                    (c4_ + result * (c5_ + result * (c6_ + result *
                        (c7_ + result * c8_)))))));
                if (x < 0.5)
                {
                    result = -result;
                }
            }

            return average_ + result * sigma_;
        }
    }
}
