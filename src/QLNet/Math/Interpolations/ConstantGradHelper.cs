namespace QLNet.Math.Interpolations
{
    [JetBrains.Annotations.PublicAPI] public class ConstantGradHelper : ISectionHelper
    {
        private double fPrev_, prevPrimitive_, xPrev_, fGrad_, fNext_;

        public ConstantGradHelper(double fPrev, double prevPrimitive, double xPrev, double xNext, double fNext)
        {
            fPrev_ = fPrev;
            prevPrimitive_ = prevPrimitive;
            xPrev_ = xPrev;
            fGrad_ = (fNext - fPrev) / (xNext - xPrev);
            fNext_ = fNext;
        }

        public double value(double x) => fPrev_ + (x - xPrev_) * fGrad_;

        public double primitive(double x) => prevPrimitive_ + (x - xPrev_) * (fPrev_ + 0.5 * (x - xPrev_) * fGrad_);

        public double fNext() => fNext_;
    }
}