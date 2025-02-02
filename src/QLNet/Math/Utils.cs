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

namespace QLNet.Math
{
    public partial class Utils
    {
        public static double betaContinuedFraction(double a, double b, double x) => betaContinuedFraction(a, b, x, 1e-16, 100);

        public static double betaContinuedFraction(double a, double b, double x, double accuracy, int maxIteration)
        {
            double aa, del;
            var qab = a + b;
            var qap = a + 1.0;
            var qam = a - 1.0;
            var c = 1.0;
            var d = 1.0 - qab * x / qap;
            if (System.Math.Abs(d) < Const.QL_EPSILON)
            {
                d = Const.QL_EPSILON;
            }

            d = 1.0 / d;
            var result = d;

            int m, m2;
            for (m = 1; m <= maxIteration; m++)
            {
                m2 = 2 * m;
                aa = m * (b - m) * x / ((qam + m2) * (a + m2));
                d = 1.0 + aa * d;
                if (System.Math.Abs(d) < Const.QL_EPSILON)
                {
                    d = Const.QL_EPSILON;
                }

                c = 1.0 + aa / c;
                if (System.Math.Abs(c) < Const.QL_EPSILON)
                {
                    c = Const.QL_EPSILON;
                }

                d = 1.0 / d;
                result *= d * c;
                aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
                d = 1.0 + aa * d;
                if (System.Math.Abs(d) < Const.QL_EPSILON)
                {
                    d = Const.QL_EPSILON;
                }

                c = 1.0 + aa / c;
                if (System.Math.Abs(c) < Const.QL_EPSILON)
                {
                    c = Const.QL_EPSILON;
                }

                d = 1.0 / d;
                del = d * c;
                result *= del;
                if (System.Math.Abs(del - 1.0) < accuracy)
                {
                    return result;
                }
            }

            QLNet.Utils.QL_FAIL("a or b too big, or maxIteration too small in betacf");
            return 0;
        }

        public static double betaFunction(double z, double w) =>
            System.Math.Exp(GammaFunction.logValue(z) +
                            GammaFunction.logValue(w) -
                            GammaFunction.logValue(z + w));

        /*! Incomplete Beta function
  
            The implementation of the algorithm was inspired by
            "Numerical Recipes in C", 2nd edition,
            Press, Teukolsky, Vetterling, Flannery, chapter 6
        */
        public static double incompleteBetaFunction(double a, double b, double x) => incompleteBetaFunction(a, b, x, 1e-16, 100);

        public static double incompleteBetaFunction(double a, double b, double x, double accuracy, int maxIteration)
        {
            QLNet.Utils.QL_REQUIRE(a > 0.0, () => "a must be greater than zero");
            QLNet.Utils.QL_REQUIRE(b > 0.0, () => "b must be greater than zero");

            if (x.IsEqual(0.0))
            {
                return 0.0;
            }

            if (x.IsEqual(1.0))
            {
                return 1.0;
            }

            QLNet.Utils.QL_REQUIRE(x > 0.0 && x < 1.0, () => "x must be in [0,1]");

            var result = System.Math.Exp(GammaFunction.logValue(a + b) -
                                         GammaFunction.logValue(a) - GammaFunction.logValue(b) +
                                         a * System.Math.Log(x) + b * System.Math.Log(1.0 - x));

            if (x < (a + 1.0) / (a + b + 2.0))
            {
                return result *
                    betaContinuedFraction(a, b, x, accuracy, maxIteration) / a;
            }

            return 1.0 - result *
                betaContinuedFraction(b, a, 1.0 - x, accuracy, maxIteration) / b;
        }
    }
}
