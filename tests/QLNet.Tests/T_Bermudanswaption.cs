﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)

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
using Xunit;
using QLNet;
using QLNet.Indexes;
using QLNet.Time;
using QLNet.Instruments;
using QLNet.Termstructures;
using QLNet.Models;
using QLNet.Cashflows;
using QLNet.Indexes.Ibor;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.PricingEngines.Swap;
using QLNet.PricingEngines.swaption;
using QLNet.PricingEngines.vanilla;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Bermudanswaption : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_Bermudanswaption()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        [JetBrains.Annotations.PublicAPI] public class CommonVars
        {
            // global data
            public Date today, settlement;
            public Calendar calendar;

            // underlying swap parameters
            public int startYears, length;
            public VanillaSwap.Type type;
            public double nominal;
            public BusinessDayConvention fixedConvention, floatingConvention;
            public Frequency fixedFrequency, floatingFrequency;
            public DayCounter fixedDayCount;
            public IborIndex index;
            public int settlementDays;

            public RelinkableHandle<YieldTermStructure> termStructure;

            // setup
            public CommonVars()
            {
                startYears = 1;
                length = 5;
                type = VanillaSwap.Type.Payer;
                nominal = 1000.0;
                settlementDays = 2;
                fixedConvention = BusinessDayConvention.Unadjusted;
                floatingConvention = BusinessDayConvention.ModifiedFollowing;
                fixedFrequency = Frequency.Annual;
                floatingFrequency = Frequency.Semiannual;
                fixedDayCount = new Thirty360();

                termStructure = new RelinkableHandle<YieldTermStructure>();
                termStructure.linkTo(Utilities.flatRate(new Date(19, Month.February, 2002), 0.04875825, new Actual365Fixed()));

                index = new Euribor6M(termStructure);
                calendar = index.fixingCalendar();
                today = calendar.adjust(Date.Today);
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
            }

            // utilities
            public VanillaSwap makeSwap(double fixedRate)
            {
                var start = calendar.advance(settlement, startYears, TimeUnit.Years);
                var maturity = calendar.advance(start, length, TimeUnit.Years);
                var fixedSchedule = new Schedule(start, maturity,
                                                      new Period(fixedFrequency),
                                                      calendar,
                                                      fixedConvention,
                                                      fixedConvention,
                                                      DateGeneration.Rule.Forward, false);
                var floatSchedule = new Schedule(start, maturity,
                                                      new Period(floatingFrequency),
                                                      calendar,
                                                      floatingConvention,
                                                      floatingConvention,
                                                      DateGeneration.Rule.Forward, false);
                var swap =
                   new VanillaSwap(type, nominal,
                                   fixedSchedule, fixedRate, fixedDayCount,
                                   floatSchedule, index, 0.0,
                                   index.dayCounter());
                swap.setPricingEngine((IPricingEngine)new DiscountingSwapEngine(termStructure));
                return swap;
            }
        }

        [Fact]
        public void testCachedValues()
        {

            //("Testing Bermudan swaption against cached values...");

            var vars = new CommonVars();

            vars.today = new Date(15, Month.February, 2002);

            Settings.setEvaluationDate(vars.today);

            vars.settlement = new Date(19, Month.February, 2002);
            // flat yield term structure impling 1x5 swap at 5%
            vars.termStructure.linkTo(Utilities.flatRate(vars.settlement,
                                                         0.04875825,
                                                         new Actual365Fixed()));

            var atmRate = vars.makeSwap(0.0).fairRate();

            var itmSwap = vars.makeSwap(0.8 * atmRate);
            var atmSwap = vars.makeSwap(atmRate);
            var otmSwap = vars.makeSwap(1.2 * atmRate);

            double a = 0.048696, sigma = 0.0058904;
            ShortRateModel model = new HullWhite(vars.termStructure, a, sigma);
            var exerciseDates = new List<Date>();
            var leg = atmSwap.fixedLeg();

            for (var i = 0; i < leg.Count; i++)
            {
                var coupon = (Coupon)leg[i];
                exerciseDates.Add(coupon.accrualStartDate());
            }

            Exercise exercise = new BermudanExercise(exerciseDates);
            IPricingEngine treeEngine = new TreeSwaptionEngine(model, 50);
            IPricingEngine fdmEngine = new FdHullWhiteSwaptionEngine(model as HullWhite);

#if QL_USE_INDEXED_COUPON
         double itmValue = 42.2413, atmValue = 12.8789, otmValue = 2.4759;
         double itmValueFdm = 42.2111, atmValueFdm = 12.8879, otmValueFdm = 2.44443;
#else
            double itmValue = 42.2470, atmValue = 12.8826, otmValue = 2.4769;
            double itmValueFdm = 42.2091, atmValueFdm = 12.8864, otmValueFdm = 2.4437;
#endif

            var tolerance = 1.0e-4;

            var swaption = new Swaption(itmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - itmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached in-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + itmValue);
            }

            swaption.setPricingEngine(fdmEngine);
            if (System.Math.Abs(swaption.NPV() - itmValueFdm) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached in-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + itmValueFdm);
            }

            swaption = new Swaption(atmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - atmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached at-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + atmValue);
            }

            swaption.setPricingEngine(fdmEngine);
            if (System.Math.Abs(swaption.NPV() - atmValueFdm) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached at-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + atmValueFdm);
            }

            swaption = new Swaption(otmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - otmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached out-of-the-money "
                             + "swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + otmValue);
            }

            swaption.setPricingEngine(fdmEngine);
            if (System.Math.Abs(swaption.NPV() - otmValueFdm) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached out-of-the-money "
                             + "swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + otmValueFdm);
            }

            for (var j = 0; j < exerciseDates.Count; j++)
            {
                exerciseDates[j] = vars.calendar.adjust(exerciseDates[j] - 10);
            }

            exercise = new BermudanExercise(exerciseDates);

#if QL_USE_INDEXED_COUPON
         itmValue = 42.1917; atmValue = 12.7788; otmValue = 2.4388;
#else
            itmValue = 42.1974; atmValue = 12.7825; otmValue = 2.4399;
#endif

            swaption = new Swaption(itmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - itmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached in-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + itmValue);
            }

            swaption = new Swaption(atmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - atmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached at-the-money swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + atmValue);
            }

            swaption = new Swaption(otmSwap, exercise);
            swaption.setPricingEngine(treeEngine);
            if (System.Math.Abs(swaption.NPV() - otmValue) > tolerance)
            {
                QAssert.Fail("failed to reproduce cached out-of-the-money "
                             + "swaption value:\n"
                             + "calculated: " + swaption.NPV() + "\n"
                             + "expected:   " + otmValue);
            }
        }
    }
}
