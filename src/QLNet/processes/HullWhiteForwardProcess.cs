using JetBrains.Annotations;
using QLNet.Termstructures;

namespace QLNet.Processes
{
    [PublicAPI]
    public class HullWhiteForwardProcess : ForwardMeasureProcess1D
    {
        protected double a_, sigma_;
        protected Handle<YieldTermStructure> h_;
        protected OrnsteinUhlenbeckProcess process_;

        public HullWhiteForwardProcess(Handle<YieldTermStructure> h, double a, double sigma)
        {
            process_ = new OrnsteinUhlenbeckProcess(a, sigma, h.link.forwardRate(0.0, 0.0,
                Compounding.Continuous, Frequency.NoFrequency).value());
            h_ = h;
            a_ = a;
            sigma_ = sigma;
        }

        public double a() => a_;

        public double alpha(double t)
        {
            var alfa = a_ > Const.QL_EPSILON ? sigma_ / a_ * (1 - System.Math.Exp(-a_ * t)) : sigma_ * t;
            alfa *= 0.5 * alfa;
            alfa += h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();

            return alfa;
        }

        public double B(double t, double T) =>
            a_ > Const.QL_EPSILON ? 1 / a_ * (1 - System.Math.Exp(-a_ * (T - t))) : T - t;

        public override double diffusion(double t, double x) => process_.diffusion(t, x);

        public override double drift(double t, double x)
        {
            var alpha_drift = sigma_ * sigma_ / (2 * a_) * (1 - System.Math.Exp(-2 * a_ * t));
            var shift = 0.0001;
            var f = h_.link.forwardRate(t, t, Compounding.Continuous, Frequency.NoFrequency).value();
            var fup = h_.link.forwardRate(t + shift, t + shift, Compounding.Continuous, Frequency.NoFrequency).value();
            var f_prime = (fup - f) / shift;
            alpha_drift += a_ * f + f_prime;
            return process_.drift(t, x) + alpha_drift - B(t, T_) * sigma_ * sigma_;
        }

        public override double expectation(double t0, double x0, double dt) =>
            process_.expectation(t0, x0, dt)
            + alpha(t0 + dt) - alpha(t0) * System.Math.Exp(-a_ * dt)
                             - M_T(t0, t0 + dt, T_);

        public double M_T(double s, double t, double T)
        {
            if (a_ > Const.QL_EPSILON)
            {
                var coeff = sigma_ * sigma_ / (a_ * a_);
                var exp1 = System.Math.Exp(-a_ * (t - s));
                var exp2 = System.Math.Exp(-a_ * (T - t));
                var exp3 = System.Math.Exp(-a_ * (T + t - 2.0 * s));
                return coeff * (1 - exp1) - 0.5 * coeff * (exp2 - exp3);
            }
            else
            {
                // low-a algebraic limit
                var coeff = sigma_ * sigma_ / 2.0;
                return coeff * (t - s) * (2.0 * T - t - s);
            }
        }

        public double sigma() => sigma_;

        public override double stdDeviation(double t0, double x0, double dt) => process_.stdDeviation(t0, x0, dt);

        public override double variance(double t0, double x0, double dt) => process_.variance(t0, x0, dt);

        // StochasticProcess1D interface
        public override double x0() => process_.x0();
    }
}
