namespace QLNet.Termstructures.Volatility
{
    [JetBrains.Annotations.PublicAPI] public class AbcdSquared
    {
        public AbcdSquared(double a, double b, double c, double d, double T, double S)
        {
            abcd_ = new AbcdFunction(a, b, c, d);
            T_ = T;
            S_ = S;
        }

        public double value(double t) => abcd_.covariance(t, T_, S_);

        private AbcdFunction abcd_;
        private double T_, S_;
    }
}