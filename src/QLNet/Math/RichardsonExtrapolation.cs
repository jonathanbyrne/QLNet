﻿/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using JetBrains.Annotations;
using QLNet.Math.Solvers1d;

namespace QLNet.Math
{
    //! Richardson Extrapolation
    /*! Richardson Extrapolation is a sequence acceleration technique for
      \f[
          f(\Delta h) = f_0 + \alpha\cdot (\Delta h)^n + O((\Delta h)^{n+1})
      \f]
     */
    /*! References:
        http://en.wikipedia.org/wiki/Richardson_extrapolation
    */

    [PublicAPI]
    public class RichardsonExtrapolation
    {
        /*! Richardon Extrapolation
           \param f function to be extrapolated to delta_h -> 0
           \param delta_h step size
           \param n if known, n is the order of convergence
        */
        public delegate double function(double num);

        private double delta_h_;
        private function f_;
        private double fdelta_h_;
        private double? n_;

        public RichardsonExtrapolation(function f, double delta_h, double? n = null)
        {
            delta_h_ = delta_h;
            fdelta_h_ = f(delta_h);
            n_ = n;
            f_ = f;
        }

        /*! Extrapolation for known order of convergence
           \param t scaling factor for the step size
        */
        public double value(double t = 2.0)
        {
            QLNet.Utils.QL_REQUIRE(t > 1, () => "scaling factor must be greater than 1");
            QLNet.Utils.QL_REQUIRE(n_ != null, () => "order of convergence must be known");

            var tk = System.Math.Pow(t, n_.Value);

            return (tk * f_(delta_h_ / t) - fdelta_h_) / (tk - 1.0);
        }

        /*! Extrapolation for unknown order of convergence
           \param t first scaling factor for the step size
           \param s second scaling factor for the step size
        */
        public double value(double t, double s)
        {
            QLNet.Utils.QL_REQUIRE(t > 1 && s > 1, () => "scaling factors must be greater than 1");
            QLNet.Utils.QL_REQUIRE(t > s, () => "t must be greater than s");

            var ft = f_(delta_h_ / t);
            var fs = f_(delta_h_ / s);

            var k = new Brent().solve(new RichardsonEqn(fdelta_h_, ft, fs, t, s),
                1e-8, 0.05, 10);

            var ts = System.Math.Pow(s, k);

            return (ts * fs - fdelta_h_) / (ts - 1.0);
        }
    }
}
