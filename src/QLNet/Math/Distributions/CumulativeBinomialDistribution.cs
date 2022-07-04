namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class CumulativeBinomialDistribution
    {
        private int n_;
        private double p_;

        public CumulativeBinomialDistribution(double p, int n)
        {
            n_ = n;
            p_ = p;

            Utils.QL_REQUIRE(p >= 0, () => "negative p not allowed");
            Utils.QL_REQUIRE(p <= 1.0, () => "p>1.0 not allowed");
        }

        // function
        public double value(long k)
        {
            if (k >= n_)
                return 1.0;
            return 1.0 - Utils.incompleteBetaFunction(k + 1, n_ - k, p_);
        }
    }
}