using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class EverywhereConstantHelper : ISectionHelper
    {
        private double prevPrimitive_;
        private double value_;
        private double xPrev_;

        public EverywhereConstantHelper(double value, double prevPrimitive, double xPrev)
        {
            value_ = value;
            prevPrimitive_ = prevPrimitive;
            xPrev_ = xPrev;
        }

        public double fNext() => value_;

        public double primitive(double x) => prevPrimitive_ + (x - xPrev_) * value_;

        public double value(double x) => value_;
    }
}
