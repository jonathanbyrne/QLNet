/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using QLNet.Time;
using QLNet.Indexes;
using QLNet.Indexes.Ibor;
using QLNet.PricingEngines.swaption;
using QLNet.Instruments;
using QLNet.PricingEngines.Swap;
using QLNet.Termstructures;
using QLNet.Quotes;
using QLNet.Termstructures.Yield;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_Swaption : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_Swaption()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        public Period[] exercises = new Period[] { new Period(1, TimeUnit.Years),
                new Period(2, TimeUnit.Years),
                new Period(3, TimeUnit.Years),
                new Period(5, TimeUnit.Years),
                new Period(7, TimeUnit.Years),
                new Period(10, TimeUnit.Years)
      };

        public Period[] lengths = new Period[] { new Period(1, TimeUnit.Years),
                new Period(2, TimeUnit.Years),
                new Period(3, TimeUnit.Years),
                new Period(5, TimeUnit.Years),
                new Period(7, TimeUnit.Years),
                new Period(10, TimeUnit.Years),
                new Period(15, TimeUnit.Years),
                new Period(20, TimeUnit.Years)
      };

        public VanillaSwap.Type[] type = new VanillaSwap.Type[] { VanillaSwap.Type.Receiver, VanillaSwap.Type.Payer };


        [JetBrains.Annotations.PublicAPI] public class CommonVars
        {
            // global data
            public Date today, settlement;
            public double nominal;
            public Calendar calendar;
            public BusinessDayConvention fixedConvention, floatingConvention;
            public Frequency fixedFrequency;
            public DayCounter fixedDayCount;
            public Period floatingTenor;
            public IborIndex index;
            public int settlementDays;
            public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

            // utilities
            public Swaption makeSwaption(VanillaSwap swap, Date exercise, double volatility,
                                         Settlement.Type settlementType = Settlement.Type.Physical,
                                         Settlement.Method settlementMethod = Settlement.Method.PhysicalOTC,
                                         BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel model = BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel.SwapRate)
            {
                var vol = new Handle<Quote>(new SimpleQuote(volatility));
                IPricingEngine engine = new BlackSwaptionEngine(termStructure, vol, new Actual365Fixed(), 0.0, model);
                var result = new Swaption(swap, new EuropeanExercise(exercise), settlementType, settlementMethod);
                result.setPricingEngine(engine);
                return result;
            }

            public IPricingEngine makeEngine(double volatility,
                                             BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel model = BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel.SwapRate)
            {
                var h = new Handle<Quote>(new SimpleQuote(volatility));
                return new BlackSwaptionEngine(termStructure, h, new Actual365Fixed(), 0.0, model);
            }

            public CommonVars()
            {
                settlementDays = 2;
                nominal = 1000000.0;
                fixedConvention = BusinessDayConvention.Unadjusted;

                fixedFrequency = Frequency.Annual;
                fixedDayCount = new Thirty360();

                index = new Euribor6M(termStructure);
                floatingConvention = index.businessDayConvention();
                floatingTenor = index.tenor();
                calendar = index.fixingCalendar();
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);

                termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new Actual365Fixed()));
            }
        }

        [Fact]
        public void testStrikeDependency()
        {
            // Testing swaption dependency on strike
            var vars = new CommonVars();
            var strikes = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };

            for (var i = 0; i < exercises.Length; i++)
            {
                for (var j = 0; j < lengths.Length; j++)
                {
                    for (var k = 0; k < type.Length; k++)
                    {
                        var exerciseDate = vars.calendar.advance(vars.today,
                                                                  exercises[i]);
                        var startDate = vars.calendar.advance(exerciseDate,
                                                               vars.settlementDays, TimeUnit.Days);
                        // store the results for different rates...
                        List<double> values = new InitializedList<double>(strikes.Length);
                        List<double> values_cash = new InitializedList<double>(strikes.Length);
                        var vol = 0.20;

                        for (var l = 0; l < strikes.Length; l++)
                        {
                            VanillaSwap swap = new MakeVanillaSwap(lengths[j], vars.index, strikes[l])
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(0.0)
                            .withType(type[k]);
                            var swaption = vars.makeSwaption(swap, exerciseDate, vol);
                            // FLOATING_POINT_EXCEPTION
                            values[l] = swaption.NPV();
                            var swaption_cash = vars.makeSwaption(swap, exerciseDate, vol,
                                                                       Settlement.Type.Cash,
                                                                       Settlement.Method.ParYieldCurve);
                            values_cash[l] = swaption_cash.NPV();
                        }

                        // and check that they go the right way
                        if (type[k] == VanillaSwap.Type.Payer)
                        {
                            for (var z = 0; z < values.Count - 1; z++)
                            {
                                if (values[z] < values[z + 1])
                                {
                                    QAssert.Fail("NPV of Payer swaption with delivery settlement" +
                                                 "is increasing with the strike:" +
                                                 "\noption tenor: " + exercises[i] +
                                                 "\noption date:  " + exerciseDate +
                                                 "\nvolatility:   " + vol +
                                                 "\nswap tenor:   " + lengths[j] +
                                                 "\nvalue:        " + values[z] + " at strike: " + strikes[z] +
                                                 "\nvalue:        " + values[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                            for (var z = 0; z < values_cash.Count - 1; z++)
                            {
                                if (values_cash[z] < values_cash[z + 1])
                                {
                                    QAssert.Fail("NPV of Payer swaption with cash settlement" +
                                                 "is increasing with the strike:" +
                                                 "\noption tenor: " + exercises[i] +
                                                 "\noption date:  " + exerciseDate +
                                                 "\nvolatility:   " + vol +
                                                 "\nswap tenor:   " + lengths[j] +
                                                 "\nvalue:        " + values_cash[z] + " at strike: " + strikes[z] +
                                                 "\nvalue:        " + values_cash[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                        }
                        else
                        {
                            for (var z = 0; z < values.Count - 1; z++)
                            {
                                if (values[z] > values[z + 1])
                                {
                                    QAssert.Fail("NPV of Receiver swaption with delivery settlement" +
                                                 "is increasing with the strike:" +
                                                 "\noption tenor: " + exercises[i] +
                                                 "\noption date:  " + exerciseDate +
                                                 "\nvolatility:   " + vol +
                                                 "\nswap tenor:   " + lengths[j] +
                                                 "\nvalue:        " + values[z] + " at strike: " + strikes[z] +
                                                 "\nvalue:        " + values[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                            for (var z = 0; z < values_cash.Count - 1; z++)
                            {
                                if (values[z] > values[z + 1])
                                {
                                    QAssert.Fail("NPV of Receiver swaption with cash settlement" +
                                                 "is increasing with the strike:" +
                                                 "\noption tenor: " + exercises[i] +
                                                 "\noption date:  " + exerciseDate +
                                                 "\nvolatility:   " + vol +
                                                 "\nswap tenor:   " + lengths[j] +
                                                 "\nvalue:        " + values_cash[z] + " at strike: " + strikes[z] +
                                                 "\nvalue:        " + values_cash[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testSpreadDependency()
        {
            // Testing swaption dependency on spread
            var vars = new CommonVars();

            double[] spreads = { -0.002, -0.001, 0.0, 0.001, 0.002 };

            for (var i = 0; i < exercises.Length; i++)
            {
                for (var j = 0; j < lengths.Length; j++)
                {
                    for (var k = 0; k < type.Length; k++)
                    {
                        var exerciseDate = vars.calendar.advance(vars.today,
                                                                  exercises[i]);
                        var startDate =
                           vars.calendar.advance(exerciseDate,
                                                 vars.settlementDays, TimeUnit.Days);
                        // store the results for different rates...
                        List<double> values = new InitializedList<double>(spreads.Length);
                        List<double> values_cash = new InitializedList<double>(spreads.Length);
                        for (var l = 0; l < spreads.Length; l++)
                        {
                            VanillaSwap swap =
                               new MakeVanillaSwap(lengths[j], vars.index, 0.06)
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(spreads[l])
                            .withType(type[k]);
                            var swaption =
                               vars.makeSwaption(swap, exerciseDate, 0.20);
                            // FLOATING_POINT_EXCEPTION
                            values[l] = swaption.NPV();
                            var swaption_cash =
                               vars.makeSwaption(swap, exerciseDate, 0.20,
                                                 Settlement.Type.Cash,
                                                 Settlement.Method.ParYieldCurve);
                            values_cash[l] = swaption_cash.NPV();
                        }
                        // and check that they go the right way
                        if (type[k] == VanillaSwap.Type.Payer)
                        {
                            for (var n = 0; n < spreads.Length - 1; n++)
                            {
                                if (values[n] > values[n + 1])
                                {
                                    QAssert.Fail("NPV is decreasing with the spread " +
                                                 "in a payer swaption (physical delivered):" +
                                                 "\nexercise date: " + exerciseDate +
                                                 "\nlength:        " + lengths[j] +
                                                 "\nvalue:         " + values[n] + " for spread: " + spreads[n] +
                                                 "\nvalue:         " + values[n + 1] + " for spread: " + spreads[n + 1]);
                                }

                                if (values_cash[n] > values_cash[n + 1])
                                {
                                    QAssert.Fail("NPV is decreasing with the spread " +
                                                 "in a payer swaption (cash delivered):" +
                                                 "\nexercise date: " + exerciseDate +
                                                 "\nlength: " + lengths[j] +
                                                 "\nvalue:  " + values_cash[n] + " for spread: " + spreads[n] +
                                                 "\nvalue:  " + values_cash[n + 1] + " for spread: " + spreads[n + 1]);
                                }
                            }
                        }
                        else
                        {
                            for (var n = 0; n < spreads.Length - 1; n++)
                            {
                                if (values[n] < values[n + 1])
                                {
                                    QAssert.Fail("NPV is increasing with the spread " +
                                                 "in a receiver swaption (physical delivered):" +
                                                 "\nexercise date: " + exerciseDate +
                                                 "\nlength: " + lengths[j] +
                                                 "\nvalue:  " + values[n] + " for spread: " + spreads[n] +
                                                 "\nvalue:  " + values[n + 1] + " for spread: " + spreads[n + 1]);
                                }

                                if (values_cash[n] < values_cash[n + 1])
                                {
                                    QAssert.Fail("NPV is increasing with the spread " +
                                                 "in a receiver swaption (cash delivered):" +
                                                 "\nexercise date: " + exerciseDate +
                                                 "\nlength: " + lengths[j] +
                                                 "\nvalue:  " + values_cash[n] + " for spread: " + spreads[n] +
                                                 "\nvalue:  " + values_cash[n + 1] + " for spread: " + spreads[n + 1]);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testSpreadTreatment()
        {
            // Testing swaption treatment of spread
            var vars = new CommonVars();

            double[] spreads = { -0.002, -0.001, 0.0, 0.001, 0.002 };

            for (var i = 0; i < exercises.Length; i++)
            {
                for (var j = 0; j < lengths.Length; j++)
                {
                    for (var k = 0; k < type.Length; k++)
                    {
                        var exerciseDate = vars.calendar.advance(vars.today,
                                                                  exercises[i]);
                        var startDate =
                           vars.calendar.advance(exerciseDate,
                                                 vars.settlementDays, TimeUnit.Days);
                        for (var l = 0; l < spreads.Length; l++)
                        {
                            VanillaSwap swap =
                               new MakeVanillaSwap(lengths[j], vars.index, 0.06)
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(spreads[l])
                            .withType(type[k]);
                            // FLOATING_POINT_EXCEPTION
                            var correction = spreads[l] *
                                             swap.floatingLegBPS() /
                                             swap.fixedLegBPS();
                            VanillaSwap equivalentSwap =
                               new MakeVanillaSwap(lengths[j], vars.index, 0.06 + correction)
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(0.0)
                            .withType(type[k]);
                            var swaption1 =
                               vars.makeSwaption(swap, exerciseDate, 0.20);
                            var swaption2 =
                               vars.makeSwaption(equivalentSwap, exerciseDate, 0.20);
                            var swaption1_cash =
                               vars.makeSwaption(swap, exerciseDate, 0.20,
                                                 Settlement.Type.Cash,
                                                 Settlement.Method.ParYieldCurve);
                            var swaption2_cash =
                               vars.makeSwaption(equivalentSwap, exerciseDate, 0.20,
                                                 Settlement.Type.Cash,
                                                 Settlement.Method.ParYieldCurve);
                            if (System.Math.Abs(swaption1.NPV() - swaption2.NPV()) > 1.0e-6)
                            {
                                QAssert.Fail("wrong spread treatment:" +
                                             "\nexercise: " + exerciseDate +
                                             "\nlength:   " + lengths[j] +
                                             "\ntype      " + type[k] +
                                             "\nspread:   " + spreads[l] +
                                             "\noriginal swaption value:   " + swaption1.NPV() +
                                             "\nequivalent swaption value: " + swaption2.NPV());
                            }

                            if (System.Math.Abs(swaption1_cash.NPV() - swaption2_cash.NPV()) > 1.0e-6)
                            {
                                QAssert.Fail("wrong spread treatment:" +
                                             "\nexercise date: " + exerciseDate +
                                             "\nlength: " + lengths[j] +
                                             //"\npay " + (ExerciseType[k] ? "fixed" : "floating") +
                                             "\nspread: " + spreads[l] +
                                             "\nvalue of original swaption:   " + swaption1_cash.NPV() +
                                             "\nvalue of equivalent swaption: " + swaption2_cash.NPV());
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testCachedValue()
        {
            // Testing swaption value against cached value
            var vars = new CommonVars();

            vars.today = new Date(13, 3, 2002);
            vars.settlement = new Date(15, 3, 2002);
            Settings.setEvaluationDate(vars.today);
            vars.termStructure.linkTo(Utilities.flatRate(vars.settlement, 0.05, new Actual365Fixed()));
            var exerciseDate = vars.calendar.advance(vars.settlement, new Period(5, TimeUnit.Years));
            var startDate = vars.calendar.advance(exerciseDate,
                                                   vars.settlementDays, TimeUnit.Days);
            VanillaSwap swap =
               new MakeVanillaSwap(new Period(10, TimeUnit.Years), vars.index, 0.06)
            .withEffectiveDate(startDate);

            var swaption =
               vars.makeSwaption(swap, exerciseDate, 0.20);
            //#if QL_USE_INDEXED_COUPON
            var cachedNPV = 0.036418158579;
            //#else
            //    double cachedNPV = 0.036421429684;
            //#endif

            // FLOATING_POINT_EXCEPTION
            if (System.Math.Abs(swaption.NPV() - cachedNPV) > 1.0e-12)
            {
                QAssert.Fail("failed to reproduce cached swaption value:\n" +
                             //QL_FIXED + std::setprecision(12) +
                             "\ncalculated: " + swaption.NPV() +
                             "\nexpected:   " + cachedNPV);
            }
        }

        [Fact]
        public void testVega()
        {
            // Testing swaption vega
            var vars = new CommonVars();

            Settlement.Type[] types = { Settlement.Type.Physical, Settlement.Type.Cash };
            Settlement.Method[] methods = { Settlement.Method.PhysicalOTC, Settlement.Method.ParYieldCurve };
            double[] strikes = { 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.20, 0.30, 0.70, 0.90 };
            var shift = 1e-8;
            for (var i = 0; i < exercises.Length; i++)
            {
                var exerciseDate = vars.calendar.advance(vars.today, exercises[i]);
                // A VERIFIER§§§§
                var startDate = vars.calendar.advance(exerciseDate,
                                                       vars.settlementDays, TimeUnit.Days);
                for (var j = 0; j < lengths.Length; j++)
                {
                    for (var t = 0; t < strikes.Length; t++)
                    {
                        for (var h = 0; h < type.Length; h++)
                        {
                            VanillaSwap swap =
                               new MakeVanillaSwap(lengths[j], vars.index, strikes[t])
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(0.0)
                            .withType(type[h]);
                            for (var u = 0; u < vols.Length; u++)
                            {
                                var swaption =
                                   vars.makeSwaption(swap, exerciseDate,
                                                     vols[u], types[h], methods[h]);
                                // FLOATING_POINT_EXCEPTION
                                var swaption1 =
                                   vars.makeSwaption(swap, exerciseDate,
                                                     vols[u] - shift, types[h], methods[h]);
                                var swaption2 =
                                   vars.makeSwaption(swap, exerciseDate,
                                                     vols[u] + shift, types[h], methods[h]);

                                var swaptionNPV = swaption.NPV();
                                var numericalVegaPerPoint =
                                   (swaption2.NPV() - swaption1.NPV()) / (200.0 * shift);
                                // check only relevant vega
                                if (numericalVegaPerPoint / swaptionNPV > 1.0e-7)
                                {
                                    var analyticalVegaPerPoint =
                                       (double)swaption.result("vega") / 100.0;
                                    var discrepancy = System.Math.Abs(analyticalVegaPerPoint
                                                                      - numericalVegaPerPoint);
                                    discrepancy /= numericalVegaPerPoint;
                                    var tolerance = 0.015;
                                    if (discrepancy > tolerance)
                                    {
                                        QAssert.Fail("failed to compute swaption vega:" +
                                                     "\n  option tenor:    " + exercises[i] +
                                                     "\n  volatility:      " + vols[u] +
                                                     "\n  option ExerciseType:     " + swaption.type() +
                                                     "\n  swap tenor:      " + lengths[j] +
                                                     "\n  strike:          " + strikes[t] +
                                                     "\n  settlement:      " + types[h] +
                                                     "\n  nominal:         " + swaption.underlyingSwap().nominal +
                                                     "\n  npv:             " + swaptionNPV +
                                                     "\n  calculated vega: " + analyticalVegaPerPoint +
                                                     "\n  expected vega:   " + numericalVegaPerPoint +
                                                     "\n  discrepancy:     " + discrepancy +
                                                     "\n  tolerance:       " + tolerance);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testCashSettledSwaptions()
        {

            var vars = new CommonVars();

            var strike = 0.05;

            for (var i = 0; i < exercises.Length; i++)
            {
                for (var j = 0; j < lengths.Length; j++)
                {

                    var exerciseDate = vars.calendar.advance(vars.today, exercises[i]);
                    var startDate = vars.calendar.advance(exerciseDate,
                                                           vars.settlementDays, TimeUnit.Days);
                    var maturity =
                       vars.calendar.advance(startDate, lengths[j],
                                             vars.floatingConvention);
                    var floatSchedule = new Schedule(startDate, maturity, vars.floatingTenor,
                                                          vars.calendar, vars.floatingConvention,
                                                          vars.floatingConvention,
                                                          DateGeneration.Rule.Forward, false);
                    // Swap with fixed leg conventions: Business Days = Unadjusted, DayCount = 30/360
                    var fixedSchedule_u = new Schedule(startDate, maturity,
                                                            new Period(vars.fixedFrequency),
                                                            vars.calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                            DateGeneration.Rule.Forward, true);
                    var swap_u360 =
                       new VanillaSwap(type[0], vars.nominal,
                                       fixedSchedule_u, strike, new Thirty360(),
                                       floatSchedule, vars.index, 0.0,
                                       vars.index.dayCounter());

                    // Swap with fixed leg conventions: Business Days = Unadjusted, DayCount = Act/365
                    var swap_u365 =
                       new VanillaSwap(type[0], vars.nominal,
                                       fixedSchedule_u, strike, new Actual365Fixed(),
                                       floatSchedule, vars.index, 0.0,
                                       vars.index.dayCounter());

                    // Swap with fixed leg conventions: Business Days = Modified Following, DayCount = 30/360
                    var fixedSchedule_a = new Schedule(startDate, maturity,
                                                            new Period(vars.fixedFrequency),
                                                            vars.calendar, BusinessDayConvention.ModifiedFollowing,
                                                            BusinessDayConvention.ModifiedFollowing,
                                                            DateGeneration.Rule.Forward, true);
                    var swap_a360 =
                       new VanillaSwap(type[0], vars.nominal,
                                       fixedSchedule_a, strike, new Thirty360(),
                                       floatSchedule, vars.index, 0.0,
                                       vars.index.dayCounter());

                    // Swap with fixed leg conventions: Business Days = Modified Following, DayCount = Act/365
                    var swap_a365 =
                       new VanillaSwap(type[0], vars.nominal,
                                       fixedSchedule_a, strike, new Actual365Fixed(),
                                       floatSchedule, vars.index, 0.0,
                                       vars.index.dayCounter());

                    IPricingEngine swapEngine =
                       new DiscountingSwapEngine(vars.termStructure);

                    swap_u360.setPricingEngine(swapEngine);
                    swap_a360.setPricingEngine(swapEngine);
                    swap_u365.setPricingEngine(swapEngine);
                    swap_a365.setPricingEngine(swapEngine);

                    var swapFixedLeg_u360 = swap_u360.fixedLeg();
                    var swapFixedLeg_a360 = swap_a360.fixedLeg();
                    var swapFixedLeg_u365 = swap_u365.fixedLeg();
                    var swapFixedLeg_a365 = swap_a365.fixedLeg();

                    // FlatForward curves
                    // FLOATING_POINT_EXCEPTION
                    var termStructure_u360 = new Handle<YieldTermStructure>(
                       new FlatForward(vars.settlement, swap_u360.fairRate(),
                                       new Thirty360(), Compounding.Compounded,
                                       vars.fixedFrequency));
                    var termStructure_a360 = new Handle<YieldTermStructure>(
                       new FlatForward(vars.settlement, swap_a360.fairRate(),
                                       new Thirty360(), Compounding.Compounded,
                                       vars.fixedFrequency));
                    var termStructure_u365 = new Handle<YieldTermStructure>(
                       new FlatForward(vars.settlement, swap_u365.fairRate(),
                                       new Actual365Fixed(), Compounding.Compounded,
                                       vars.fixedFrequency));
                    var termStructure_a365 = new Handle<YieldTermStructure>(
                       new FlatForward(vars.settlement, swap_a365.fairRate(),
                                       new Actual365Fixed(), Compounding.Compounded,
                                       vars.fixedFrequency));

                    // Annuity calculated by swap method fixedLegBPS().
                    // Fixed leg conventions: Unadjusted, 30/360
                    var annuity_u360 = swap_u360.fixedLegBPS() / 0.0001;
                    annuity_u360 = swap_u360.swapType == VanillaSwap.Type.Payer ?
                                   -annuity_u360 : annuity_u360;
                    // Fixed leg conventions: ModifiedFollowing, act/365
                    var annuity_a365 = swap_a365.fixedLegBPS() / 0.0001;
                    annuity_a365 = swap_a365.swapType == VanillaSwap.Type.Payer ?
                                   -annuity_a365 : annuity_a365;
                    // Fixed leg conventions: ModifiedFollowing, 30/360
                    var annuity_a360 = swap_a360.fixedLegBPS() / 0.0001;
                    annuity_a360 = swap_a360.swapType == VanillaSwap.Type.Payer ?
                                   -annuity_a360 : annuity_a360;
                    // Fixed leg conventions: Unadjusted, act/365
                    var annuity_u365 = swap_u365.fixedLegBPS() / 0.0001;
                    annuity_u365 = swap_u365.swapType == VanillaSwap.Type.Payer ?
                                   -annuity_u365 : annuity_u365;

                    // Calculation of Modified Annuity (cash settlement)
                    // Fixed leg conventions of swap: unadjusted, 30/360
                    var cashannuity_u360 = 0.0;
                    int k;
                    for (k = 0; k < swapFixedLeg_u360.Count; k++)
                    {
                        cashannuity_u360 += swapFixedLeg_u360[k].amount() / strike
                                            * termStructure_u360.currentLink().discount(
                                               swapFixedLeg_u360[k].date());
                    }
                    // Fixed leg conventions of swap: unadjusted, act/365
                    var cashannuity_u365 = 0.0;
                    for (k = 0; k < swapFixedLeg_u365.Count; k++)
                    {
                        cashannuity_u365 += swapFixedLeg_u365[k].amount() / strike
                                            * termStructure_u365.currentLink().discount(
                                               swapFixedLeg_u365[k].date());
                    }
                    // Fixed leg conventions of swap: modified following, 30/360
                    var cashannuity_a360 = 0.0;
                    for (k = 0; k < swapFixedLeg_a360.Count; k++)
                    {
                        cashannuity_a360 += swapFixedLeg_a360[k].amount() / strike
                                            * termStructure_a360.currentLink().discount(
                                               swapFixedLeg_a360[k].date());
                    }
                    // Fixed leg conventions of swap: modified following, act/365
                    var cashannuity_a365 = 0.0;
                    for (k = 0; k < swapFixedLeg_a365.Count; k++)
                    {
                        cashannuity_a365 += swapFixedLeg_a365[k].amount() / strike
                                            * termStructure_a365.currentLink().discount(
                                               swapFixedLeg_a365[k].date());
                    }

                    // Swaptions: underlying swap fixed leg conventions:
                    // unadjusted, 30/360

                    // Physical settled swaption
                    var swaption_p_u360 =
                       vars.makeSwaption(swap_u360, exerciseDate, 0.20);
                    var value_p_u360 = swaption_p_u360.NPV();
                    // Cash settled swaption
                    var swaption_c_u360 =
                       vars.makeSwaption(swap_u360, exerciseDate, 0.20,
                                         Settlement.Type.Cash,
                                         Settlement.Method.ParYieldCurve);
                    var value_c_u360 = swaption_c_u360.NPV();
                    // the NPV's ratio must be equal to annuities ratio
                    var npv_ratio_u360 = value_c_u360 / value_p_u360;
                    var annuity_ratio_u360 = cashannuity_u360 / annuity_u360;

                    // Swaptions: underlying swap fixed leg conventions:
                    // modified following, act/365

                    // Physical settled swaption
                    var swaption_p_a365 =
                       vars.makeSwaption(swap_a365, exerciseDate, 0.20);
                    var value_p_a365 = swaption_p_a365.NPV();
                    // Cash settled swaption
                    var swaption_c_a365 =
                       vars.makeSwaption(swap_a365, exerciseDate, 0.20,
                                         Settlement.Type.Cash,
                                         Settlement.Method.ParYieldCurve);
                    var value_c_a365 = swaption_c_a365.NPV();
                    // the NPV's ratio must be equal to annuities ratio
                    var npv_ratio_a365 = value_c_a365 / value_p_a365;
                    var annuity_ratio_a365 = cashannuity_a365 / annuity_a365;

                    // Swaptions: underlying swap fixed leg conventions:
                    // modified following, 30/360

                    // Physical settled swaption
                    var swaption_p_a360 =
                       vars.makeSwaption(swap_a360, exerciseDate, 0.20);
                    var value_p_a360 = swaption_p_a360.NPV();
                    // Cash settled swaption
                    var swaption_c_a360 =
                       vars.makeSwaption(swap_a360, exerciseDate, 0.20,
                                         Settlement.Type.Cash,
                                         Settlement.Method.ParYieldCurve);
                    var value_c_a360 = swaption_c_a360.NPV();
                    // the NPV's ratio must be equal to annuities ratio
                    var npv_ratio_a360 = value_c_a360 / value_p_a360;
                    var annuity_ratio_a360 = cashannuity_a360 / annuity_a360;

                    // Swaptions: underlying swap fixed leg conventions:
                    // unadjusted, act/365

                    // Physical settled swaption
                    var swaption_p_u365 =
                       vars.makeSwaption(swap_u365, exerciseDate, 0.20);
                    var value_p_u365 = swaption_p_u365.NPV();
                    // Cash settled swaption
                    var swaption_c_u365 =
                       vars.makeSwaption(swap_u365, exerciseDate, 0.20,
                                         Settlement.Type.Cash,
                                         Settlement.Method.ParYieldCurve);
                    var value_c_u365 = swaption_c_u365.NPV();
                    // the NPV's ratio must be equal to annuities ratio
                    var npv_ratio_u365 = value_c_u365 / value_p_u365;
                    var annuity_ratio_u365 = cashannuity_u365 / annuity_u365;

                    if (System.Math.Abs(annuity_ratio_u360 - npv_ratio_u360) > 1e-10)
                    {
                        QAssert.Fail("\n" +
                                     "    The npv's ratio must be equal to " +
                                     " annuities ratio" + "\n" +
                                     "    Swaption " +
                                     exercises[i].units() + "y x " + lengths[j].units() + "y" +
                                     " (underlying swap fixed leg Unadjusted, 30/360)" + "\n" +
                                     "    Today           : " +
                                     vars.today + "\n" +
                                     "    Settlement date : " +
                                     vars.settlement + "\n" +
                                     "    Exercise date   : " +
                                     exerciseDate + "\n" +
                                     "    Swap start date : " +
                                     startDate + "\n" +
                                     "    Swap end date   : " +
                                     maturity + "\n" +
                                     "    physical delivered swaption npv : " +
                                     value_p_u360 + "\t\t\t" +
                                     "    annuity : " +
                                     annuity_u360 + "\n" +
                                     "    cash delivered swaption npv :     " +
                                     value_c_u360 + "\t\t\t" +
                                     "    annuity : " +
                                     cashannuity_u360 + "\n" +
                                     "    npv ratio : " +
                                     npv_ratio_u360 + "\n" +
                                     "    annuity ratio : " +
                                     annuity_ratio_u360 + "\n" +
                                     "    difference : " +
                                     (annuity_ratio_u360 - npv_ratio_u360));
                    }
                    if (System.Math.Abs(annuity_ratio_a365 - npv_ratio_a365) > 1e-10)
                    {
                        QAssert.Fail("\n" +
                                     "    The npv's ratio must be equal to " +
                                     " annuities ratio" + "\n" +
                                     "    Swaption " +
                                     exercises[i].units() + "y x " + lengths[j].units() + "y" +
                                     " (underlying swap fixed leg Modified Following, act/365" + "\n" +
                                     "    Today           : " +
                                     vars.today + "\n" +
                                     "    Settlement date : " +
                                     vars.settlement + "\n" +
                                     "    Exercise date   : " +
                                     exerciseDate + "\n" +
                                     "    Swap start date : " +
                                     startDate + "\n" +
                                     "    Swap end date   : " +
                                     maturity + "\n" +
                                     "    physical delivered swaption npv : " +
                                     value_p_a365 + "\t\t\t" +
                                     "    annuity : " +
                                     annuity_a365 + "\n" +
                                     "    cash delivered swaption npv :     " +
                                     value_c_a365 + "\t\t\t" +
                                     "    annuity : " +
                                     cashannuity_a365 + "\n" +
                                     "    npv ratio : " +
                                     npv_ratio_a365 + "\n" +
                                     "    annuity ratio : " +
                                     annuity_ratio_a365 + "\n" +
                                     "    difference : " +
                                     (annuity_ratio_a365 - npv_ratio_a365));
                    }
                    if (System.Math.Abs(annuity_ratio_a360 - npv_ratio_a360) > 1e-10)
                    {
                        QAssert.Fail("\n" +
                                     "    The npv's ratio must be equal to " +
                                     " annuities ratio" + "\n" +
                                     "    Swaption " +
                                     exercises[i].units() + "y x " + lengths[j].units() + "y" +
                                     " (underlying swap fixed leg Unadjusted, 30/360)" + "\n" +
                                     "    Today           : " +
                                     vars.today + "\n" +
                                     "    Settlement date : " +
                                     vars.settlement + "\n" +
                                     "    Exercise date   : " +
                                     exerciseDate + "\n" +
                                     "    Swap start date : " +
                                     startDate + "\n" +
                                     "    Swap end date   : " +
                                     maturity + "\n" +
                                     "    physical delivered swaption npv : " +
                                     value_p_a360 + "\t\t\t" +
                                     "    annuity : " +
                                     annuity_a360 + "\n" +
                                     "    cash delivered swaption npv :     " +
                                     value_c_a360 + "\t\t\t" +
                                     "    annuity : " +
                                     cashannuity_a360 + "\n" +
                                     "    npv ratio : " +
                                     npv_ratio_a360 + "\n" +
                                     "    annuity ratio : " +
                                     annuity_ratio_a360 + "\n" +
                                     "    difference : " +
                                     (annuity_ratio_a360 - npv_ratio_a360));
                    }
                    if (System.Math.Abs(annuity_ratio_u365 - npv_ratio_u365) > 1e-10)
                    {
                        QAssert.Fail("\n" +
                                     "    The npv's ratio must be equal to " +
                                     " annuities ratio" + "\n" +
                                     "    Swaption " +
                                     exercises[i].units() + "y x " + lengths[j].units() + "y" +
                                     " (underlying swap fixed leg Unadjusted, act/365)" + "\n" +
                                     "    Today           : " +
                                     vars.today + "\n" +
                                     "    Settlement date : " +
                                     vars.settlement + "\n" +
                                     "    Exercise date   : " +
                                     exerciseDate + "\n" +
                                     "    Swap start date : " +
                                     startDate + "\n" +
                                     "    Swap end date   : " +
                                     maturity + "\n" +
                                     "    physical delivered swaption npv : " +
                                     value_p_u365 + "\t\t\t" +
                                     "    annuity : " +
                                     annuity_u365 + "\n" +
                                     "    cash delivered swaption npv :     " +
                                     value_c_u365 + "\t\t\t" +
                                     "    annuity : " +
                                     cashannuity_u365 + "\n" +
                                     "    npv ratio : " +
                                     npv_ratio_u365 + "\n" +
                                     "    annuity ratio : " +
                                     annuity_ratio_u365 + "\n" +
                                     "    difference : " +
                                     (annuity_ratio_u365 - npv_ratio_u365));
                    }
                }
            }
        }

        [Fact]
        public void testImpliedVolatility()
        {
            // Testing implied volatility for swaptions
            var vars = new CommonVars();

            var maxEvaluations = 100;
            var tolerance = 1.0e-08;

            Settlement.Type[] types = { Settlement.Type.Physical, Settlement.Type.Cash };
            Settlement.Method[] methods = { Settlement.Method.PhysicalOTC, Settlement.Method.ParYieldCurve };
            // test data
            double[] strikes = { 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.05, 0.10, 0.20, 0.30, 0.70, 0.90 };

            for (var i = 0; i < exercises.Length; i++)
            {
                for (var j = 0; j < lengths.Length; j++)
                {
                    var exerciseDate = vars.calendar.advance(vars.today, exercises[i]);
                    var startDate = vars.calendar.advance(exerciseDate,
                                                           vars.settlementDays, TimeUnit.Days);
                    var maturity = vars.calendar.advance(startDate, lengths[j],
                                                          vars.floatingConvention);
                    for (var t = 0; t < strikes.Length; t++)
                    {
                        for (var k = 0; k < type.Length; k++)
                        {
                            VanillaSwap swap = new MakeVanillaSwap(lengths[j], vars.index, strikes[t])
                            .withEffectiveDate(startDate)
                            .withFloatingLegSpread(0.0)
                            .withType(type[k]);
                            for (var h = 0; h < types.Length; h++)
                            {
                                for (var u = 0; u < vols.Length; u++)
                                {
                                    var swaption = vars.makeSwaption(swap, exerciseDate,
                                                                          vols[u], types[h], methods[h],
                                                                          BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel.DiscountCurve);
                                    // Black price
                                    var value = swaption.NPV();
                                    var implVol = 0.0;
                                    try
                                    {
                                        implVol =
                                           swaption.impliedVolatility(value,
                                                                      vars.termStructure,
                                                                      0.10,
                                                                      tolerance,
                                                                      maxEvaluations);
                                    }
                                    catch (Exception e)
                                    {
                                        // couldn't bracket?
                                        swaption.setPricingEngine(vars.makeEngine(0.0, BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel.DiscountCurve));
                                        var value2 = swaption.NPV();
                                        if (System.Math.Abs(value - value2) < tolerance)
                                        {
                                            // ok, just skip:
                                            continue;
                                        }
                                        // otherwise, report error
                                        QAssert.Fail("implied vol failure: " +
                                                     exercises[i] + "x" + lengths[j] + " " + type[k] +
                                                     "\nsettlement: " + types[h] +
                                                     "\nstrike      " + strikes[t] +
                                                     "\natm level:  " + swap.fairRate() +
                                                     "\nvol:        " + vols[u] +
                                                     "\nprice:      " + value +
                                                     "\n" + e.Message.ToString());
                                    }
                                    if (System.Math.Abs(implVol - vols[u]) > tolerance)
                                    {
                                        // the difference might not matter
                                        swaption.setPricingEngine(vars.makeEngine(implVol, BlackStyleSwaptionEngine<Black76Spec>.CashAnnuityModel.DiscountCurve));
                                        var value2 = swaption.NPV();
                                        if (System.Math.Abs(value - value2) > tolerance)
                                        {
                                            QAssert.Fail("implied vol failure: " +
                                                         exercises[i] + "x" + lengths[j] + " " + type[k] +
                                                         "\nsettlement:    " + types[h] +
                                                         "\nstrike         " + strikes[t] +
                                                         "\natm level:     " + swap.fairRate() +
                                                         "\nvol:           " + vols[u] +
                                                         "\nprice:         " + value +
                                                         "\nimplied vol:   " + implVol +
                                                         "\nimplied price: " + value2);
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
}
