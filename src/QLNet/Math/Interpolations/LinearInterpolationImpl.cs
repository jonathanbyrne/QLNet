using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    [PublicAPI]
    public class LinearInterpolationImpl : Interpolation.templateImpl
    {
        private List<double> primitiveConst_, s_;

        public LinearInterpolationImpl(List<double> xBegin, int size, List<double> yBegin)
            : base(xBegin, size, yBegin)
        {
            primitiveConst_ = new InitializedList<double>(size_);
            s_ = new InitializedList<double>(size_);
        }

        public override double derivative(double x)
        {
            var i = locate(x);
            return s_[i];
        }

        public override double primitive(double x)
        {
            var i = locate(x);
            var dx = x - xBegin_[i];
            return primitiveConst_[i] + dx * (yBegin_[i] + 0.5 * dx * s_[i]);
        }

        public override double secondDerivative(double x) => 0.0;

        public override void update()
        {
            primitiveConst_[0] = 0.0;
            for (var i = 1; i < size_; ++i)
            {
                var dx = xBegin_[i] - xBegin_[i - 1];
                s_[i - 1] = (yBegin_[i] - yBegin_[i - 1]) / dx;
                primitiveConst_[i] = primitiveConst_[i - 1] + dx * (yBegin_[i - 1] + 0.5 * dx * s_[i - 1]);
            }
        }

        public override double value(double x)
        {
            var i = locate(x);
            var result = yBegin_[i] + (x - xBegin_[i]) * s_[i];
            return result;
        }
    }
}
