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

using JetBrains.Annotations;

namespace QLNet.Math.Distributions
{
    [PublicAPI]
    public class GammaDistribution
    {
        private double a_;

        public GammaDistribution(double a)
        {
            a_ = a;
            QLNet.Utils.QL_REQUIRE(a > 0.0, () => "invalid parameter for gamma distribution");
        }

        public double value(double x)
        {
            if (x <= 0.0)
            {
                return 0.0;
            }

            var gln = GammaFunction.logValue(a_);

            if (x < a_ + 1.0)
            {
                var ap = a_;
                var del = 1.0 / a_;
                var sum = del;
                for (var n = 1; n <= 100; n++)
                {
                    ap += 1.0;
                    del *= x / ap;
                    sum += del;
                    if (System.Math.Abs(del) < System.Math.Abs(sum) * 3.0e-7)
                    {
                        return sum * System.Math.Exp(-x + a_ * System.Math.Log(x) - gln);
                    }
                }
            }
            else
            {
                var b = x + 1.0 - a_;
                var c = double.MaxValue;
                var d = 1.0 / b;
                var h = d;
                for (var n = 1; n <= 100; n++)
                {
                    var an = -1.0 * n * (n - a_);
                    b += 2.0;
                    d = an * d + b;
                    if (System.Math.Abs(d) < Const.QL_EPSILON)
                    {
                        d = Const.QL_EPSILON;
                    }

                    c = b + an / c;
                    if (System.Math.Abs(c) < Const.QL_EPSILON)
                    {
                        c = Const.QL_EPSILON;
                    }

                    d = 1.0 / d;
                    var del = d * c;
                    h *= del;
                    if (System.Math.Abs(del - 1.0) < Const.QL_EPSILON)
                    {
                        return h * System.Math.Exp(-x + a_ * System.Math.Log(x) - gln);
                    }
                }
            }

            QLNet.Utils.QL_FAIL("too few iterations");
            return 0;
        }
    }

    //! Gamma function class
    /*! This is a function defined by
        \f[
            \Gamma(z) = \int_0^{\infty}t^{z-1}e^{-t}dt
        \f]

        The implementation of the algorithm was inspired by
        "Numerical Recipes in C", 2nd edition,
        Press, Teukolsky, Vetterling, Flannery, chapter 6

        \test the correctness of the returned value is tested by
              checking it against known good results.
    */
}
