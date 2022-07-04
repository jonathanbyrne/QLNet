/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_BlackFormula
    {
        [Fact]
        public void testBachelierImpliedVol()
        {
            // Testing Bachelier implied vol...

            var forward = 1.0;
            var bpvol = 0.01;
            var tte = 10.0;
            var stdDev = bpvol * System.Math.Sqrt(tte);
            var optionType = QLNet.Option.Type.Call;
            var discount = 0.95;

            double[] d = { -3.0, -2.0, -1.0, -0.5, 0.0, 0.5, 1.0, 2.0, 3.0 };
            for (var i = 0; i < d.Length; ++i)
            {
                var strike = forward - d[i] * bpvol * System.Math.Sqrt(tte);
                var callPrem = Utils.bachelierBlackFormula(optionType, strike, forward, stdDev, discount);
                var impliedBpVol = Utils.bachelierBlackFormulaImpliedVol(optionType, strike, forward, tte, callPrem, discount);

                if (System.Math.Abs(bpvol - impliedBpVol) > 1.0e-12)
                {
                    QAssert.Fail("Failed, expected " + bpvol + " realised " + impliedBpVol);
                }
            }
            return;
        }

        [Fact]
        public void testChambersImpliedVol()
        {
            // Testing Chambers-Nawalkha implied vol approximation

            Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
            double[] displacements = { 0.0000, 0.0010, 0.0050, 0.0100, 0.0200 };
            double[] forwards = { -0.0010, 0.0000, 0.0050, 0.0100, 0.0200, 0.0500 };
            double[] strikes = { -0.0100, -0.0050, -0.0010, 0.0000, 0.0010, 0.0050, 0.0100, 0.0200, 0.0500, 0.1000 };
            double[] stdDevs = { 0.10, 0.15, 0.20, 0.30, 0.50, 0.60, 0.70, 0.80, 1.00, 1.50, 2.00 };
            double[] discounts = { 1.00, 0.95, 0.80, 1.10 };

            var tol = 5.0E-4;

            for (var i1 = 0; i1 < types.Length; ++i1)
            {
                for (var i2 = 0; i2 < displacements.Length; ++i2)
                {
                    for (var i3 = 0; i3 < forwards.Length; ++i3)
                    {
                        for (var i4 = 0; i4 < strikes.Length; ++i4)
                        {
                            for (var i5 = 0; i5 < stdDevs.Length; ++i5)
                            {
                                for (var i6 = 0; i6 < discounts.Length; ++i6)
                                {
                                    if (forwards[i3] + displacements[i2] > 0.0 &&
                                        strikes[i4] + displacements[i2] > 0.0)
                                    {
                                        var premium = Utils.blackFormula(
                                                            types[i1], strikes[i4], forwards[i3],
                                                            stdDevs[i5], discounts[i6],
                                                            displacements[i2]);
                                        var atmPremium = Utils.blackFormula(
                                                               types[i1], forwards[i3], forwards[i3],
                                                               stdDevs[i5], discounts[i6],
                                                               displacements[i2]);
                                        var iStdDev = Utils.blackFormulaImpliedStdDevChambers(
                                                            types[i1], strikes[i4], forwards[i3],
                                                            premium, atmPremium, discounts[i6],
                                                            displacements[i2]);
                                        var moneyness = (strikes[i4] + displacements[i2]) /
                                                        (forwards[i3] + displacements[i2]);
                                        if (moneyness > 1.0)
                                            moneyness = 1.0 / moneyness;
                                        var error = (iStdDev - stdDevs[i5]) / stdDevs[i5] * moneyness;
                                        if (error > tol)
                                            QAssert.Fail("Failed to verify Chambers-Nawalkha approximation for "
                                                         + types[i1]
                                                         + " displacement=" + displacements[i2]
                                                         + " forward=" + forwards[i3]
                                                         + " strike=" + strikes[i4]
                                                         + " discount=" + discounts[i6]
                                                         + " stddev=" + stdDevs[i5]
                                                         + " result=" + iStdDev
                                                         + " exceeds maximum error tolerance");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
