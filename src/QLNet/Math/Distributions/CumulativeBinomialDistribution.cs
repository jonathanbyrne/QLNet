using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class CumulativeBinomialDistribution
    {
        private int n_;
        private double p_;

        public CumulativeBinomialDistribution(double p, int n)
        {
            n_ = n;
            p_ = p;

            QLNet.Utils.QL_REQUIRE(p >= 0, () => "negative p not allowed");
            QLNet.Utils.QL_REQUIRE(p <= 1.0, () => "p>1.0 not allowed");
        }

        // function
        public double value(long k)
        {
            if (k >= n_)
            {
                return 1.0;
            }

            return 1.0 - Math.Utils.incompleteBetaFunction(k + 1, n_ - k, p_);
        }
    }
}
