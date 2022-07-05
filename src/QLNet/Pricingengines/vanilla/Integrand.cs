using JetBrains.Annotations;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class Integrand
    {
        private double drift_;
        private Payoff payoff_;
        private double s0_;
        private double variance_;

        public Integrand(Payoff payoff, double s0, double drift, double variance)
        {
            payoff_ = payoff;
            s0_ = s0;
            drift_ = drift;
            variance_ = variance;
        }

        public double value(double x)
        {
            var temp = s0_ * System.Math.Exp(x);
            var result = payoff_.value(temp);
            return result * System.Math.Exp(-(x - drift_) * (x - drift_) / (2.0 * variance_));
        }
    }
}
