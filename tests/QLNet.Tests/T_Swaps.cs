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

using System;
using System.Collections.Generic;
using QLNet.Cashflows;
using QLNet.Currencies;
using Xunit;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Time;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Math.Interpolations;
using QLNet.Indexes.Ibor;
using QLNet.PricingEngines.Swap;
using QLNet.Termstructures.Yield;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Swaps : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_Swaps()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        class CommonVars
        {
            // global data
            public Date today, settlement;
            public VanillaSwap.Type type;
            public double nominal;
            public Calendar calendar;
            public BusinessDayConvention fixedConvention, floatingConvention;
            public Frequency fixedFrequency, floatingFrequency;
            public DayCounter fixedDayCount;
            public IborIndex index;
            public int settlementDays;
            public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

            // utilities
            public VanillaSwap makeSwap(int length, double fixedRate, double floatingSpread)
            {
                var maturity = calendar.advance(settlement, length, TimeUnit.Years, floatingConvention);
                var fixedSchedule = new Schedule(settlement, maturity, new Period(fixedFrequency),
                                                      calendar, fixedConvention, fixedConvention, DateGeneration.Rule.Forward, false);
                var floatSchedule = new Schedule(settlement, maturity, new Period(floatingFrequency),
                                                      calendar, floatingConvention, floatingConvention, DateGeneration.Rule.Forward, false);
                var swap = new VanillaSwap(type, nominal, fixedSchedule, fixedRate, fixedDayCount,
                                                   floatSchedule, index, floatingSpread, index.dayCounter());
                swap.setPricingEngine(new DiscountingSwapEngine(termStructure));
                return swap;
            }

            public CommonVars()
            {
                type = VanillaSwap.Type.Payer;
                settlementDays = 2;
                nominal = 100.0;
                fixedConvention = BusinessDayConvention.Unadjusted;
                floatingConvention = BusinessDayConvention.ModifiedFollowing;
                fixedFrequency = Frequency.Annual;
                floatingFrequency = Frequency.Semiannual;
                fixedDayCount = new Thirty360();

                index = new Euribor(new Period(floatingFrequency), termStructure);

                calendar = index.fixingCalendar();
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);

                termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new Actual365Fixed()));
            }
        }

        [Fact]
        public void testFairRate()
        {
            // Testing vanilla-swap calculation of fair fixed rate

            var vars = new CommonVars();

            var lengths = new int[] { 1, 2, 5, 10, 20 };
            var spreads = new double[] { -0.001, -0.01, 0.0, 0.01, 0.001 };

            for (var i = 0; i < lengths.Length; i++)
            {
                for (var j = 0; j < spreads.Length; j++)
                {

                    var swap = vars.makeSwap(lengths[i], 0.0, spreads[j]);
                    swap = vars.makeSwap(lengths[i], swap.fairRate(), spreads[j]);
                    if (System.Math.Abs(swap.NPV()) > 1.0e-10)
                    {
                        QAssert.Fail("recalculating with implied rate:\n"
                                     + "    length: " + lengths[i] + " years\n"
                                     + "    floating spread: "
                                     + spreads[j] + "\n"
                                     + "    swap value: " + swap.NPV());
                    }
                }
            }
        }
        [Fact]
        public void testFairSpread()
        {
            // Testing vanilla-swap calculation of fair floating spread
            var vars = new CommonVars();

            var lengths = new int[] { 1, 2, 5, 10, 20 };
            var rates = new double[] { 0.04, 0.05, 0.06, 0.07 };

            for (var i = 0; i < lengths.Length; i++)
            {
                for (var j = 0; j < rates.Length; j++)
                {

                    var swap = vars.makeSwap(lengths[i], rates[j], 0.0);
                    swap = vars.makeSwap(lengths[i], rates[j], swap.fairSpread());
                    if (System.Math.Abs(swap.NPV()) > 1.0e-10)
                    {
                        QAssert.Fail("recalculating with implied spread:\n"
                                     + "    length: " + lengths[i] + " years\n"
                                     + "    fixed rate: "
                                     + rates[j] + "\n"
                                     + "    swap value: " + swap.NPV());
                    }
                }
            }
        }
        [Fact]
        public void testRateDependency()
        {
            // Testing vanilla-swap dependency on fixed rate
            var vars = new CommonVars();

            var lengths = new int[] { 1, 2, 5, 10, 20 };
            var spreads = new double[] { -0.001, -0.01, 0.0, 0.01, 0.001 };
            var rates = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };

            for (var i = 0; i < lengths.Length; i++)
            {
                for (var j = 0; j < spreads.Length; j++)
                {

                    // store the results for different rates...
                    var swap_values = new List<double>();
                    for (var k = 0; k < rates.Length; k++)
                    {
                        var swap = vars.makeSwap(lengths[i], rates[k], spreads[j]);
                        swap_values.Add(swap.NPV());
                    }

                    // and check that they go the right way
                    for (var z = 0; z < swap_values.Count - 1; z++)
                    {
                        if (swap_values[z] < swap_values[z + 1])
                        {
                            QAssert.Fail(
                                "NPV is increasing with the fixed rate in a swap: \n"
                                + "    length: " + lengths[i] + " years\n"
                                + "    value:  " + swap_values[z]
                                + " paying fixed rate: " + rates[z] + "\n"
                                + "    value:  " + swap_values[z + 1]
                                + " paying fixed rate: " + rates[z + 1]);
                        }
                    }
                }
            }
        }
        [Fact]
        public void testSpreadDependency()
        {
            // Testing vanilla-swap dependency on floating spread
            var vars = new CommonVars();

            var lengths = new int[] { 1, 2, 5, 10, 20 };
            var spreads = new double[] { -0.01, -0.002, -0.001, 0.0, 0.001, 0.002, 0.01 };
            var rates = new double[] { 0.04, 0.05, 0.06, 0.07 };

            for (var i = 0; i < lengths.Length; i++)
            {
                for (var j = 0; j < rates.Length; j++)
                {

                    // store the results for different rates...
                    var swap_values = new List<double>();
                    for (var k = 0; k < spreads.Length; k++)
                    {
                        var swap = vars.makeSwap(lengths[i], rates[j], spreads[k]);
                        swap_values.Add(swap.NPV());
                    }

                    // and check that they go the right way
                    for (var z = 0; z < swap_values.Count - 1; z++)
                    {
                        if (swap_values[z] > swap_values[z + 1])
                        {
                            QAssert.Fail(
                                "NPV is decreasing with the floating spread in a swap: \n"
                                + "    length: " + lengths[i] + " years\n"
                                + "    value:  " + swap_values[z]
                                + " receiving spread: " + rates[z] + "\n"
                                + "    value:  " + swap_values[z + 1]
                                + " receiving spread: " + rates[z + 1]);
                        }
                    }
                }
            }
        }
        [Fact]
        public void testInArrears()
        {
            // Testing in-arrears swap calculation
            var vars = new CommonVars();

            /* See Hull, 4th ed., page 550
               Note: the calculation in the book is wrong (work out the adjustment and you'll get 0.05 + 0.000115 T1) */
            var maturity = vars.today + new Period(5, TimeUnit.Years);
            Calendar calendar = new NullCalendar();
            var schedule = new Schedule(vars.today, maturity, new Period(Frequency.Annual), calendar,
                                             BusinessDayConvention.Following, BusinessDayConvention.Following,
                                             DateGeneration.Rule.Forward, false);
            DayCounter dayCounter = new SimpleDayCounter();

            var nominals = new List<double>() { 100000000.0 };

            var index = new IborIndex("dummy", new Period(1, TimeUnit.Years), 0, new EURCurrency(), calendar,
                                            BusinessDayConvention.Following, false, dayCounter, vars.termStructure);
            var oneYear = 0.05;
            var r = System.Math.Log(1.0 + oneYear);
            vars.termStructure.linkTo(Utilities.flatRate(vars.today, r, dayCounter));

            var coupons = new List<double>() { oneYear };
            List<CashFlow> fixedLeg = new FixedRateLeg(schedule)
            .withCouponRates(coupons, dayCounter)
            .withNotionals(nominals);

            var gearings = new List<double>();
            var spreads = new List<double>();
            var fixingDays = 0;

            var capletVolatility = 0.22;
            var vol = new Handle<OptionletVolatilityStructure>(
               new ConstantOptionletVolatility(vars.today, new NullCalendar(),
                                               BusinessDayConvention.Following, capletVolatility, dayCounter));
            IborCouponPricer pricer = new BlackIborCouponPricer(vol);

            List<CashFlow> floatingLeg = new IborLeg(schedule, index)
            .withPaymentDayCounter(dayCounter)
            .withFixingDays(fixingDays)
            .withGearings(gearings)
            .withSpreads(spreads)
            .inArrears()
            .withNotionals(nominals);
            Cashflows.Utils.setCouponPricer(floatingLeg, pricer);

            var swap = new Swap(floatingLeg, fixedLeg);
            swap.setPricingEngine(new DiscountingSwapEngine(vars.termStructure));

            var storedValue = -144813.0;
            var tolerance = 1.0;

            if (System.Math.Abs(swap.NPV() - storedValue) > tolerance)
            {
                QAssert.Fail("Wrong NPV calculation:\n"
                             + "    expected:   " + storedValue + "\n"
                             + "    calculated: " + swap.NPV());
            }
        }
        [Fact]
        public void testCachedValue()
        {
            // Testing vanilla-swap calculation against cached value
            var vars = new CommonVars();

            vars.today = new Date(17, Month.June, 2002);
            Settings.setEvaluationDate(vars.today);
            vars.settlement = vars.calendar.advance(vars.today, vars.settlementDays, TimeUnit.Days);
            vars.termStructure.linkTo(Utilities.flatRate(vars.settlement, 0.05, new Actual365Fixed()));

            var swap = vars.makeSwap(10, 0.06, 0.001);
#if QL_USE_INDEXED_COUPON
         double cachedNPV   = -5.872342992212;
#else
            var cachedNPV = -5.872863313209;
#endif

            if (System.Math.Abs(swap.NPV() - cachedNPV) > 1.0e-11)
            {
                QAssert.Fail("failed to reproduce cached swap value:\n"
                             + "    calculated: " + swap.NPV() + "\n"
                             + "    expected:   " + cachedNPV);
            }
        }
        [Fact]
        public void testFixing()
        {
            var tradeDate = new Date(17, Month.April, 2015);
            Calendar calendar = new UnitedKingdom();
            var settlementDate = calendar.advance(tradeDate, 2, TimeUnit.Days, BusinessDayConvention.Following);
            var maturityDate = calendar.advance(settlementDate, 5, TimeUnit.Years, BusinessDayConvention.Following);

            var valueDate = new Date(20, Month.April, 2015);
            Settings.setEvaluationDate(valueDate);

            var dates = new List<Date>();
            dates.Add(valueDate);
            dates.Add(valueDate + new Period(1, TimeUnit.Years));
            dates.Add(valueDate + new Period(2, TimeUnit.Years));
            dates.Add(valueDate + new Period(5, TimeUnit.Years));
            dates.Add(valueDate + new Period(10, TimeUnit.Years));
            dates.Add(valueDate + new Period(20, TimeUnit.Years));

            var rates = new List<double>();
            rates.Add(0.01);
            rates.Add(0.01);
            rates.Add(0.01);
            rates.Add(0.01);
            rates.Add(0.01);
            rates.Add(0.01);

            var discountCurveHandle = new RelinkableHandle<YieldTermStructure>();
            var forecastCurveHandle = new RelinkableHandle<YieldTermStructure>();
            var index = new GBPLibor(new Period(6, TimeUnit.Months), forecastCurveHandle);
            var zeroCurve = new InterpolatedZeroCurve<Linear>(dates, rates, new Actual360(), new Linear());
            var fixedSchedule = new Schedule(settlementDate, maturityDate, new Period(1, TimeUnit.Years), calendar, BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward, false);
            var floatSchedule = new Schedule(settlementDate, maturityDate, index.tenor(), calendar, BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward, false);
            var swap = new VanillaSwap(VanillaSwap.Type.Payer, 1000000, fixedSchedule, 0.01, new Actual360(), floatSchedule, index, 0, new Actual360());
            discountCurveHandle.linkTo(zeroCurve);
            forecastCurveHandle.linkTo(zeroCurve);
            var swapEngine = new DiscountingSwapEngine(discountCurveHandle, false, null);
            swap.setPricingEngine(swapEngine);

            try
            {
                var npv = swap.NPV();
            }
            catch (Exception ex)
            {
                QAssert.Fail(ex.Message);
            }
        }

    }

}
