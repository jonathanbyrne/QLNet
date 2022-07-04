using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class SvenssonFitting : FittedBondDiscountCurve.FittingMethod
    {
        public SvenssonFitting(Vector weights = null, OptimizationMethod optimizationMethod = null)
            : base(true, weights, optimizationMethod)
        { }

        public override FittedBondDiscountCurve.FittingMethod clone() => MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;

        public override int size() => 6;

        internal override double discountFunction(Vector x, double t)
        {
            var kappa = x[size() - 2];
            var kappa_1 = x[size() - 1];

            var zeroRate = x[0] + (x[1] + x[2]) *
                           (1.0 - System.Math.Exp(-kappa * t)) /
                           ((kappa + Const.QL_EPSILON) * (t + Const.QL_EPSILON)) -
                           x[2] * System.Math.Exp(-kappa * t) +
                           x[3] * ((1.0 - System.Math.Exp(-kappa_1 * t)) / ((kappa_1 + Const.QL_EPSILON) * (t + Const.QL_EPSILON)) - System.Math.Exp(-kappa_1 * t));
            var d = System.Math.Exp(-zeroRate * t);
            return d;
        }

    }
}