﻿/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using Xunit;
using System;
using System.Collections.Generic;
using QLNet.Currencies;
using QLNet.Termstructures.Yield;
using QLNet.Indexes;
using QLNet.Time;
using QLNet.PricingEngines.credit;
using QLNet.Instruments;
using QLNet.Math.Interpolations;
using QLNet.Termstructures;
using QLNet.Quotes;
using QLNet.Termstructures.Credit;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_CreditDefaultSwap
    {
        [Fact]
        public void testCachedValue()
        {
            // Testing credit-default swap against cached values...

            using (var backup = new SavedSettings())
            {

                // Initialize curves
                Settings.setEvaluationDate(new Date(9, Month.June, 2006));
                var today = Settings.evaluationDate();
                Calendar calendar = new TARGET();

                var hazardRate = new Handle<Quote>(new SimpleQuote(0.01234));
                var probabilityCurve = new RelinkableHandle<DefaultProbabilityTermStructure>();
                probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

                var discountCurve = new RelinkableHandle<YieldTermStructure>();

                discountCurve.linkTo(new FlatForward(today, 0.06, new Actual360()));

                // Build the schedule
                var issueDate = calendar.advance(today, -1, TimeUnit.Years);
                var maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
                var frequency = Frequency.Semiannual;
                var convention = BusinessDayConvention.ModifiedFollowing;

                var schedule = new Schedule(issueDate, maturity, new Period(frequency), calendar,
                                                 convention, convention, DateGeneration.Rule.Forward, false);

                // Build the CDS
                var fixedRate = 0.0120;
                DayCounter dayCount = new Actual360();
                var notional = 10000.0;
                var recoveryRate = 0.4;

                var cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
                                                              schedule, convention, dayCount, true, true);
                cds.setPricingEngine(new MidPointCdsEngine(probabilityCurve, recoveryRate, discountCurve));

                var npv = 295.0153398;
                var fairRate = 0.007517539081;

                var calculatedNpv = cds.NPV();
                var calculatedFairRate = cds.fairSpread();
                var tolerance = 1.0e-7;

                if (System.Math.Abs(calculatedNpv - npv) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce NPV with mid-point engine\n"
                        + "    calculated NPV: " + calculatedNpv + "\n"
                        + "    expected NPV:   " + npv);
                }

                if (System.Math.Abs(calculatedFairRate - fairRate) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce fair rate with mid-point engine\n"
                        + "    calculated fair rate: " + calculatedFairRate + "\n"
                        + "    expected fair rate:   " + fairRate);
                }

                cds.setPricingEngine(new IntegralCdsEngine(new Period(1, TimeUnit.Days), probabilityCurve,
                                                           recoveryRate, discountCurve));

                calculatedNpv = cds.NPV();
                calculatedFairRate = cds.fairSpread();
                tolerance = 1.0e-5;

                if (System.Math.Abs(calculatedNpv - npv) > notional * tolerance * 10)
                {
                    QAssert.Fail(
                        "Failed to reproduce NPV with integral engine "
                        + "(step = 1 day)\n"
                        + "    calculated NPV: " + calculatedNpv + "\n"
                        + "    expected NPV:   " + npv);
                }

                if (System.Math.Abs(calculatedFairRate - fairRate) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce fair rate with integral engine "
                        + "(step = 1 day)\n"
                        + "    calculated fair rate: " + calculatedFairRate + "\n"
                        + "    expected fair rate:   " + fairRate);
                }

                cds.setPricingEngine(new IntegralCdsEngine(new Period(1, TimeUnit.Weeks), probabilityCurve, recoveryRate, discountCurve));

                calculatedNpv = cds.NPV();
                calculatedFairRate = cds.fairSpread();
                tolerance = 1.0e-5;

                if (System.Math.Abs(calculatedNpv - npv) > notional * tolerance * 10)
                {
                    QAssert.Fail(
                        "Failed to reproduce NPV with integral engine "
                        + "(step = 1 week)\n"
                        + "    calculated NPV: " + calculatedNpv + "\n"
                        + "    expected NPV:   " + npv);
                }

                if (System.Math.Abs(calculatedFairRate - fairRate) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce fair rate with integral engine "
                        + "(step = 1 week)\n"
                        + "    calculated fair rate: " + calculatedFairRate + "\n"
                        + "    expected fair rate:   " + fairRate);
                }
            }
        }

        [Fact]
        public void testCachedMarketValue()
        {
            // Testing credit-default swap against cached market values...

            using (var backup = new SavedSettings())
            {

                Settings.setEvaluationDate(new Date(9, Month.June, 2006));
                var evalDate = Settings.evaluationDate();
                Calendar calendar = new UnitedStates();

                var discountDates = new List<Date>();
                discountDates.Add(evalDate);
                discountDates.Add(calendar.advance(evalDate, 1, TimeUnit.Weeks, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 1, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 2, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 3, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 6, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 12, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 2, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 3, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 4, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 5, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 6, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 7, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 8, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 9, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 10, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                discountDates.Add(calendar.advance(evalDate, 15, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));

                var dfs = new List<double>();
                dfs.Add(1.0);
                dfs.Add(0.9990151375768731);
                dfs.Add(0.99570502636871183);
                dfs.Add(0.99118260474528685);
                dfs.Add(0.98661167950906203);
                dfs.Add(0.9732592953359388);
                dfs.Add(0.94724424481038083);
                dfs.Add(0.89844996737120875);
                dfs.Add(0.85216647839921411);
                dfs.Add(0.80775477692556874);
                dfs.Add(0.76517289234200347);
                dfs.Add(0.72401019553182933);
                dfs.Add(0.68503909569219212);
                dfs.Add(0.64797499814013748);
                dfs.Add(0.61263171936255534);
                dfs.Add(0.5791942350748791);
                dfs.Add(0.43518868769953606);

                DayCounter curveDayCounter = new Actual360();

                var discountCurve = new RelinkableHandle<YieldTermStructure>();
                discountCurve.linkTo(new InterpolatedDiscountCurve<LogLinear>(discountDates, dfs, curveDayCounter, null, null, null, new LogLinear()));

                DayCounter dayCounter = new Thirty360();
                var dates = new List<Date>();
                dates.Add(evalDate);
                dates.Add(calendar.advance(evalDate, 6, TimeUnit.Months, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 1, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 2, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 3, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 4, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 5, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 7, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));
                dates.Add(calendar.advance(evalDate, 10, TimeUnit.Years, BusinessDayConvention.ModifiedFollowing));

                var defaultProbabilities = new List<double>();
                defaultProbabilities.Add(0.0000);
                defaultProbabilities.Add(0.0047);
                defaultProbabilities.Add(0.0093);
                defaultProbabilities.Add(0.0286);
                defaultProbabilities.Add(0.0619);
                defaultProbabilities.Add(0.0953);
                defaultProbabilities.Add(0.1508);
                defaultProbabilities.Add(0.2288);
                defaultProbabilities.Add(0.3666);

                var hazardRates = new List<double>();
                hazardRates.Add(0.0);
                for (var i = 1; i < dates.Count; ++i)
                {
                    var t1 = dayCounter.yearFraction(dates[0], dates[i - 1]);
                    var t2 = dayCounter.yearFraction(dates[0], dates[i]);
                    var S1 = 1.0 - defaultProbabilities[i - 1];
                    var S2 = 1.0 - defaultProbabilities[i];
                    hazardRates.Add(System.Math.Log(S1 / S2) / (t2 - t1));
                }

                var piecewiseFlatHazardRate = new RelinkableHandle<DefaultProbabilityTermStructure>();
                piecewiseFlatHazardRate.linkTo(new InterpolatedHazardRateCurve<BackwardFlat>(dates, hazardRates, new Thirty360()));

                // Testing credit default swap

                // Build the schedule
                var issueDate = new Date(20, Month.March, 2006);
                var maturity = new Date(20, Month.June, 2013);
                var cdsFrequency = Frequency.Semiannual;
                var cdsConvention = BusinessDayConvention.ModifiedFollowing;

                var schedule = new Schedule(issueDate, maturity, new Period(cdsFrequency), calendar,
                                                 cdsConvention, cdsConvention,
                                                 DateGeneration.Rule.Forward, false);

                // Build the CDS
                var recoveryRate = 0.25;
                var fixedRate = 0.0224;
                DayCounter dayCount = new Actual360();
                var cdsNotional = 100.0;

                var cds = new CreditDefaultSwap(Protection.Side.Seller, cdsNotional, fixedRate,
                                                              schedule, cdsConvention, dayCount, true, true);
                cds.setPricingEngine(new MidPointCdsEngine(piecewiseFlatHazardRate, recoveryRate, discountCurve));

                var calculatedNpv = cds.NPV();
                var calculatedFairRate = cds.fairSpread();

                var npv = -1.364048777;        // from Bloomberg we have 98.15598868 - 100.00;
                var fairRate = 0.0248429452; // from Bloomberg we have 0.0258378;

                var tolerance = 1e-9;

                if (System.Math.Abs(npv - calculatedNpv) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce the npv for the given credit-default swap\n"
                        + "    computed NPV:  " + calculatedNpv + "\n"
                        + "    Given NPV:     " + npv);
                }

                if (System.Math.Abs(fairRate - calculatedFairRate) > tolerance)
                {
                    QAssert.Fail("Failed to reproduce the fair rate for the given credit-default swap\n"
                                 + "    computed fair rate:  " + calculatedFairRate + "\n"
                                 + "    Given fair rate:     " + fairRate);
                }
            }
        }

        [Fact]
        public void testImpliedHazardRate()
        {
            // Testing implied hazard-rate for credit-default swaps...

            using (var backup = new SavedSettings())
            {

                // Initialize curves
                Calendar calendar = new TARGET();
                var today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);

                double h1 = 0.30, h2 = 0.40;
                DayCounter dayCounter = new Actual365Fixed();

                var dates = new List<Date>(3);
                var hazardRates = new List<double>(3);
                dates.Add(today);
                hazardRates.Add(h1);

                dates.Add(today + new Period(5, TimeUnit.Years));
                hazardRates.Add(h1);

                dates.Add(today + new Period(10, TimeUnit.Years));
                hazardRates.Add(h2);

                var probabilityCurve =
                   new RelinkableHandle<DefaultProbabilityTermStructure>();
                probabilityCurve.linkTo(new InterpolatedHazardRateCurve<BackwardFlat>(dates,
                                                                                      hazardRates,
                                                                                      dayCounter));

                var discountCurve = new RelinkableHandle<YieldTermStructure>();
                discountCurve.linkTo(new FlatForward(today, 0.03, new Actual360()));

                var frequency = Frequency.Semiannual;
                var convention = BusinessDayConvention.ModifiedFollowing;

                var issueDate = calendar.advance(today, -6, TimeUnit.Months);
                var fixedRate = 0.0120;
                DayCounter cdsDayCount = new Actual360();
                var notional = 10000.0;
                var recoveryRate = 0.4;

                double? latestRate = null;
                for (var n = 6; n <= 10; ++n)
                {
                    var maturity = calendar.advance(issueDate, n, TimeUnit.Years);
                    var schedule = new Schedule(issueDate, maturity, new Period(frequency), calendar,
                                                     convention, convention,
                                                     DateGeneration.Rule.Forward, false);

                    var cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
                                                                  schedule, convention, cdsDayCount, true, true);
                    cds.setPricingEngine(new MidPointCdsEngine(probabilityCurve, recoveryRate, discountCurve));

                    var NPV = cds.NPV();
                    var flatRate = cds.impliedHazardRate(NPV, discountCurve,
                                                            dayCounter,
                                                            recoveryRate);

                    if (flatRate < h1 || flatRate > h2)
                    {
                        QAssert.Fail("implied hazard rate outside expected range\n"
                                     + "    maturity: " + n + " years\n"
                                     + "    expected minimum: " + h1 + "\n"
                                     + "    expected maximum: " + h2 + "\n"
                                     + "    implied rate:     " + flatRate);
                    }

                    if (n > 6 && flatRate < latestRate)
                    {
                        QAssert.Fail("implied hazard rate decreasing with swap maturity\n"
                                     + "    maturity: " + n + " years\n"
                                     + "    previous rate: " + latestRate + "\n"
                                     + "    implied rate:  " + flatRate);
                    }

                    latestRate = flatRate;

                    var probability = new RelinkableHandle<DefaultProbabilityTermStructure>();
                    probability.linkTo(new FlatHazardRate(today, new Handle<Quote>(new SimpleQuote(flatRate)), dayCounter));

                    var cds2 = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
                                                                   schedule, convention, cdsDayCount, true, true);
                    cds2.setPricingEngine(new MidPointCdsEngine(probability, recoveryRate, discountCurve));

                    var NPV2 = cds2.NPV();
                    var tolerance = 1.0;
                    if (System.Math.Abs(NPV - NPV2) > tolerance)
                    {
                        QAssert.Fail("failed to reproduce NPV with implied rate\n"
                                     + "    expected:   " + NPV + "\n"
                                     + "    calculated: " + NPV2);
                    }
                }
            }
        }

        [Fact]
        public void testFairSpread()
        {
            // Testing fair-spread calculation for credit-default swaps...

            using (var backup = new SavedSettings())
            {

                // Initialize curves
                Calendar calendar = new TARGET();
                var today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);

                var hazardRate = new Handle<Quote>(new SimpleQuote(0.01234));
                var probabilityCurve =
                   new RelinkableHandle<DefaultProbabilityTermStructure>();
                probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

                var discountCurve =
                   new RelinkableHandle<YieldTermStructure>();
                discountCurve.linkTo(new FlatForward(today, 0.06, new Actual360()));

                // Build the schedule
                var issueDate = calendar.advance(today, -1, TimeUnit.Years);
                var maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
                var convention = BusinessDayConvention.Following;

                var schedule =
                   new MakeSchedule().from(issueDate)
                .to(maturity)
                .withFrequency(Frequency.Quarterly)
                .withCalendar(calendar)
                .withTerminationDateConvention(convention)
                .withRule(DateGeneration.Rule.TwentiethIMM).value();

                // Build the CDS
                var fixedRate = 0.001;
                DayCounter dayCount = new Actual360();
                var notional = 10000.0;
                var recoveryRate = 0.4;

                IPricingEngine engine = new MidPointCdsEngine(probabilityCurve, recoveryRate, discountCurve);

                var cds = new CreditDefaultSwap(Protection.Side.Seller, notional, fixedRate,
                                                              schedule, convention, dayCount, true, true);
                cds.setPricingEngine(engine);

                var fairRate = cds.fairSpread();

                var fairCds = new CreditDefaultSwap(Protection.Side.Seller, notional, fairRate,
                                                                  schedule, convention, dayCount, true, true);
                fairCds.setPricingEngine(engine);

                var fairNPV = fairCds.NPV();
                var tolerance = 1e-10;

                if (System.Math.Abs(fairNPV) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce null NPV with calculated fair spread\n"
                        + "    calculated spread: " + fairRate + "\n"
                        + "    calculated NPV:    " + fairNPV);
                }
            }
        }

        [Fact]
        public void testFairUpfront()
        {
            // Testing fair-upfront calculation for credit-default swaps...

            using (var backup = new SavedSettings())
            {
                // Initialize curves
                Calendar calendar = new TARGET();
                var today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);

                var hazardRate = new Handle<Quote>(new SimpleQuote(0.01234));
                var probabilityCurve =
                   new RelinkableHandle<DefaultProbabilityTermStructure>();
                probabilityCurve.linkTo(new FlatHazardRate(0, calendar, hazardRate, new Actual360()));

                var discountCurve =
                   new RelinkableHandle<YieldTermStructure>();
                discountCurve.linkTo(new FlatForward(today, 0.06, new Actual360()));

                // Build the schedule
                var issueDate = today;
                var maturity = calendar.advance(issueDate, 10, TimeUnit.Years);
                var convention = BusinessDayConvention.Following;

                var schedule =
                   new MakeSchedule().from(issueDate)
                .to(maturity)
                .withFrequency(Frequency.Quarterly)
                .withCalendar(calendar)
                .withTerminationDateConvention(convention)
                .withRule(DateGeneration.Rule.TwentiethIMM).value();

                // Build the CDS
                var fixedRate = 0.05;
                var upfront = 0.001;
                DayCounter dayCount = new Actual360();
                var notional = 10000.0;
                var recoveryRate = 0.4;

                IPricingEngine engine = new MidPointCdsEngine(probabilityCurve, recoveryRate, discountCurve, true);

                var cds = new CreditDefaultSwap(Protection.Side.Seller, notional, upfront, fixedRate,
                                                              schedule, convention, dayCount, true, true);
                cds.setPricingEngine(engine);

                var fairUpfront = cds.fairUpfront();

                var fairCds = new CreditDefaultSwap(Protection.Side.Seller, notional,
                                                                  fairUpfront, fixedRate, schedule, convention, dayCount, true, true);
                fairCds.setPricingEngine(engine);

                var fairNPV = fairCds.NPV();
                var tolerance = 1e-10;

                if (System.Math.Abs(fairNPV) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce null NPV with calculated fair upfront\n"
                        + "    calculated upfront: " + fairUpfront + "\n"
                        + "    calculated NPV:     " + fairNPV);
                }

                // same with null upfront to begin with
                upfront = 0.0;
                var cds2 = new CreditDefaultSwap(Protection.Side.Seller, notional, upfront, fixedRate,
                                                               schedule, convention, dayCount, true, true);
                cds2.setPricingEngine(engine);

                fairUpfront = cds2.fairUpfront();

                var fairCds2 = new CreditDefaultSwap(Protection.Side.Seller, notional,
                                                                   fairUpfront, fixedRate, schedule, convention, dayCount, true, true);
                fairCds2.setPricingEngine(engine);

                fairNPV = fairCds2.NPV();

                if (System.Math.Abs(fairNPV) > tolerance)
                {
                    QAssert.Fail(
                        "Failed to reproduce null NPV with calculated fair upfront\n"
                        + "    calculated upfront: " + fairUpfront + "\n"
                        + "    calculated NPV:     " + fairNPV);
                }
            }
        }

        [Fact]
        public void testIsdaEngine()
        {
            // Testing ISDA engine calculations for credit-default swaps

            var backup = new SavedSettings();

            var tradeDate = new Date(21, Month.May, 2009);
            Settings.setEvaluationDate(tradeDate);


            //build an ISDA compliant yield curve
            //data comes from Markit published rates
            var isdaRateHelpers = new List<RateHelper>();
            int[] dep_tenors = { 1, 2, 3, 6, 9, 12 };
            double[] dep_quotes = {0.003081,
                                0.005525,
                                0.007163,
                                0.012413,
                                0.014,
                                0.015488
                               };

            for (var i = 0; i < dep_tenors.Length; i++)
            {
                isdaRateHelpers.Add(new DepositRateHelper(dep_quotes[i], new Period(dep_tenors[i], TimeUnit.Months), 2,
                                                          new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing, false, new Actual360())
                                   );
            }
            int[] swap_tenors = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 15, 20, 25, 30 };
            double[] swap_quotes = {0.011907,
                                 0.01699,
                                 0.021198,
                                 0.02444,
                                 0.026937,
                                 0.028967,
                                 0.030504,
                                 0.031719,
                                 0.03279,
                                 0.034535,
                                 0.036217,
                                 0.036981,
                                 0.037246,
                                 0.037605
                                };

            var isda_ibor = new IborIndex("IsdaIbor", new Period(3, TimeUnit.Months), 2, new USDCurrency(),
                                                new WeekendsOnly(), BusinessDayConvention.ModifiedFollowing, false, new Actual360());
            for (var i = 0; i < swap_tenors.Length; i++)
            {
                isdaRateHelpers.Add(new SwapRateHelper(swap_quotes[i], new Period(swap_tenors[i], TimeUnit.Years),
                                                       new WeekendsOnly(), Frequency.Semiannual, BusinessDayConvention.ModifiedFollowing, new Thirty360(),
                                                       isda_ibor));
            }

            var discountCurve = new RelinkableHandle<YieldTermStructure>();
            discountCurve.linkTo(new PiecewiseYieldCurve<Discount, LogLinear>(0, new WeekendsOnly(), isdaRateHelpers,
                                                                              new Actual365Fixed()));


            var probabilityCurve = new RelinkableHandle<DefaultProbabilityTermStructure>();
            Date[] termDates = { new Date(20, Month.June, 2010),
                 new Date(20, Month.June, 2011),
                 new Date(20, Month.June, 2012),
                 new Date(20, Month.June, 2016),
                 new Date(20, Month.June, 2019)
         };
            double[] spreads = { 0.001, 0.1 };
            double[] recoveries = { 0.2, 0.4 };

            double[] markitValues = {97798.29358, //0.001
                                  97776.11889, //0.001
                                  -914971.5977, //0.1
                                  -894985.6298, //0.1
                                  186921.3594, //0.001
                                  186839.8148, //0.001
                                  -1646623.672, //0.1
                                  -1579803.626, //0.1
                                  274298.9203,
                                  274122.4725,
                                  -2279730.93,
                                  -2147972.527,
                                  592420.2297,
                                  591571.2294,
                                  -3993550.206,
                                  -3545843.418,
                                  797501.1422,
                                  795915.9787,
                                  -4702034.688,
                                  -4042340.999
                                 };
#if !QL_USE_INDEXED_COUPON
            var tolerance = 1.0e-2; //TODO Check calculation , tolerance must be 1.0e-6;
#else
         /* The risk-free curve is a bit off. We might skip the tests
            altogether and rely on running them with indexed coupons
            disabled, but leaving them can be useful anyway. */
         double tolerance = 1.0e-3;
#endif

            var l = 0;

            for (var i = 0; i < termDates.Length; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    for (var k = 0; k < 2; k++)
                    {

                        var quotedTrade = new MakeCreditDefaultSwap(termDates[i], spreads[j])
                        .withNominal(10000000.0).value();

                        var h = quotedTrade.impliedHazardRate(0.0,
                                                                 discountCurve,
                                                                 new Actual365Fixed(),
                                                                 recoveries[k],
                                                                 1e-10,
                                                                 PricingModel.ISDA);

                        probabilityCurve.linkTo(new FlatHazardRate(0, new WeekendsOnly(), h, new Actual365Fixed()));

                        var engine = new IsdaCdsEngine(probabilityCurve, recoveries[k], discountCurve);

                        var conventionalTrade = new MakeCreditDefaultSwap(termDates[i], 0.01)
                        .withNominal(10000000.0)
                        .withPricingEngine(engine).value();

                        var x = conventionalTrade.notional().Value;
                        var y = conventionalTrade.fairUpfront();

                        var calculated = System.Math.Abs(x * y - markitValues[l]);

                        QAssert.IsTrue(calculated <= tolerance);

                        l++;

                    }
                }
            }
        }

    }
}
