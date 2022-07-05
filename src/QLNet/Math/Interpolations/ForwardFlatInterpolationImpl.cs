using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class ForwardFlatInterpolationImpl : Interpolation.templateImpl
    {
        private List<double> primitive_;

        public ForwardFlatInterpolationImpl(List<double> xBegin, int size, List<double> yBegin) : base(xBegin, size, yBegin)
        {
            primitive_ = new InitializedList<double>(size_);
        }

        public override double derivative(double x) => 0.0;

        public override double primitive(double x)
        {
            var i = locate(x);
            var dx = x - xBegin_[i];
            return primitive_[i] + dx * yBegin_[i];
        }

        public override double secondDerivative(double x) => 0.0;

        public override void update()
        {
            primitive_[0] = 0.0;
            for (var i = 1; i < size_; i++)
            {
                var dx = xBegin_[i] - xBegin_[i - 1];
                primitive_[i] = primitive_[i - 1] + dx * yBegin_[i - 1];
            }
        }

        public override double value(double x)
        {
            if (x >= xBegin_[size_ - 1])
            {
                return yBegin_[size_ - 1];
            }

            var i = locate(x);
            return yBegin_[i];
        }
    }
}
