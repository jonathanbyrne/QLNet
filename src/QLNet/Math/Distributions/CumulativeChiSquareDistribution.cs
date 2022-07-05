using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class CumulativeChiSquareDistribution
    {
        private double df_;

        public CumulativeChiSquareDistribution(double df)
        {
            df_ = df;
        }

        public double value(double x) => new CumulativeGammaDistribution(0.5 * df_).value(0.5 * x);
    }
}
