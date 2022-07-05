/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System;
using JetBrains.Annotations;
using QLNet.Math.matrixutilities;

namespace QLNet.Math.integrals
{
    //! Integral of a 1-dimensional function using the Gauss quadratures method
    /*! References:
       Gauss quadratures and orthogonal polynomials

       G.H. Gloub and J.H. Welsch: Calculation of Gauss quadrature rule.
       Math. Comput. 23 (1986), 221-230

       "Numerical Recipes in C", 2nd edition,
       Press, Teukolsky, Vetterling, Flannery,

       \test the correctness of the result is tested by checking it
             against known good values.
    */
    [PublicAPI]
    public class GaussianQuadrature
    {
        private Vector x_, w_;

        public GaussianQuadrature(int n, GaussianOrthogonalPolynomial orthPoly)
        {
            x_ = new Vector(n);
            w_ = new Vector(n);

            // set-up matrix to compute the roots and the weights
            var e = new Vector(n - 1);

            int i;
            for (i = 1; i < n; ++i)
            {
                x_[i] = orthPoly.alpha(i);
                e[i - 1] = System.Math.Sqrt(orthPoly.beta(i));
            }

            x_[0] = orthPoly.alpha(0);

            var tqr = new TqrEigenDecomposition(x_, e,
                TqrEigenDecomposition.EigenVectorCalculation.OnlyFirstRowEigenVector,
                TqrEigenDecomposition.ShiftStrategy.Overrelaxation);

            x_ = tqr.eigenvalues();
            var ev = tqr.eigenvectors();

            var mu_0 = orthPoly.mu_0();
            for (i = 0; i < n; ++i)
            {
                w_[i] = mu_0 * ev[0, i] * ev[0, i] / orthPoly.w(x_[i]);
            }
        }

        public int order() => x_.size();

        public double value(Func<double, double> f)
        {
            var sum = 0.0;
            for (var i = order() - 1; i >= 0; --i)
            {
                sum += w_[i] * f(x_[i]);
            }

            return sum;
        }

        public Vector weights() => w_;

        public Vector x() => x_;
    }

    //! generalized Gauss-Laguerre integration
    // This class performs a 1-dimensional Gauss-Laguerre integration.

    //! generalized Gauss-Hermite integration
    // This class performs a 1-dimensional Gauss-Hermite integration.

    //! Gauss-Jacobi integration
    // This class performs a 1-dimensional Gauss-Jacobi integration.

    //! Gauss-Hyperbolic integration
    // This class performs a 1-dimensional Gauss-Hyperbolic integration.

    //! Gauss-Legendre integration
    // This class performs a 1-dimensional Gauss-Legendre integration.

    //! Gauss-Chebyshev integration
    // This class performs a 1-dimensional Gauss-Chebyshev integration.

    //! Gauss-Chebyshev integration (second kind)
    // This class performs a 1-dimensional Gauss-Chebyshev integration.

    //! Gauss-Gegenbauer integration
    // This class performs a 1-dimensional Gauss-Gegenbauer integration.

    //! tabulated Gauss-Legendre quadratures
}
