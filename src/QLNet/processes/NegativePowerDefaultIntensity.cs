using JetBrains.Annotations;

namespace QLNet.Processes
{
    [PublicAPI]
    public class NegativePowerDefaultIntensity : Defaultable
    {
        private double alpha_;
        private double p_;
        private double recovery_;

        public NegativePowerDefaultIntensity(double alpha, double p) : this(alpha, p, 0.0)
        {
        }

        public NegativePowerDefaultIntensity(double alpha, double p, double recovery)
        {
            alpha_ = alpha;
            p_ = p;
            recovery_ = recovery;
        }

        public override double defaultRecovery(double t, double s) => recovery_;

        public override double hazardRate(double t, double s)
        {
            if (s <= 0.0)
            {
                return 0.0;
            }

            return alpha_ * System.Math.Pow(s, -p_);
        }
    }
}
