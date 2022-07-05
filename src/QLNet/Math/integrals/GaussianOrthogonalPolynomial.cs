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

namespace QLNet.Math.integrals
{
    //! orthogonal polynomial for Gaussian quadratures
    /*! References:
        Gauss quadratures and orthogonal polynomials

        G.H. Gloub and J.H. Welsch: Calculation of Gauss quadrature rule.
        Math. Comput. 23 (1986), 221-230

        "Numerical Recipes in C", 2nd edition,
        Press, Teukolsky, Vetterling, Flannery,

        The polynomials are defined by the three-term recurrence relation

    */
    public abstract class GaussianOrthogonalPolynomial
    {
        public abstract double alpha(int i);

        public abstract double beta(int i);

        public abstract double mu_0();

        public abstract double w(double x);

        public double value(int n, double x)
        {
            if (n > 1)
            {
                return (x - alpha(n - 1)) * value(n - 1, x)
                       - beta(n - 1) * value(n - 2, x);
            }

            if (n == 1)
            {
                return x - alpha(0);
            }

            return 1;
        }

        public double weightedValue(int n, double x) => System.Math.Sqrt(w(x)) * value(n, x);
    }

    //! Gauss-Laguerre polynomial

    //! Gauss-Hermite polynomial

    //! Gauss-Jacobi polynomial

    //! Gauss-Legendre polynomial

    //! Gauss-Chebyshev polynomial

    //! Gauss-Chebyshev polynomial (second kind)

    //! Gauss-Gegenbauer polynomial

    //! Gauss hyperbolic polynomial
}
