﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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

using System;
using JetBrains.Annotations;
using QLNet.Extensions;

namespace QLNet.Math.integrals
{
    //! Integral of a one-dimensional function
    /*! Given a target accuracy \f$ \epsilon \f$, the integral of
        a function \f$ f \f$ between \f$ a \f$ and \f$ b \f$ is
        calculated by means of the Gauss-Lobatto formula
    */

    /*! References:
       This algorithm is a C++ implementation of the algorithm outlined in

       W. Gander and W. Gautschi, Adaptive Quadrature - Revisited.
       BIT, 40(1):84-101, March 2000. CS technical report:
       ftp.inf.ethz.ch/pub/publications/tech-reports/3xx/306.ps.gz

       The original MATLAB version can be downloaded here
       http://www.inf.ethz.ch/personal/gander/adaptlob.m
    */
    [PublicAPI]
    public class GaussLobattoIntegral : Integrator
    {
        protected static readonly double alpha_ = System.Math.Sqrt(2.0 / 3.0);
        protected static readonly double beta_ = 1.0 / System.Math.Sqrt(5.0);
        protected static readonly double x1_ = 0.94288241569547971906;
        protected static readonly double x2_ = 0.64185334234578130578;
        protected static readonly double x3_ = 0.23638319966214988028;
        protected double? relAccuracy_;
        protected bool useConvergenceEstimate_;

        public GaussLobattoIntegral(int maxIterations,
            double? absAccuracy,
            double? relAccuracy = null,
            bool useConvergenceEstimate = true)
            : base(absAccuracy, maxIterations)
        {
            relAccuracy_ = relAccuracy;
            useConvergenceEstimate_ = useConvergenceEstimate;
        }

        protected double adaptivGaussLobattoStep(Func<double, double> f, double a, double b, double fa, double fb, double acc)
        {
            QLNet.Utils.QL_REQUIRE(numberOfEvaluations() < maxEvaluations(), () => "max number of iterations reached");

            var h = (b - a) / 2;
            var m = (a + b) / 2;

            var mll = m - alpha_ * h;
            var ml = m - beta_ * h;
            var mr = m + beta_ * h;
            var mrr = m + alpha_ * h;

            var fmll = f(mll);
            var fml = f(ml);
            var fm = f(m);
            var fmr = f(mr);
            var fmrr = f(mrr);
            increaseNumberOfEvaluations(5);

            var integral2 = h / 6 * (fa + fb + 5 * (fml + fmr));
            var integral1 = h / 1470 * (77 * (fa + fb)
                                        + 432 * (fmll + fmrr) + 625 * (fml + fmr) + 672 * fm);

            // avoid 80 bit logic on x86 cpu
            var dist = acc + (integral1 - integral2);
            if (dist.IsEqual(acc) || mll <= a || b <= mrr)
            {
                QLNet.Utils.QL_REQUIRE(m > a && b > m, () => "Interval contains no more machine number");
                return integral1;
            }

            return adaptivGaussLobattoStep(f, a, mll, fa, fmll, acc)
                   + adaptivGaussLobattoStep(f, mll, ml, fmll, fml, acc)
                   + adaptivGaussLobattoStep(f, ml, m, fml, fm, acc)
                   + adaptivGaussLobattoStep(f, m, mr, fm, fmr, acc)
                   + adaptivGaussLobattoStep(f, mr, mrr, fmr, fmrr, acc)
                   + adaptivGaussLobattoStep(f, mrr, b, fmrr, fb, acc);
        }

        protected double calculateAbsTolerance(Func<double, double> f, double a, double b)
        {
            var relTol = System.Math.Max(relAccuracy_ ?? 0, Const.QL_EPSILON);

            var m = (a + b) / 2;
            var h = (b - a) / 2;
            var y1 = f(a);
            var y3 = f(m - alpha_ * h);
            var y5 = f(m - beta_ * h);
            var y7 = f(m);
            var y9 = f(m + beta_ * h);
            var y11 = f(m + alpha_ * h);
            var y13 = f(b);

            var f1 = f(m - x1_ * h);
            var f2 = f(m + x1_ * h);
            var f3 = f(m - x2_ * h);
            var f4 = f(m + x2_ * h);
            var f5 = f(m - x3_ * h);
            var f6 = f(m + x3_ * h);

            var acc = h * (0.0158271919734801831 * (y1 + y13)
                           + 0.0942738402188500455 * (f1 + f2)
                           + 0.1550719873365853963 * (y3 + y11)
                           + 0.1888215739601824544 * (f3 + f4)
                           + 0.1997734052268585268 * (y5 + y9)
                           + 0.2249264653333395270 * (f5 + f6)
                           + 0.2426110719014077338 * y7);

            increaseNumberOfEvaluations(13);
            if (acc.IsEqual(0.0) && (f1.IsNotEqual(0.0) || f2.IsNotEqual(0.0) || f3.IsNotEqual(0.0)
                                     || f4.IsNotEqual(0.0) || f5.IsNotEqual(0.0) || f6.IsNotEqual(0.0)))
            {
                QLNet.Utils.QL_FAIL("can not calculate absolute accuracy from relative accuracy");
            }

            var r = 1.0;
            if (useConvergenceEstimate_)
            {
                var integral2 = h / 6 * (y1 + y13 + 5 * (y5 + y9));
                var integral1 = h / 1470 * (77 * (y1 + y13) + 432 * (y3 + y11) +
                                            625 * (y5 + y9) + 672 * y7);

                if (System.Math.Abs(integral2 - acc).IsNotEqual(0.0))
                {
                    r = System.Math.Abs(integral1 - acc) / System.Math.Abs(integral2 - acc);
                }

                if (r.IsEqual(0.0) || r > 1.0)
                {
                    r = 1.0;
                }
            }

            if (relAccuracy_ != null)
            {
                if (absoluteAccuracy() != null)
                {
                    return System.Math.Min(absoluteAccuracy().GetValueOrDefault(), acc * relTol) / (r * Const.QL_EPSILON);
                }

                return acc * relTol / (r * Const.QL_EPSILON);
            }

            return absoluteAccuracy().GetValueOrDefault() / (r * Const.QL_EPSILON);
        }

        protected override double integrate(Func<double, double> f, double a, double b)
        {
            setNumberOfEvaluations(0);
            var calcAbsTolerance = calculateAbsTolerance(f, a, b);

            increaseNumberOfEvaluations(2);
            return adaptivGaussLobattoStep(f, a, b, f(a), f(b), calcAbsTolerance);
        }
    }
}
