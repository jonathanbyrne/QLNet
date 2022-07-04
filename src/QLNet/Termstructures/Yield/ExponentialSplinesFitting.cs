//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using QLNet.Math;
using QLNet.Math.Optimization;
using System;

namespace QLNet.Termstructures.Yield
{
    //! Exponential-splines fitting method
    /*! Fits a discount function to the exponential form
        See:Li, B., E. DeWetering, G. Lucas, R. Brenner
        and A. Shapiro (2001): "Merrill Lynch Exponential Spline
        Model." Merrill Lynch Working Paper

        \warning convergence may be slow
    */
    [JetBrains.Annotations.PublicAPI] public class ExponentialSplinesFitting : FittedBondDiscountCurve.FittingMethod
    {
        public ExponentialSplinesFitting(bool constrainAtZero = true,
                                         Vector weights = null,
                                         OptimizationMethod optimizationMethod = null)
           : base(constrainAtZero, weights, optimizationMethod)
        { }

        public override FittedBondDiscountCurve.FittingMethod clone() => MemberwiseClone() as FittedBondDiscountCurve.FittingMethod;

        public override int size() => constrainAtZero_ ? 9 : 10;

        internal override double discountFunction(Vector x, double t)
        {
            var d = 0.0;
            var N = size();
            var kappa = x[N - 1];
            double coeff = 0;

            if (!constrainAtZero_)
            {
                for (var i = 0; i < N - 1; ++i)
                {
                    d += x[i] * System.Math.Exp(-kappa * (i + 1) * t);
                }
            }
            else
            {
                //  notation:
                //  d(t) = coeff* exp(-kappa*1*t) + x[0]* exp(-kappa*2*t) +
                //  x[1]* exp(-kappa*3*t) + ..+ x[7]* exp(-kappa*9*t)
                for (var i = 0; i < N - 1; i++)
                {
                    d += x[i] * System.Math.Exp(-kappa * (i + 2) * t);
                    coeff += x[i];
                }
                coeff = 1.0 - coeff;
                d += coeff * System.Math.Exp(-kappa * t);
            }
            return d;
        }
    }

    //! Nelson-Siegel fitting method
    /*! Fits a discount function to the form
        \f$ d(t) = \exp^{-r t}, \f$ where the zero rate \f$r\f$ is defined as
        \f[
        r \equiv c_0 + (c_0 + c_1)*(1 - exp^{-\kappa*t}/(\kappa t) -
        c_2 exp^{ - \kappa t}.
        \f]
        See: Nelson, C. and A. Siegel (1985): "Parsimonious modeling of yield
        curves for US Treasury bills." NBER Working Paper Series, no 1594.
    */

    //! Svensson Fitting method
    /*! Fits a discount function to the form

        See: Svensson, L. (1994). Estimating and interpreting forward
        interest rates: Sweden 1992-4.
        Discussion paper, Centre for Economic Policy Research(1051).
    */

    //! CubicSpline B-splines fitting method
    /*! Fits a discount function to a set of cubic B-splines
        \f$ N_{i,3}(t) \f$, i.e.,
        \f[
        d(t) = \sum_{i=0}^{n}  c_i * N_{i,3}(t)
        \f]

        See: McCulloch, J. 1971, "Measuring the Term Structure of
        Interest Rates." Journal of Business, 44: 19-31

        McCulloch, J. 1975, "The tax adjusted yield curve."
        Journal of Finance, XXX811-30

        \warning "The results are extremely sensitive to the number
                  and location of the knot points, and there is no
                  optimal way of selecting them." James, J. and
                  N. Webber, "Interest Rate Modelling" John Wiley,
                  2000, pp. 440.
    */

    //! Simple polynomial fitting method
    /*
          This is a simple/crude, but fast and robust, means of fitting
          a yield curve.
    */

    //! Spread fitting method helper
    /*  Fits a spread curve on top of a discount function according to given parametric method
    */
}
