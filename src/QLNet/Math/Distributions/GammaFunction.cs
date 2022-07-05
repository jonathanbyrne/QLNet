namespace QLNet.Math.Distributions
{
    public static class GammaFunction
    {
        private const double c1_ = 76.18009172947146;
        private const double c2_ = -86.50532032941677;
        private const double c3_ = 24.01409824083091;
        private const double c4_ = -1.231739572450155;
        private const double c5_ = 0.1208650973866179e-2;
        private const double c6_ = -0.5395239384953e-5;

        public static double logValue(double x)
        {
            QLNet.Utils.QL_REQUIRE(x > 0.0, () => "positive argument required");

            var temp = x + 5.5;
            temp -= (x + 0.5) * System.Math.Log(temp);
            var ser = 1.000000000190015;
            ser += c1_ / (x + 1.0);
            ser += c2_ / (x + 2.0);
            ser += c3_ / (x + 3.0);
            ser += c4_ / (x + 4.0);
            ser += c5_ / (x + 5.0);
            ser += c6_ / (x + 6.0);

            return -temp + System.Math.Log(2.5066282746310005 * ser / x);
        }

        public static double value(double x)
        {
            if (x >= 1.0)
            {
                return System.Math.Exp(logValue(x));
            }

            if (x > -20.0)
            {
                return value(x + 1.0) / x;
            }

            return -Const.M_PI / (value(-x) * x * System.Math.Sin(Const.M_PI * x));
        }
    }
}
