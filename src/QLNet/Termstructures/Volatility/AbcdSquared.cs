using JetBrains.Annotations;

namespace QLNet.Termstructures.Volatility
{
    [PublicAPI]
    public class AbcdSquared
    {
        private AbcdFunction abcd_;
        private double T_, S_;

        public AbcdSquared(double a, double b, double c, double d, double T, double S)
        {
            abcd_ = new AbcdFunction(a, b, c, d);
            T_ = T;
            S_ = S;
        }

        public double value(double t) => abcd_.covariance(t, T_, S_);
    }
}
