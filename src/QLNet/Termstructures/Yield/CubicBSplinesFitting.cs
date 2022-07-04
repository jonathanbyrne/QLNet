using System.Collections.Generic;
using QLNet.Math;
using QLNet.Math.Optimization;

namespace QLNet.Termstructures.Yield
{
    [JetBrains.Annotations.PublicAPI] public class CubicBSplinesFitting : FittedBondDiscountCurve.FittingMethod
    {
        public CubicBSplinesFitting(List<double> knots, bool constrainAtZero = true, Vector weights = null,
            OptimizationMethod optimizationMethod = null)
            : base(constrainAtZero, weights, optimizationMethod)
        {
            splines_ = new BSpline(3, knots.Count - 5, knots);

            Utils.QL_REQUIRE(knots.Count >= 8, () => "At least 8 knots are required");
            var basisFunctions = knots.Count - 4;

            if (constrainAtZero)
            {
                size_ = basisFunctions - 1;

                // Note: A small but nonzero N_th basis function at t=0 may
                // lead to an ill conditioned problem
                N_ = 1;

                Utils.QL_REQUIRE(System.Math.Abs(splines_.value(N_, 0.0)) > Const.QL_EPSILON, () =>
                    "N_th cubic B-spline must be nonzero at t=0");
            }
            else
            {
                size_ = basisFunctions;
                N_ = 0;
            }

        }

        //! cubic B-spline basis functions
        public double basisFunction(int i, double t) => splines_.value(i, t);

        public override FittedBondDiscountCurve.FittingMethod clone() => MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;

        public override int size() => size_;

        internal override double discountFunction(Vector x, double t)
        {
            var d = 0.0;

            if (!constrainAtZero_)
            {
                for (var i = 0; i < size_; ++i)
                {
                    d += x[i] * splines_.value(i, t);
                }
            }
            else
            {
                var T = 0.0;
                var sum = 0.0;
                for (var i = 0; i < size_; ++i)
                {
                    if (i < N_)
                    {
                        d += x[i] * splines_.value(i, t);
                        sum += x[i] * splines_.value(i, T);
                    }
                    else
                    {
                        d += x[i] * splines_.value(i + 1, t);
                        sum += x[i] * splines_.value(i + 1, T);
                    }
                }
                var coeff = 1.0 - sum;
                coeff /= splines_.value(N_, T);
                d += coeff * splines_.value(N_, t);
            }

            return d;

        }

        private BSpline splines_;
        private int size_;
        //! N_th basis function coefficient to solve for when d(0)=1
        private int N_;
    }
}