﻿/*
 Copyright (C) 2008-2009 Andrea Maggiulli

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
using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;
using QLNet.Math.Interpolations;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_SVI
    {
        private readonly ITestOutputHelper testOutputHelper;

        public T_SVI(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        double add10(double x) => x + 10;

        double mul10(double x) => x * 10;

        double sub10(double x) => x - 10;

        double add
           (double x, double y) =>
            x + y;

        double mul(double x, double y) => x * y;

        double sub(double x, double y) => x - y;

        [Fact(Skip = "Failing")]
        public void testCalibration()
        {
            var forward = 0.03;
            var tau = 1.0;

            //Real a = 0.04;
            //Real b = 0.1;
            //Real rho = -0.5;
            //Real sigma = 0.1;
            //Real m  = 0.0;
            var a = 0.1;
            var b = 0.06;
            var rho = -0.9;
            var m = 0.24;
            var sigma = 0.06;

            var strikes = new List<double>();
            strikes.Add(0.01);
            strikes.Add(0.015);
            strikes.Add(0.02);
            strikes.Add(0.025);
            strikes.Add(0.03);
            strikes.Add(0.035);
            strikes.Add(0.04);
            strikes.Add(0.045);
            strikes.Add(0.05);

            List<double> vols = new InitializedList<double>(strikes.Count, 0.20); //dummy vols (we do not calibrate here)

            var svi = new SviInterpolation(strikes, strikes.Count, vols, tau,
                                                        forward, a, b, sigma, rho, m, true, true, true,
                                                        true, true);

            svi.enableExtrapolation();

            List<double> sviVols = new InitializedList<double>(strikes.Count, 0.0);
            for (var i = 0; i < strikes.Count; ++i)
            {
                sviVols[i] = svi.value(strikes[i]);
            }

            var svi2 = new SviInterpolation(strikes, strikes.Count, sviVols, tau,
                                                         forward, null, null, null,
                                                         null, null, false, false, false,
                                                         false, false, false, null,
                                                         null, 1E-8, false,
                                                         0); //don't allow for random start values

            svi2.enableExtrapolation();
            svi2.update();

            testOutputHelper.WriteLine("a=" + svi2.a());
            if (!Math.Utils.close_enough(a, svi2.a(), 100))
            {
                QAssert.Fail("error in a coefficient estimation");
            }

            testOutputHelper.WriteLine("b=" + svi2.b());
            if (!Math.Utils.close_enough(b, svi2.b(), 100))
            {
                QAssert.Fail("error in b coefficient estimation");
            }

            testOutputHelper.WriteLine("sigma=" + svi2.sigma());
            if (!Math.Utils.close_enough(sigma, svi2.sigma(), 100))
            {
                QAssert.Fail("error in sigma coefficient estimation");
            }

            testOutputHelper.WriteLine("rho=" + svi2.rho());
            if (!Math.Utils.close_enough(rho, svi2.rho(), 100))
            {
                QAssert.Fail("error in rho coefficient estimation");
            }

            testOutputHelper.WriteLine("m=" + svi2.m());
            if (!Math.Utils.close_enough(m, svi2.m(), 100))
            {
                QAssert.Fail("error in m coefficient estimation");
            }

            testOutputHelper.WriteLine("error=" + svi2.rmsError());
        }

    }
}
