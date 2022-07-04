namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class EverywhereConstantHelper : ISectionHelper
    {
        private double value_;
        private double prevPrimitive_;
        private double xPrev_;

        public EverywhereConstantHelper(double value, double prevPrimitive, double xPrev)
        {
            value_ = value;
            prevPrimitive_ = prevPrimitive;
            xPrev_ = xPrev;
        }

        public double value(double x) => value_;

        public double primitive(double x) => prevPrimitive_ + (x - xPrev_) * value_;

        public double fNext() => value_;
    }
}