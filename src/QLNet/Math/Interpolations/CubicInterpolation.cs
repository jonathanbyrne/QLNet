/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.Interpolations
{
    //! %Cubic interpolation between discrete points.
    /*! Cubic interpolation is fully defined when the ${f_i}$ function values
        at points ${x_i}$ are supplemented with ${f_i}$ function derivative
        values.

        Different ExerciseType of first derivative approximations are implemented, both
        local and non-local. Local schemes (Fourth-order, Parabolic,
        Modified Parabolic, Fritsch-Butland, Akima, Kruger) use only $f$ values
        near $x_i$ to calculate $f_i$. Non-local schemes (Spline with different
        boundary conditions) use all ${f_i}$ values and obtain ${f_i}$ by
        solving a linear system of equations. Local schemes produce $C^1$
        interpolants, while the spline scheme generates $C^2$ interpolants.

        Hyman's monotonicity constraint filter is also implemented: it can be
        applied to all schemes to ensure that in the regions of local
        monotoniticity of the input (three successive increasing or decreasing
        values) the interpolating cubic remains monotonic. If the interpolating
        cubic is already monotonic, the Hyman filter leaves it unchanged
        preserving all its original features.

        In the case of $C^2$ interpolants the Hyman filter ensures local
        monotonicity at the expense of the second derivative of the interpolant
        which will no longer be continuous in the points where the filter has
        been applied.

        While some non-linear schemes (Modified Parabolic, Fritsch-Butland,
        Kruger) are guaranteed to be locally monotone in their original
        approximation, all other schemes must be filtered according to the
        Hyman criteria at the expense of their linearity.

        See R. L. Dougherty, A. Edelman, and J. M. Hyman,
        "Nonnegativity-, Monotonicity-, or Convexity-Preserving CubicSpline and
        Quintic Hermite Interpolation"
        Mathematics Of Computation, v. 52, n. 186, April 1989, pp. 471-494.

        \todo implement missing schemes (FourthOrder and ModifiedParabolic) and
              missing boundary conditions (Periodic and Lagrange).

        \test to be adapted from old ones.
    */

    [PublicAPI]
    public class CubicInterpolation : Interpolation
    {
        public CubicInterpolation(List<double> xBegin, int size, List<double> yBegin,
            DerivativeApprox da,
            bool monotonic,
            BoundaryCondition leftCond,
            double leftConditionValue,
            BoundaryCondition rightCond,
            double rightConditionValue)
        {
            impl_ = new CubicInterpolationImpl(xBegin, size, yBegin, da, monotonic, leftCond, leftConditionValue, rightCond,
                rightConditionValue);
            impl_.update();
        }

        public List<double> aCoefficients() => ((CubicInterpolationImpl)impl_).a_;

        public List<double> bCoefficients() => ((CubicInterpolationImpl)impl_).b_;

        public List<double> cCoefficients() => ((CubicInterpolationImpl)impl_).c_;

        #region enums

        public enum DerivativeApprox
        {
            /*! Spline approximation (non-local, non-monotone, linear[?]).
                Different boundary conditions can be used on the left and right
                boundaries: see BoundaryCondition.
            */
            Spline,

            //! Overshooting minimization 1st derivative
            SplineOM1,

            //! Overshooting minimization 2nd derivative
            SplineOM2,

            //! Fourth-order approximation (local, non-monotone, linear)
            FourthOrder,

            //! Parabolic approximation (local, non-monotone, linear)
            Parabolic,

            //! Fritsch-Butland approximation (local, monotone, non-linear)
            FritschButland,

            //! Akima approximation (local, non-monotone, non-linear)
            Akima,

            //! Kruger approximation (local, monotone, non-linear)
            Kruger,

            //! Weighted harmonic mean approximation (local, monotonic, non-linear)
            Harmonic
        }

        public enum BoundaryCondition
        {
            //! Make second(-last) point an inactive knot
            NotAKnot,

            //! Match value of end-slope
            FirstDerivative,

            //! Match value of second derivative at end
            SecondDerivative,

            //! Match first and second derivative at either end
            Periodic,
            /*! Match end-slope to the slope of the cubic that matches
                the first four data at the respective end
            */
            Lagrange
        }

        #endregion
    }

    // convenience classes

    //! %Cubic interpolation factory and traits
}
