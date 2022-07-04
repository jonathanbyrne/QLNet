using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class SimplePolynomialFitting : FittedBondDiscountCurve.FittingMethod
    {
        public SimplePolynomialFitting(int degree,
            bool constrainAtZero = true,
            Vector weights = null,
            OptimizationMethod optimizationMethod = null)
            : base(constrainAtZero, weights, optimizationMethod)
        {
            size_ = constrainAtZero ? degree : degree + 1;
        }
        public override FittedBondDiscountCurve.FittingMethod clone() => MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;

        public override int size() => size_;

        internal override double discountFunction(Vector x, double t)
        {
            var d = 0.0;

            if (!constrainAtZero_)
            {
                for (var i = 0; i < size_; ++i)
                    d += x[i] * BernsteinPolynomial.get((uint)i, (uint)i, t);
            }
            else
            {
                d = 1.0;
                for (var i = 0; i < size_; ++i)
                    d += x[i] * BernsteinPolynomial.get((uint)i + 1, (uint)i + 1, t);
            }
            return d;
        }

        private int size_;
    }
}