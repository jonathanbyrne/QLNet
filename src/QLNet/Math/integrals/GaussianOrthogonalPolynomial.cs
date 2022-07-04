﻿/*
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
using QLNet.Extensions;
using QLNet.Math.Distributions;
using System;

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
        public abstract double mu_0();
        public abstract double alpha(int i);
        public abstract double beta(int i);
        public abstract double w(double x);

        public double value(int n, double x)
        {
            if (n > 1)
            {
                return (x - alpha(n - 1)) * value(n - 1, x)
                       - beta(n - 1) * value(n - 2, x);
            }
            else if (n == 1)
            {
                return x - alpha(0);
            }
            return 1;
        }

        public double weightedValue(int n, double x) => System.Math.Sqrt(w(x)) * value(n, x);
    }

    //! Gauss-Laguerre polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussLaguerrePolynomial : GaussianOrthogonalPolynomial
    {
        private double s_;

        public GaussLaguerrePolynomial() : this(0.0) { }
        public GaussLaguerrePolynomial(double s)
        {
            s_ = s;
            Utils.QL_REQUIRE(s > -1.0, () => "s must be bigger than -1");
        }

        public override double mu_0() => System.Math.Exp(GammaFunction.logValue(s_ + 1));

        public override double alpha(int i) => 2 * i + 1 + s_;

        public override double beta(int i) => i * (i + s_);

        public override double w(double x) => System.Math.Pow(x, s_) * System.Math.Exp(-x);
    }

    //! Gauss-Hermite polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussHermitePolynomial : GaussianOrthogonalPolynomial
    {
        private double mu_;

        public GaussHermitePolynomial() : this(0.0) { }
        public GaussHermitePolynomial(double mu)
        {
            mu_ = mu;
            Utils.QL_REQUIRE(mu > -0.5, () => "mu must be bigger than -0.5");
        }

        public override double mu_0() => System.Math.Exp(GammaFunction.logValue(mu_ + 0.5));

        public override double alpha(int i) => 0.0;

        public override double beta(int i) => i % 2 != 0 ? i / 2.0 + mu_ : i / 2.0;

        public override double w(double x) => System.Math.Pow(System.Math.Abs(x), 2 * mu_) * System.Math.Exp(-x * x);
    }

    //! Gauss-Jacobi polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussJacobiPolynomial : GaussianOrthogonalPolynomial
    {
        private double alpha_;
        private double beta_;

        public GaussJacobiPolynomial(double alpha, double beta)
        {
            alpha_ = alpha;
            beta_ = beta;

            Utils.QL_REQUIRE(alpha_ + beta_ > -2.0, () => "alpha+beta must be bigger than -2");
            Utils.QL_REQUIRE(alpha_ > -1.0, () => "alpha must be bigger than -1");
            Utils.QL_REQUIRE(beta_ > -1.0, () => "beta  must be bigger than -1");
        }

        public override double mu_0() =>
            System.Math.Pow(2.0, alpha_ + beta_ + 1)
            * System.Math.Exp(GammaFunction.logValue(alpha_ + 1)
                              + GammaFunction.logValue(beta_ + 1)
                              - GammaFunction.logValue(alpha_ + beta_ + 2));

        public override double alpha(int i)
        {
            var num = beta_ * beta_ - alpha_ * alpha_;
            var denom = (2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_ + 2);

            if (denom.IsEqual(0.0))
            {
                if (num.IsNotEqual(0.0))
                {
                    Utils.QL_FAIL("can't compute a_k for jacobi integration");
                }
                else
                {
                    // l'Hospital
                    num = 2 * beta_;
                    denom = 2 * (2.0 * i + alpha_ + beta_ + 1);

                    Utils.QL_REQUIRE(denom.IsNotEqual(0.0), () => "can't compute a_k for jacobi integration");
                }
            }

            return num / denom;
        }
        public override double beta(int i)
        {
            var num = 4.0 * i * (i + alpha_) * (i + beta_) * (i + alpha_ + beta_);
            var denom = (2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_)
                                                   * ((2.0 * i + alpha_ + beta_) * (2.0 * i + alpha_ + beta_) - 1);

            if (denom.IsEqual(0.0))
            {
                if (num.IsNotEqual(0.0))
                {
                    Utils.QL_FAIL("can't compute b_k for jacobi integration");
                }
                else
                {
                    // l'Hospital
                    num = 4.0 * i * (i + beta_) * (2.0 * i + 2 * alpha_ + beta_);
                    denom = 2.0 * (2.0 * i + alpha_ + beta_);
                    denom *= denom - 1;
                    Utils.QL_REQUIRE(denom.IsNotEqual(0.0), () => "can't compute b_k for jacobi integration");
                }
            }
            return num / denom;
        }
        public override double w(double x) => System.Math.Pow(1 - x, alpha_) * System.Math.Pow(1 + x, beta_);
    }

    //! Gauss-Legendre polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussLegendrePolynomial : GaussJacobiPolynomial
    {
        public GaussLegendrePolynomial() : base(0.0, 0.0) { }
    }

    //! Gauss-Chebyshev polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussChebyshevPolynomial : GaussJacobiPolynomial
    {
        public GaussChebyshevPolynomial() : base(-0.5, -0.5) { }
    }

    //! Gauss-Chebyshev polynomial (second kind)
    [JetBrains.Annotations.PublicAPI] public class GaussChebyshev2ndPolynomial : GaussJacobiPolynomial
    {
        public GaussChebyshev2ndPolynomial() : base(0.5, 0.5) { }
    }

    //! Gauss-Gegenbauer polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussGegenbauerPolynomial : GaussJacobiPolynomial
    {
        public GaussGegenbauerPolynomial(double lambda) : base(lambda - 0.5, lambda - 0.5) { }
    }

    //! Gauss hyperbolic polynomial
    [JetBrains.Annotations.PublicAPI] public class GaussHyperbolicPolynomial : GaussianOrthogonalPolynomial
    {
        public override double mu_0() => Const.M_PI;

        public override double alpha(int i) => 0.0;

        public override double beta(int i) => i != 0 ? Const.M_PI_2 * Const.M_PI_2 * i * i : Const.M_PI;

        public override double w(double x) => 1 / System.Math.Cosh(x);
    }
}
