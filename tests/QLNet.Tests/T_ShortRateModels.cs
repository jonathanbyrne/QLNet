﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Models;
using QLNet.Math.Optimization;
using QLNet.Indexes;
using QLNet.Indexes.Ibor;
using QLNet.Time;
using QLNet.Math.Interpolations;
using QLNet.Instruments;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Models.Shortrate.calibrationhelpers;
using QLNet.Quotes;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.PricingEngines.Swap;
using QLNet.PricingEngines.swaption;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_ShortRateModels
    {

        [JetBrains.Annotations.PublicAPI] public class CalibrationData
        {
            public int start;
            public int length;
            public double volatility;
            public CalibrationData(int s, int l, double v)
            {
                start = s;
                length = l;
                volatility = v;
            }
        }

        [Fact]
        public void testCachedHullWhite()
        {
            //("Testing Hull-White calibration against cached values...");

            var today = new Date(15, Month.February, 2002);
            var settlement = new Date(19, Month.February, 2002);
            Settings.setEvaluationDate(today);
            var termStructure =
               new Handle<YieldTermStructure>(Utilities.flatRate(settlement, 0.04875825, new Actual365Fixed()));
            //termStructure.link
            var model = new HullWhite(termStructure);

            CalibrationData[] data = { new CalibrationData(1, 5, 0.1148),
                            new CalibrationData(2, 4, 0.1108),
                            new CalibrationData(3, 3, 0.1070),
                            new CalibrationData(4, 2, 0.1021),
                            new CalibrationData(5, 1, 0.1000)
         };
            IborIndex index = new Euribor6M(termStructure);

            IPricingEngine engine = new JamshidianSwaptionEngine(model);

            var swaptions = new List<CalibrationHelper>();
            for (var i = 0; i < data.Length; i++)
            {
                Quote vol = new SimpleQuote(data[i].volatility);
                CalibrationHelper helper =
                   new SwaptionHelper(new Period(data[i].start, TimeUnit.Years),
                                      new Period(data[i].length, TimeUnit.Years),
                                      new Handle<Quote>(vol),
                                      index,
                                      new Period(1, TimeUnit.Years),
                                      new Thirty360(),
                                      new Actual360(),
                                      termStructure);
                helper.setPricingEngine(engine);
                swaptions.Add(helper);
            }

            // Set up the optimization problem
            // Real simplexLambda = 0.1;
            // Simplex optimizationMethod(simplexLambda);
            var optimizationMethod = new LevenbergMarquardt(1.0e-8, 1.0e-8, 1.0e-8);
            var endCriteria = new EndCriteria(10000, 100, 1e-6, 1e-8, 1e-8);

            //Optimize
            model.calibrate(swaptions, optimizationMethod, endCriteria, new Constraint(), new List<double>());
            var ecType = model.endCriteria();

            // Check and print out results
#if QL_USE_INDEXED_COUPON
         double cachedA = 0.0488199, cachedSigma = 0.00593579;
#else
            double cachedA = 0.0488565, cachedSigma = 0.00593662;
#endif
            var tolerance = 1.120e-5;
            //double tolerance = 1.0e-6;
            var xMinCalculated = model.parameters();
            var yMinCalculated = model.value(xMinCalculated, swaptions);
            var xMinExpected = new Vector(2);
            xMinExpected[0] = cachedA;
            xMinExpected[1] = cachedSigma;
            var yMinExpected = model.value(xMinExpected, swaptions);
            if (System.Math.Abs(xMinCalculated[0] - cachedA) > tolerance
                || System.Math.Abs(xMinCalculated[1] - cachedSigma) > tolerance)
            {
                QAssert.Fail("Failed to reproduce cached calibration results:\n"
                             + "calculated: a = " + xMinCalculated[0] + ", "
                             + "sigma = " + xMinCalculated[1] + ", "
                             + "f(a) = " + yMinCalculated + ",\n"
                             + "expected:   a = " + xMinExpected[0] + ", "
                             + "sigma = " + xMinExpected[1] + ", "
                             + "f(a) = " + yMinExpected + ",\n"
                             + "difference: a = " + (xMinCalculated[0] - xMinExpected[0]) + ", "
                             + "sigma = " + (xMinCalculated[1] - xMinExpected[1]) + ", "
                             + "f(a) = " + (yMinCalculated - yMinExpected) + ",\n"
                             + "end criteria = " + ecType);
            }
        }

        [Fact]
        public void testSwaps()
        {
            //BOOST_MESSAGE("Testing Hull-White swap pricing against known values...");

            Date today;  //=Settings::instance().evaluationDate();;

            Calendar calendar = new TARGET();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);

            var settlement = calendar.advance(today, 2, TimeUnit.Days);

            Date[] dates =
            {
            settlement,
            calendar.advance(settlement, 1, TimeUnit.Weeks),
            calendar.advance(settlement, 1, TimeUnit.Months),
            calendar.advance(settlement, 3, TimeUnit.Months),
            calendar.advance(settlement, 6, TimeUnit.Months),
            calendar.advance(settlement, 9, TimeUnit.Months),
            calendar.advance(settlement, 1, TimeUnit.Years),
            calendar.advance(settlement, 2, TimeUnit.Years),
            calendar.advance(settlement, 3, TimeUnit.Years),
            calendar.advance(settlement, 5, TimeUnit.Years),
            calendar.advance(settlement, 10, TimeUnit.Years),
            calendar.advance(settlement, 15, TimeUnit.Years)
         };
            double[] discounts =
            {
            1.0,
            0.999258,
            0.996704,
            0.990809,
            0.981798,
            0.972570,
            0.963430,
            0.929532,
            0.889267,
            0.803693,
            0.596903,
            0.433022
         };

            //for (int i = 0; i < dates.Length; i++)
            //    dates[i] + dates.Length;

            var Interpolator = new LogLinear();

            var termStructure =
               new Handle<YieldTermStructure>(
               new InterpolatedDiscountCurve<LogLinear>(
                  dates.ToList(),
                  discounts.ToList(),
                  new Actual365Fixed(), new Calendar(), null, null, Interpolator)
            );

            var model = new HullWhite(termStructure);

            int[] start = { -3, 0, 3 };
            int[] length = { 2, 5, 10 };
            double[] rates = { 0.02, 0.04, 0.06 };
            IborIndex euribor = new Euribor6M(termStructure);

            IPricingEngine engine = new TreeVanillaSwapEngine(model, 120, termStructure);

#if QL_USE_INDEXED_COUPON
         double tolerance = 4.0e-3;
#else
            var tolerance = 1.0e-8;
#endif

            for (var i = 0; i < start.Length; i++)
            {

                var startDate = calendar.advance(settlement, start[i], TimeUnit.Months);
                if (startDate < today)
                {
                    var fixingDate = calendar.advance(startDate, -2, TimeUnit.Days);
                    var pastFixings = new TimeSeries<double?>();
                    pastFixings[fixingDate] = 0.03;
                    IndexManager.instance().setHistory(euribor.name(), pastFixings);
                }

                for (var j = 0; j < length.Length; j++)
                {

                    var maturity = calendar.advance(startDate, length[i], TimeUnit.Years);
                    var fixedSchedule = new Schedule(startDate, maturity, new Period(Frequency.Annual),
                                                          calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Forward, false);
                    var floatSchedule = new Schedule(startDate, maturity, new Period(Frequency.Semiannual),
                                                          calendar, BusinessDayConvention.Following, BusinessDayConvention.Following,
                                                          DateGeneration.Rule.Forward, false);
                    for (var k = 0; k < rates.Length; k++)
                    {

                        var swap = new VanillaSwap(VanillaSwap.Type.Payer, 1000000.0,
                                                           fixedSchedule, rates[k], new Thirty360(),
                                                           floatSchedule, euribor, 0.0, new Actual360());
                        swap.setPricingEngine(new DiscountingSwapEngine(termStructure));
                        var expected = swap.NPV();
                        swap.setPricingEngine(engine);
                        var calculated = swap.NPV();

                        var error = System.Math.Abs((expected - calculated) / expected);
                        if (error > tolerance)
                        {
                            QAssert.Fail("Failed to reproduce swap NPV:"
                                         //+ QL_FIXED << std::setprecision(9)
                                         + "\n    calculated: " + calculated
                                         + "\n    expected:   " + expected
                                         //+ QL_SCIENTIFIC
                                         + "\n    rel. error: " + error);
                        }
                    }
                }
            }
        }

        [Fact]
        public void testFuturesConvexityBias()
        {
            //BOOST_MESSAGE("Testing Hull-White futures convexity bias...");

            // G. Kirikos, D. Novak, "Convexity Conundrums", Risk Magazine, March 1997
            var futureQuote = 94.0;
            var a = 0.03;
            var sigma = 0.015;
            var t = 5.0;
            var T = 5.25;

            var expectedForward = 0.0573037;
            var tolerance = 0.0000001;

            var futureImpliedRate = (100.0 - futureQuote) / 100.0;
            var calculatedForward =
               futureImpliedRate - HullWhite.convexityBias(futureQuote, t, T, sigma, a);

            var error = System.Math.Abs(calculatedForward - expectedForward);

            if (error > tolerance)
            {
                QAssert.Fail("Failed to reproduce convexity bias:"
                             + "\ncalculated: " + calculatedForward
                             + "\n  expected: " + expectedForward
                             //+ QL_SCIENTIFIC
                             + "\n     error: " + error
                             + "\n tolerance: " + tolerance);
            }

        }
    }
}
