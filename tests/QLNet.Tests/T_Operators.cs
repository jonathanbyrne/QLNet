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
using System;
using Xunit;
using QLNet.Math.Distributions;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Methods.Finitedifferences;
using QLNet.Processes;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Operators
    {
        public const double average = 0.0, sigma = 1.0;

        [Fact]
        public void testOperatorConsistency()
        {

            //("Testing differential operators...");

            var normal = new NormalDistribution(average, sigma);
            var cum = new CumulativeNormalDistribution(average, sigma);

            double xMin = average - 4 * sigma,
                   xMax = average + 4 * sigma;
            var N = 10001;
            var h = (xMax - xMin) / (N - 1);

            Vector x = new Vector(N),
            y = new Vector(N),
            yi = new Vector(N),
            yd = new Vector(N),
            temp = new Vector(N),
            diff = new Vector(N);

            for (var i = 0; i < N; i++)
            {
                x[i] = xMin + h * i;
            }

            for (var i = 0; i < x.Count; i++)
            {
                y[i] = normal.value(x[i]);
            }

            for (var i = 0; i < x.Count; i++)
            {
                yi[i] = cum.value(x[i]);
            }

            for (var i = 0; i < x.size(); i++)
            {
                yd[i] = normal.derivative(x[i]);
            }

            // define the differential operators
            var D = new DZero(N, h);
            var D2 = new DPlusDMinus(N, h);

            // check that the derivative of cum is Gaussian
            temp = D.applyTo(yi);

            for (var i = 0; i < y.Count; i++)
            {
                diff[i] = y[i] - temp[i];
            }

            var e = Utilities.norm(diff, diff.size(), h);
            if (e > 1.0e-6)
            {
                QAssert.Fail("norm of 1st derivative of cum minus Gaussian: " + e + "\ntolerance exceeded");
            }

            // check that the second derivative of cum is normal.derivative
            temp = D2.applyTo(yi);

            for (var i = 0; i < yd.Count; i++)
            {
                diff[i] = yd[i] - temp[i];
            }

            e = Utilities.norm(diff, diff.size(), h);
            if (e > 1.0e-4)
            {
                QAssert.Fail("norm of 2nd derivative of cum minus Gaussian derivative: " + e + "\ntolerance exceeded");
            }
        }

        [Fact]
        public void testBSMOperatorConsistency()
        {
            //("Testing consistency of BSM operators...");

            var grid = new Vector(10);
            var price = 20.0;
            var factor = 1.1;
            for (var i = 0; i < grid.size(); i++)
            {
                grid[i] = price;
                price *= factor;
            }

            var dx = System.Math.Log(factor);
            var r = 0.05;
            var q = 0.01;
            var sigma = 0.5;

            var refer = new BSMOperator(grid.size(), dx, r, q, sigma);

            DayCounter dc = new Actual360();
            var today = Date.Today;
            var exercise = today + new Period(2, TimeUnit.Years);
            var residualTime = dc.yearFraction(today, exercise);

            var spot = new SimpleQuote(0.0);
            var qTS = Utilities.flatRate(today, q, dc);
            var rTS = Utilities.flatRate(today, r, dc);
            var volTS = Utilities.flatVol(today, sigma, dc);
            var stochProcess = new GeneralizedBlackScholesProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));
            var op1 = new BSMOperator(grid, stochProcess, residualTime);
            var op2 = new PdeOperator<PdeBSM>(grid, stochProcess, residualTime);

            var tolerance = 1.0e-6;
            var lderror = refer.lowerDiagonal() - op1.lowerDiagonal();
            var derror = refer.diagonal() - op1.diagonal();
            var uderror = refer.upperDiagonal() - op1.upperDiagonal();

            for (var i = 2; i < grid.size() - 2; i++)
            {
                if (System.Math.Abs(lderror[i]) > tolerance ||
                    System.Math.Abs(derror[i]) > tolerance ||
                    System.Math.Abs(uderror[i]) > tolerance)
                {
                    QAssert.Fail("inconsistency between BSM operators:\n"
                                 + i + " row:\n"
                                 + "expected:   "
                                 + refer.lowerDiagonal()[i] + ", "
                                 + refer.diagonal()[i] + ", "
                                 + refer.upperDiagonal()[i] + "\n"
                                 + "calculated: "
                                 + op1.lowerDiagonal()[i] + ", "
                                 + op1.diagonal()[i] + ", "
                                 + op1.upperDiagonal()[i]);
                }
            }
            lderror = refer.lowerDiagonal() - op2.lowerDiagonal();
            derror = refer.diagonal() - op2.diagonal();
            uderror = refer.upperDiagonal() - op2.upperDiagonal();

            for (var i = 2; i < grid.size() - 2; i++)
            {
                if (System.Math.Abs(lderror[i]) > tolerance ||
                    System.Math.Abs(derror[i]) > tolerance ||
                    System.Math.Abs(uderror[i]) > tolerance)
                {
                    QAssert.Fail("inconsistency between BSM operators:\n"
                                 + i + " row:\n"
                                 + "expected:   "
                                 + refer.lowerDiagonal()[i] + ", "
                                 + refer.diagonal()[i] + ", "
                                 + refer.upperDiagonal()[i] + "\n"
                                 + "calculated: "
                                 + op2.lowerDiagonal()[i] + ", "
                                 + op2.diagonal()[i] + ", "
                                 + op2.upperDiagonal()[i]);
                }
            }
        }

    }
}
