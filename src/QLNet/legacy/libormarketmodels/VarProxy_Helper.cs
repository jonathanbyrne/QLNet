namespace QLNet.legacy.libormarketmodels
{
    [JetBrains.Annotations.PublicAPI] public class VarProxy_Helper
    {
        private int i_, j_;
        public LmVolatilityModel volaModel_ { get; set; }
        public LmCorrelationModel corrModel_ { get; set; }

        public VarProxy_Helper(LfmCovarianceProxy proxy, int i, int j)
        {
            i_ = i;
            j_ = j;
            volaModel_ = proxy.volaModel_;
            corrModel_ = proxy.corrModel_;
        }

        public double value(double t)
        {
            double v1, v2;
            if (i_ == j_)
            {
                v1 = v2 = volaModel_.volatility(i_, t, null);
            }
            else
            {
                v1 = volaModel_.volatility(i_, t, null);
                v2 = volaModel_.volatility(j_, t, null);
            }
            return v1 * corrModel_.correlation(i_, j_, t, null) * v2;
        }
    }
}