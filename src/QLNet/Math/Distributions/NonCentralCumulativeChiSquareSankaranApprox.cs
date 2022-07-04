namespace QLNet.Math.Distributions
{
    [JetBrains.Annotations.PublicAPI] public class NonCentralCumulativeChiSquareSankaranApprox
    {
        protected double df_, ncp_;

        public NonCentralCumulativeChiSquareSankaranApprox(double df, double ncp)
        {
            df_ = df;
            ncp_ = ncp;
        }

        double value(double x)
        {
            var h = 1 - 2 * (df_ + ncp_) * (df_ + 3 * ncp_) / (3 * System.Math.Pow(df_ + 2 * ncp_, 2));
            var p = (df_ + 2 * ncp_) / System.Math.Pow(df_ + ncp_, 2);
            var m = (h - 1) * (1 - 3 * h);

            var u = (System.Math.Pow(x / (df_ + ncp_), h) - (1 + h * p * (h - 1 - 0.5 * (2 - h) * m * p))) /
                    (h * System.Math.Sqrt(2 * p) * (1 + 0.5 * m * p));

            return new CumulativeNormalDistribution().value(u);
        }
    }
}