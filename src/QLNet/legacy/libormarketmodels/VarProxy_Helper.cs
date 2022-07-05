using JetBrains.Annotations;

namespace QLNet.legacy.libormarketmodels
{
    [PublicAPI]
    public class VarProxy_Helper
    {
        private int i_, j_;

        public VarProxy_Helper(LfmCovarianceProxy proxy, int i, int j)
        {
            i_ = i;
            j_ = j;
            volaModel_ = proxy.volaModel_;
            corrModel_ = proxy.corrModel_;
        }

        public LmCorrelationModel corrModel_ { get; set; }

        public LmVolatilityModel volaModel_ { get; set; }

        public double value(double t)
        {
            double v1, v2;
            if (i_ == j_)
            {
                v1 = v2 = volaModel_.volatility(i_, t);
            }
            else
            {
                v1 = volaModel_.volatility(i_, t);
                v2 = volaModel_.volatility(j_, t);
            }

            return v1 * corrModel_.correlation(i_, j_, t) * v2;
        }
    }
}
