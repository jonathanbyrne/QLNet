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
using System.Collections.Generic;
using Xunit;
using QLNet.Models;
using QLNet.Time;
using QLNet.Models.Equity;
using QLNet.Math.Optimization;
using QLNet.Instruments;
using QLNet.PricingEngines.vanilla;
using QLNet.Math.Interpolations;
using QLNet.Math.RandomNumbers;
using QLNet.Termstructures;
using QLNet.Math.statistics;
using QLNet.Processes;
using QLNet.Quotes;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_HestonModel
    {
        struct CalibrationMarketData
        {
            public Handle<Quote> s0;
            public Handle<YieldTermStructure> riskFreeTS, dividendYield;
            public List<CalibrationHelper> options;
            public CalibrationMarketData(Handle<Quote> _s0, Handle<YieldTermStructure> _riskFreeTS,
                                         Handle<YieldTermStructure> _dividendYield, List<CalibrationHelper> _options)
            {
                s0 = _s0;
                riskFreeTS = _riskFreeTS;
                dividendYield = _dividendYield;
                options = _options;
            }
        }

        CalibrationMarketData getDAXCalibrationMarketData()
        {
            /* this example is taken from A. Sepp
               Pricing European-Style Options under Jump Diffusion Processes
               with Stochstic Volatility: Applications of Fourier Transform
               http://math.ut.ee/~spartak/papers/stochjumpvols.pdf
            */

            var settlementDate = Settings.evaluationDate();

            DayCounter dayCounter = new Actual365Fixed();
            Calendar calendar = new TARGET();

            int[] t = { 13, 41, 75, 165, 256, 345, 524, 703 };
            double[] r = { 0.0357, 0.0349, 0.0341, 0.0355, 0.0359, 0.0368, 0.0386, 0.0401 };

            var dates = new List<Date>();
            var rates = new List<double>();
            dates.Add(settlementDate);
            rates.Add(0.0357);
            int i;
            for (i = 0; i < 8; ++i)
            {
                dates.Add(settlementDate + t[i]);
                rates.Add(r[i]);
            }
            // FLOATING_POINT_EXCEPTION
            var riskFreeTS = new Handle<YieldTermStructure>(
               new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter));


            var dividendYield = new Handle<YieldTermStructure>(Utilities.flatRate(settlementDate, 0.0, dayCounter));

            double[] v =
            {
            0.6625, 0.4875, 0.4204, 0.3667, 0.3431, 0.3267, 0.3121, 0.3121,
            0.6007, 0.4543, 0.3967, 0.3511, 0.3279, 0.3154, 0.2984, 0.2921,
            0.5084, 0.4221, 0.3718, 0.3327, 0.3155, 0.3027, 0.2919, 0.2889,
            0.4541, 0.3869, 0.3492, 0.3149, 0.2963, 0.2926, 0.2819, 0.2800,
            0.4060, 0.3607, 0.3330, 0.2999, 0.2887, 0.2811, 0.2751, 0.2775,
            0.3726, 0.3396, 0.3108, 0.2781, 0.2788, 0.2722, 0.2661, 0.2686,
            0.3550, 0.3277, 0.3012, 0.2781, 0.2781, 0.2661, 0.2661, 0.2681,
            0.3428, 0.3209, 0.2958, 0.2740, 0.2688, 0.2627, 0.2580, 0.2620,
            0.3302, 0.3062, 0.2799, 0.2631, 0.2573, 0.2533, 0.2504, 0.2544,
            0.3343, 0.2959, 0.2705, 0.2540, 0.2504, 0.2464, 0.2448, 0.2462,
            0.3460, 0.2845, 0.2624, 0.2463, 0.2425, 0.2385, 0.2373, 0.2422,
            0.3857, 0.2860, 0.2578, 0.2399, 0.2357, 0.2327, 0.2312, 0.2351,
            0.3976, 0.2860, 0.2607, 0.2356, 0.2297, 0.2268, 0.2241, 0.2320
         };

            var s0 = new Handle<Quote>(new SimpleQuote(4468.17));
            double[] strike = { 3400, 3600, 3800, 4000, 4200, 4400,
                             4500, 4600, 4800, 5000, 5200, 5400, 5600
                           };

            var options = new List<CalibrationHelper>();

            for (var s = 0; s < 13; ++s)
            {
                for (var m = 0; m < 8; ++m)
                {
                    var vol = new Handle<Quote>(new SimpleQuote(v[s * 8 + m]));

                    var maturity = new Period((int)((t[m] + 3) / 7.0), TimeUnit.Weeks); // round to weeks
                    options.Add(new HestonModelHelper(maturity, calendar, s0, strike[s], vol, riskFreeTS, dividendYield,
                                                      CalibrationHelper.CalibrationErrorType.ImpliedVolError));
                }
            }

            var marketData = new CalibrationMarketData(s0, riskFreeTS, dividendYield, options);

            return marketData;
        }

        [Fact]
        public void testBlackCalibration()
        {
            // Testing Heston model calibration using a flat volatility surface

            using (var backup = new SavedSettings())
            {
                /* calibrate a Heston model to a constant volatility surface without
                   smile. expected result is a vanishing volatility of the volatility.
                   In addition theta and v0 should be equal to the constant variance */

                var today = Date.Today;
                Settings.setEvaluationDate(today);

                DayCounter dayCounter = new Actual360();
                Calendar calendar = new NullCalendar();

                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.04, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.50, dayCounter));

                var optionMaturities = new List<Period>();
                optionMaturities.Add(new Period(1, TimeUnit.Months));
                optionMaturities.Add(new Period(2, TimeUnit.Months));
                optionMaturities.Add(new Period(3, TimeUnit.Months));
                optionMaturities.Add(new Period(6, TimeUnit.Months));
                optionMaturities.Add(new Period(9, TimeUnit.Months));
                optionMaturities.Add(new Period(1, TimeUnit.Years));
                optionMaturities.Add(new Period(2, TimeUnit.Years));

                var options = new List<CalibrationHelper>();
                var s0 = new Handle<Quote>(new SimpleQuote(1.0));
                var vol = new Handle<Quote>(new SimpleQuote(0.1));
                var volatility = vol.link.value();

                for (var i = 0; i < optionMaturities.Count; ++i)
                {
                    for (var moneyness = -1.0; moneyness < 2.0; moneyness += 1.0)
                    {
                        // FLOATING_POINT_EXCEPTION
                        var tau = dayCounter.yearFraction(riskFreeTS.link.referenceDate(),
                                                             calendar.advance(riskFreeTS.link.referenceDate(),
                                                                              optionMaturities[i]));
                        var fwdPrice = s0.link.value() * dividendTS.link.discount(tau)
                                       / riskFreeTS.link.discount(tau);
                        var strikePrice = fwdPrice * System.Math.Exp(-moneyness * volatility * System.Math.Sqrt(tau));

                        options.Add(new HestonModelHelper(optionMaturities[i], calendar, s0, strikePrice, vol,
                                                          riskFreeTS, dividendTS));
                    }
                }

                for (var sigma = 0.1; sigma < 0.7; sigma += 0.2)
                {
                    var v0 = 0.01;
                    var kappa = 0.2;
                    var theta = 0.02;
                    var rho = -0.75;

                    var process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);

                    var model = new HestonModel(process);
                    IPricingEngine engine = new AnalyticHestonEngine(model, 96);

                    for (var i = 0; i < options.Count; ++i)
                        options[i].setPricingEngine(engine);

                    var om = new LevenbergMarquardt(1e-8, 1e-8, 1e-8);
                    model.calibrate(options, om, new EndCriteria(400, 40, 1.0e-8, 1.0e-8, 1.0e-8));

                    var tolerance = 3.0e-3;

                    if (model.sigma() > tolerance)
                    {
                        QAssert.Fail("Failed to reproduce expected sigma"
                                     + "\n    calculated: " + model.sigma()
                                     + "\n    expected:   " + 0.0
                                     + "\n    tolerance:  " + tolerance);
                    }

                    if (System.Math.Abs(model.kappa() * (model.theta() - volatility * volatility)) > tolerance)
                    {
                        QAssert.Fail("Failed to reproduce expected theta"
                                     + "\n    calculated: " + model.theta()
                                     + "\n    expected:   " + volatility * volatility);
                    }

                    if (System.Math.Abs(model.v0() - volatility * volatility) > tolerance)
                    {
                        QAssert.Fail("Failed to reproduce expected v0"
                                     + "\n    calculated: " + model.v0()
                                     + "\n    expected:   " + volatility * volatility);
                    }
                }
            }
        }

        [Fact]
        public void testDAXCalibration()
        {
            // Testing Heston model calibration using DAX volatility data
            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(5, Month.July, 2002);

                Settings.setEvaluationDate(settlementDate);

                var marketData = getDAXCalibrationMarketData();

                var riskFreeTS = marketData.riskFreeTS;
                var dividendTS = marketData.dividendYield;
                var s0 = marketData.s0;

                var options = marketData.options;

                var v0 = 0.1;
                var kappa = 1.0;
                var theta = 0.1;
                var sigma = 0.5;
                var rho = -0.5;

                var process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);

                var model = new HestonModel(process);

                IPricingEngine engine = new AnalyticHestonEngine(model, 64);

                for (var i = 0; i < options.Count; ++i)
                    options[i].setPricingEngine(engine);

                var om = new LevenbergMarquardt(1e-8, 1e-8, 1e-8);
                model.calibrate(options, om, new EndCriteria(400, 40, 1.0e-8, 1.0e-8, 1.0e-8));

                double sse = 0;
                for (var i = 0; i < 13 * 8; ++i)
                {
                    var diff = options[i].calibrationError() * 100.0;
                    sse += diff * diff;
                }
                var expected = 177.2; //see article by A. Sepp.
                if (System.Math.Abs(sse - expected) > 1.0)
                {
                    QAssert.Fail("Failed to reproduce calibration error"
                                 + "\n    calculated: " + sse
                                 + "\n    expected:   " + expected);
                }
            }
        }

        //[TestMethod()]
        //public void testAnalyticVsBlack()
        //{
        // Testing analytic Heston engine against Black formula

        //using (SavedSettings backup = new SavedSettings())
        //{

        //Date settlementDate = Date.Today;
        //Settings.setEvaluationDate(settlementDate);
        //DayCounter dayCounter = new ActualActual();
        //Date exerciseDate = settlementDate + new Period(6,TimeUnit.Months);

        //StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, 30);
        //Exercise exercise = new EuropeanExercise(exerciseDate);

        //Handle<YieldTermStructure> riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.1, dayCounter));
        //Handle<YieldTermStructure> dividendTS  = new Handle<YieldTermStructure>(Utilities.flatRate(0.04, dayCounter));

        //Handle<Quote> s0  = new Handle<Quote>(new SimpleQuote(32.0));

        //double v0 = 0.05;
        //double kappa = 5.0;
        //double theta = 0.05;
        //double sigma = 1.0e-4;
        //double rho = 0.0;

        //HestonProcess process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);

        //VanillaOption option = new VanillaOption(payoff, exercise);
        //// FLOATING_POINT_EXCEPTION
        //IPricingEngine engine = new AnalyticHestonEngine(new HestonModel(process), 144);

        //option.setPricingEngine(engine);
        //double calculated = option.NPV();

        //double yearFraction = dayCounter.yearFraction(settlementDate, exerciseDate);
        //double forwardPrice = 32*System.Math.Exp((0.1 - 0.04)*yearFraction);
        //double expected = QLNet.Utils.blackFormula(payoff.optionType(), payoff.strike(),
        //   forwardPrice, System.Math.Sqrt(0.05*yearFraction))*
        //                System.Math.Exp(-0.1*yearFraction);
        //double error = System.Math.Abs(calculated - expected);
        //double tolerance = 2.0e-7;
        //if (error > tolerance)
        //{
        //   QAssert.Fail("failed to reproduce Black price with AnalyticHestonEngine"
        //              + "\n    calculated: " + calculated
        //              + "\n    expected:   " + expected
        //              + "\n    error:      " + error);
        //}

        //engine = new FdHestonVanillaEngine(new HestonModel(process),200, 200, 100);
        //option.setPricingEngine(engine);

        //calculated = option.NPV();
        //error = System.Math.Abs(calculated - expected);
        //tolerance = 1.0e-3;
        //if (error > tolerance)
        //{
        //   QAssert.Fail("failed to reproduce Black price with FdHestonVanillaEngine"
        //              +"\n    calculated: " + calculated
        //              +"\n    expected:   " + expected
        //              +"\n    error:      " + error);
        //}
        //}
        //}

        [Fact]
        public void testAnalyticVsCached()
        {
            // Testing analytic Heston engine against cached values

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(27, Month.December, 2004);
                Settings.setEvaluationDate(settlementDate);
                DayCounter dayCounter = new ActualActual();
                var exerciseDate = new Date(28, Month.March, 2005);

                StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 1.05);
                Exercise exercise = new EuropeanExercise(exerciseDate);

                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0225, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dayCounter));

                var s0 = new Handle<Quote>(new SimpleQuote(1.0));
                var v0 = 0.1;
                var kappa = 3.16;
                var theta = 0.09;
                var sigma = 0.4;
                var rho = -0.2;

                var process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);

                var option = new VanillaOption(payoff, exercise);

                var engine = new AnalyticHestonEngine(new HestonModel(process), 64);

                option.setPricingEngine(engine);

                var expected1 = 0.0404774515;
                var calculated1 = option.NPV();
                var tolerance = 1.0e-8;

                if (System.Math.Abs(calculated1 - expected1) > tolerance)
                {
                    QAssert.Fail("Failed to reproduce cached analytic price"
                                 + "\n    calculated: " + calculated1
                                 + "\n    expected:   " + expected1);
                }

                // reference values from www.wilmott.com, technical forum
                // search for "Heston or VG price check"

                double[] K = { 0.9, 1.0, 1.1 };
                double[] expected2 = { 0.1330371, 0.0641016, 0.0270645 };
                var calculated2 = new double[6];

                int i;
                for (i = 0; i < 6; ++i)
                {
                    var exerciseDate2 = new Date(8 + i / 3, Month.September, 2005);

                    StrikedTypePayoff payoff2 = new PlainVanillaPayoff(QLNet.Option.Type.Call, K[i % 3]);
                    Exercise exercise2 = new EuropeanExercise(exerciseDate2);

                    var riskFreeTS2 = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, dayCounter));
                    var dividendTS2 = new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dayCounter));

                    var s = riskFreeTS2.link.discount(0.7) / dividendTS2.link.discount(0.7);
                    var s02 = new Handle<Quote>(new SimpleQuote(s));

                    var process2 = new HestonProcess(riskFreeTS2, dividendTS2, s02, 0.09, 1.2, 0.08, 1.8, -0.45);

                    var option2 = new VanillaOption(payoff2, exercise2);

                    IPricingEngine engine2 = new AnalyticHestonEngine(new HestonModel(process2));

                    option2.setPricingEngine(engine2);
                    calculated2[i] = option2.NPV();
                }

                // we are after the value for T=0.7
                var t1 = dayCounter.yearFraction(settlementDate, new Date(8, Month.September, 2005));
                var t2 = dayCounter.yearFraction(settlementDate, new Date(9, Month.September, 2005));

                for (i = 0; i < 3; ++i)
                {
                    var interpolated = calculated2[i] + (calculated2[i + 3] - calculated2[i]) / (t2 - t1) * (0.7 - t1);

                    if (System.Math.Abs(interpolated - expected2[i]) > 100 * tolerance)
                    {
                        QAssert.Fail("Failed to reproduce cached analytic prices:"
                                     + "\n    calculated: " + interpolated
                                     + "\n    expected:   " + expected2[i]);
                    }
                }
            }
        }

        [Fact]
        public void testMcVsCached()
        {
            // Testing Monte Carlo Heston engine against cached values
            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(27, Month.December, 2004);
                Settings.setEvaluationDate(settlementDate);

                DayCounter dayCounter = new ActualActual();
                var exerciseDate = new Date(28, Month.March, 2005);

                StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, 1.05);
                Exercise exercise = new EuropeanExercise(exerciseDate);

                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.7, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.4, dayCounter));

                var s0 = new Handle<Quote>(new SimpleQuote(1.05));

                var process = new HestonProcess(riskFreeTS, dividendTS, s0, 0.3, 1.16, 0.2, 0.8, 0.8);

                var option = new VanillaOption(payoff, exercise);

                var engine = new MakeMCEuropeanHestonEngine<PseudoRandom, Statistics>(process)
                .withStepsPerYear(11)
                .withAntitheticVariate()
                .withSamples(50000)
                .withSeed(1234).getAsPricingEngine();

                option.setPricingEngine(engine);

                var expected = 0.0632851308977151;
                var calculated = option.NPV();
                var errorEstimate = option.errorEstimate();
                var tolerance = 7.5e-4;

                if (System.Math.Abs(calculated - expected) > 2.34 * errorEstimate)
                {
                    QAssert.Fail("Failed to reproduce cached price"
                                 + "\n    calculated: " + calculated
                                 + "\n    expected:   " + expected
                                 + " +/- " + errorEstimate);
                }

                if (errorEstimate > tolerance)
                {
                    QAssert.Fail("failed to reproduce error estimate"
                                 + "\n    calculated: " + errorEstimate
                                 + "\n    expected:   " + tolerance);
                }
            }
        }

        //[TestMethod()]
        //public void testFdBarrierVsCached()
        //{
        //  // Testing FD barrier Heston engine against cached values

        //   using (SavedSettings backup = new SavedSettings())
        // {

        //   DayCounter dc = new Actual360();
        //   Date today = Date.Today;

        //   Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100.0));
        //   Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.08, dc));
        //   Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.04, dc));

        //   Date exDate = today + (int)(0.5*360 + 0.5);
        //   Exercise exercise  = new EuropeanExercise(exDate);

        //   StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 90.0);

        //   HestonProcess process = new HestonProcess(rTS, qTS, s0, 0.25*0.25, 1.0, 0.25*0.25, 0.001, 0.0);

        //   IPricingEngine engine = new FdHestonBarrierEngine(new HestonModel(process),200, 400, 100);

        //   BarrierOption option = new BarrierOption(Barrier.Type.DownOut,95.0,3.0,payoff,exercise);
        //   option.setPricingEngine(engine);

        //   double calculated = option.NPV();
        //   double expected = 9.0246;
        //   double error = System.Math.Abs(calculated - expected);
        //   if (error > 1.0e-3)
        //   {
        //      QAssert.Fail("failed to reproduce cached price with FD Barrier engine"
        //                 + "\n    calculated: " + calculated
        //                 + "\n    expected:   " + expected
        //                 + "\n    error:      " +  error);
        //   }

        //   option = new BarrierOption(Barrier.Type.DownIn, 95.0, 3.0, payoff, exercise);
        //   option.setPricingEngine(engine);

        //   calculated = option.NPV();
        //   expected = 7.7627;
        //   error = System.Math.Abs(calculated - expected);
        //   if (error > 1.0e-3)
        //   {
        //      QAssert.Fail("failed to reproduce cached price with FD Barrier engine"
        //                 + "\n    calculated: " + calculated
        //                 + "\n    expected:   " + expected
        //                 + "\n    error:      " + error);
        //   }
        //}
        //}

        //[TestMethod()]
        //public void testFdVanillaVsCached()
        //{
        //   // Testing FD vanilla Heston engine against cached values

        //   using (SavedSettings backup = new SavedSettings())
        //{

        //   Date settlementDate = new Date(27,Month.December,2004);
        //   Settings.setEvaluationDate(settlementDate);

        //   DayCounter dayCounter = new ActualActual();
        //   Date exerciseDate = new Date(28,Month.March,2005);

        //   StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, 1.05);
        //   Exercise exercise = new EuropeanExercise(exerciseDate);

        //   Handle<YieldTermStructure> riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.7, dayCounter));
        //   Handle<YieldTermStructure> dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.4, dayCounter));

        //   Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(1.05));

        //   VanillaOption option = new VanillaOption(payoff, exercise);

        //   HestonProcess process = new HestonProcess(riskFreeTS, dividendTS, s0, 0.3, 1.16, 0.2, 0.8, 0.8);

        //   IPricingEngine engine = new FdHestonVanillaEngine(new HestonModel(process),100, 200, 100);
        //   option.setPricingEngine(engine);

        //   double expected = 0.06325;
        //   double calculated = option.NPV();
        //   double error = System.Math.Abs(calculated - expected);
        //   double tolerance = 1.0e-4;

        //   if (error > tolerance)
        //   {
        //      QAssert.Fail("failed to reproduce cached price with FD engine"
        //                 + "\n    calculated: " + calculated
        //                 + "\n    expected:   " + expected
        //                 + "\n    error:      " +  error);
        //   }

        //   // Testing FD vanilla Heston engine for discrete dividends

        //   payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 95.0);
        //   s0 = new Handle<Quote>(new SimpleQuote(100.0));

        //   riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, dayCounter));
        //   dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, dayCounter));

        //   exerciseDate = new Date(28, Month.March, 2006);
        //   exercise = new EuropeanExercise(exerciseDate);

        //   List<Date> dividendDates = new List<Date>();
        //   List<double> dividends = new List<double>();
        //   for (Date d = settlementDate + new Period(3,TimeUnit.Months);
        //      d < exercise.lastDate();
        //      d += new Period(6,TimeUnit.Months))
        //   {
        //      dividendDates.Add(d);
        //      dividends.Add(1.0);
        //   }

        //   DividendVanillaOption divOption = new DividendVanillaOption(payoff, exercise,dividendDates, dividends);
        //   process = new HestonProcess(riskFreeTS, dividendTS, s0, 0.04, 1.0, 0.04, 0.001, 0.0);
        //   engine = new FdHestonVanillaEngine(new HestonModel(process),200, 400, 100);
        //   divOption.setPricingEngine(engine);
        //   calculated = divOption.NPV();
        //   // Value calculated with an independent FD framework, validated with
        //   // an independent MC framework
        //   expected = 12.946;
        //   error = System.Math.Abs(calculated - expected);
        //   tolerance = 5.0e-3;

        //   if (error > tolerance)
        //   {
        //      QAssert.Fail("failed to reproduce discrete dividend price with FD engine"
        //                 + "\n    calculated: " + calculated
        //                 + "\n    expected:   " + expected
        //                 + "\n    error:      " +  error);
        //   }

        //   // Testing FD vanilla Heston engine for american exercise

        //   dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.03, dayCounter));
        //   process = new HestonProcess(riskFreeTS, dividendTS, s0, 0.04, 1.0, 0.04, 0.001, 0.0);
        //   engine = new FdHestonVanillaEngine(new HestonModel(process),200, 400, 100);
        //   payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, 95.0);
        //   exercise = new AmericanExercise(settlementDate, exerciseDate);
        //   option = new VanillaOption(payoff, exercise);
        //   option.setPricingEngine(engine);
        //   calculated = option.NPV();

        //   Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(settlementDate, 0.2,
        //      dayCounter));
        //   BlackScholesMertonProcess ref_process = new BlackScholesMertonProcess(s0, dividendTS, riskFreeTS, volTS);
        //   IPricingEngine ref_engine = new FDAmericanEngine<CrankNicolson>(ref_process, 200, 400);
        //   option.setPricingEngine(ref_engine);
        //   expected = option.NPV();

        //   error = System.Math.Abs(calculated - expected);
        //   tolerance = 1.0e-3;

        //   if (error > tolerance)
        //   {
        //      QAssert.Fail("failed to reproduce american option price with FD engine"
        //                 + "\n    calculated: " + calculated
        //                 + "\n    expected:   " + expected
        //                 + "\n    error:      " + error);
        //   }
        //}
        //}

        // [TestMethod()]
        //public void testKahlJaeckelCase()
        //{
        //// Testing MC and FD Heston engines for the Kahl-Jaeckel example

        ///* Example taken from Wilmott mag (Sept. 2005).
        //   "Not-so-complex logarithms in the Heston model",
        //   Example was also discussed within the Wilmott thread
        //   "QuantLib code is very high quatlity"
        //*/

        //using (SavedSettings backup = new SavedSettings())
        //{

        //Date settlementDate = new Date(30,Month.March,2007);
        //Settings.setEvaluationDate(settlementDate);

        //DayCounter dayCounter = new ActualActual();
        //Date exerciseDate = new Date(30,Month.March,2017);

        //StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 200);
        //Exercise exercise = new EuropeanExercise(exerciseDate);

        //VanillaOption option = new VanillaOption(payoff, exercise);

        //Handle<YieldTermStructure> riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, dayCounter));
        //Handle<YieldTermStructure> dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, dayCounter));

        //Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(100));

        //double v0 = 0.16;
        //double theta = v0;
        //double kappa = 1.0;
        //double sigma = 2.0;
        //double rho = -0.8;


        ////HestonProcessDiscretizationDesc[] descriptions =
        ////{
        ////   new HestonProcessDiscretizationDesc(HestonProcess::NonCentralChiSquareVariance,10,"NonCentralChiSquareVariance"),
        ////   new HestonProcessDiscretizationDesc{HestonProcess::QuadraticExponentialMartingale,100,"QuadraticExponentialMartingale")
        ////}

        //double tolerance = 0.1;
        //double expected = 4.95212;

        //for (int i = 0; i < descriptions.Lenght; ++i)
        //{
        //   HestonProcess process = new HestonProcess(riskFreeTS, dividendTS, s0, v0,kappa, theta, sigma, rho,
        //         descriptions[i].discretization);

        //   IPricingEngine engine = new MakeMCEuropeanHestonEngine<PseudoRandom>(process)
        //         .withSteps(descriptions[i].nSteps)
        //         .withAntitheticVariate()
        //         .withAbsoluteTolerance(tolerance)
        //         .withSeed(1234);
        //   option.setPricingEngine(engine);

        //   double calculated = option.NPV();
        //   double errorEstimate = option.errorEstimate();

        //   if (System.Math.Abs(calculated - expected) > 2.34*errorEstimate)
        //   {
        //      QAssert.Fail("Failed to reproduce cached price with MC engine"
        //                  + "\n    discretization: " + descriptions[i].name
        //                  + "\n    expected:       " + expected
        //                  + "\n    calculated:     " + calculated
        //                  + " +/- " + errorEstimate);
        //   }

        //   if (errorEstimate > tolerance)
        //   {
        //      QAssert.Fail("failed to reproduce error estimate with MC engine"
        //                  + "\n    discretization: " + descriptions[i].name
        //                  + "\n    calculated    : " + errorEstimate
        //                  + "\n    expected      :   " + tolerance);
        //   }
        //}

        //option.setPricingEngine( new MakeMCEuropeanHestonEngine<LowDiscrepancy>(
        //      new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho,
        //         HestonProcess.Discretization.BroadieKayaExactSchemeLaguerre)))
        //      .withSteps(1)
        //      .withSamples(1023));

        //double calculated = option.NPV();
        //if (System.Math.Abs(calculated - expected) > tolerance)
        //{
        //   QAssert.Fail("Failed to reproduce cached price with MC engine"
        //               + "\n    discretization: BroadieKayaExactSchemeLobatto"
        //               + "\n    calculated:     " + calculated
        //               + "\n    expected:       " + expected
        //               + "\n    tolerance:      " + tolerance);
        //}

        //option.setPricingEngine( new FdHestonVanillaEngine(new HestonModel(
        //   new HestonProcess(riskFreeTS, dividendTS, s0, v0,kappa, theta, sigma, rho))),200, 400, 100);

        //calculated = option.NPV();
        //double error = System.Math.Abs(calculated - expected);
        //if (error > 5.0e-2)
        //{
        //   QAssert.Fail("failed to reproduce cached price with FD engine"
        //              + "\n    calculated: " + calculated
        //              + "\n    expected:   " + expected
        //              + "\n    error:      " + error);
        //}
        //}
        //}

        struct HestonParameter
        {
            public double v0, kappa, theta, sigma, rho;
            public HestonParameter(double _v0, double _kappa, double _theta, double _sigma, double _rho)
            {
                v0 = _v0;
                kappa = _kappa;
                theta = _theta;
                sigma = _sigma;
                rho = _rho;
            }
        }

        [Fact]
        public void testDifferentIntegrals()
        {
            // Testing different numerical Heston integration algorithms

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(27, Month.December, 2004);
                Settings.setEvaluationDate(settlementDate);

                DayCounter dayCounter = new ActualActual();

                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.05, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.03, dayCounter));

                double[] strikes = { 0.5, 0.7, 1.0, 1.25, 1.5, 2.0 };
                int[] maturities = { 1, 2, 3, 12, 60, 120, 360 };
                Option.Type[] types = { QLNet.Option.Type.Put, QLNet.Option.Type.Call };

                var equityfx = new HestonParameter(0.07, 2.0, 0.04, 0.55, -0.8);
                var highCorr = new HestonParameter(0.07, 1.0, 0.04, 0.55, 0.995);
                var lowVolOfVol = new HestonParameter(0.07, 1.0, 0.04, 0.025, -0.75);
                var highVolOfVol = new HestonParameter(0.07, 1.0, 0.04, 5.0, -0.75);
                var kappaEqSigRho = new HestonParameter(0.07, 0.4, 0.04, 0.5, 0.8);

                var parameters = new List<HestonParameter>();
                parameters.Add(equityfx);
                parameters.Add(highCorr);
                parameters.Add(lowVolOfVol);
                parameters.Add(highVolOfVol);
                parameters.Add(kappaEqSigRho);

                double[] tol = { 1e-3, 1e-3, 0.2, 0.01, 1e-3 };
                var count = 0;
                foreach (var iter in parameters)
                {
                    var s0 = new Handle<Quote>(new SimpleQuote(1.0));
                    var process = new HestonProcess(riskFreeTS, dividendTS, s0, iter.v0, iter.kappa,
                                                              iter.theta, iter.sigma, iter.rho);

                    var model = new HestonModel(process);

                    var lobattoEngine = new AnalyticHestonEngine(model, 1e-10, 1000000);
                    var laguerreEngine = new AnalyticHestonEngine(model, 128);
                    var legendreEngine = new AnalyticHestonEngine(model,
                                                                                   AnalyticHestonEngine.ComplexLogFormula.Gatheral, AnalyticHestonEngine.Integration.gaussLegendre(512));
                    var chebyshevEngine = new AnalyticHestonEngine(model,
                                                                                    AnalyticHestonEngine.ComplexLogFormula.Gatheral, AnalyticHestonEngine.Integration.gaussChebyshev(512));
                    var chebyshev2ndEngine = new AnalyticHestonEngine(model,
                                                                                       AnalyticHestonEngine.ComplexLogFormula.Gatheral, AnalyticHestonEngine.Integration.gaussChebyshev2nd(512));

                    var maxLegendreDiff = 0.0;
                    var maxChebyshevDiff = 0.0;
                    var maxChebyshev2ndDiff = 0.0;
                    var maxLaguerreDiff = 0.0;

                    for (var i = 0; i < maturities.Length; ++i)
                    {
                        Exercise exercise = new EuropeanExercise(settlementDate + new Period(maturities[i], TimeUnit.Months));
                        for (var j = 0; j < strikes.Length; ++j)
                        {
                            for (var k = 0; k < types.Length; ++k)
                            {
                                StrikedTypePayoff payoff = new PlainVanillaPayoff(types[k], strikes[j]);

                                var option = new VanillaOption(payoff, exercise);

                                option.setPricingEngine(lobattoEngine);
                                var lobattoNPV = option.NPV();

                                option.setPricingEngine(laguerreEngine);
                                var laguerre = option.NPV();

                                option.setPricingEngine(legendreEngine);
                                var legendre = option.NPV();

                                option.setPricingEngine(chebyshevEngine);
                                var chebyshev = option.NPV();

                                option.setPricingEngine(chebyshev2ndEngine);
                                var chebyshev2nd = option.NPV();

                                maxLaguerreDiff = System.Math.Max(maxLaguerreDiff, System.Math.Abs(lobattoNPV - laguerre));
                                maxLegendreDiff = System.Math.Max(maxLegendreDiff, System.Math.Abs(lobattoNPV - legendre));
                                maxChebyshevDiff = System.Math.Max(maxChebyshevDiff, System.Math.Abs(lobattoNPV - chebyshev));
                                maxChebyshev2ndDiff = System.Math.Max(maxChebyshev2ndDiff, System.Math.Abs(lobattoNPV - chebyshev2nd));
                            }
                        }
                    }
                    var maxDiff = System.Math.Max(System.Math.Max(System.Math.Max(maxLaguerreDiff, maxLegendreDiff), maxChebyshevDiff),
                                              maxChebyshev2ndDiff);

                    var tr = tol[count++];
                    if (maxDiff > tr)
                    {
                        QAssert.Fail("Failed to reproduce Heston pricing values within given tolerance"
                                     + "\n    maxDifference: " + maxDiff
                                     + "\n    tolerance:     " + tr);
                    }
                }
            }
        }

        // [TestMethod()]
        //public void testMultipleStrikesEngine()
        //{
        //// Testing multiple-strikes FD Heston engine...");

        //using (SavedSettings backup = new SavedSettings())
        //{

        //Date settlementDate = new Date(27,Month.December,2004);
        //Settings.setEvaluationDate(settlementDate);

        //DayCounter dayCounter = new ActualActual();
        //Date exerciseDate = new Date(28,Month.March,2006);

        //Exercise exercise  = new EuropeanExercise(exerciseDate);

        //Handle<YieldTermStructure> riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.06, dayCounter));
        //Handle<YieldTermStructure> dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dayCounter));

        //Handle<Quote> s0 = new Handle<Quote>(new SimpleQuote(1.05));

        //HestonProcess process = new HestonProcess(riskFreeTS, dividendTS, s0, 0.16, 2.5, 0.09, 0.8, -0.8);
        //HestonModel model = new HestonModel(process);

        //List<double> strikes = new List<double>();
        //strikes.Add(1.0);
        //strikes.Add(0.5);
        //strikes.Add(0.75);
        //strikes.Add(1.5);
        //strikes.Add(2.0);

        //FdHestonVanillaEngine singleStrikeEngine = new FdHestonVanillaEngine(model, 20, 400, 50);
        //FdHestonVanillaEngine multiStrikeEngine = new FdHestonVanillaEngine(model, 20, 400, 50);
        //multiStrikeEngine.enableMultipleStrikesCaching(strikes);

        //double relTol = 5e-3;
        //for (int i = 0; i < strikes.Count; ++i)
        //{
        //   StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, strikes[i]);

        //   VanillaOption aOption = new VanillaOption(payoff, exercise);
        //   aOption.setPricingEngine(multiStrikeEngine);

        //   double npvCalculated = aOption.NPV();
        //   double deltaCalculated = aOption.delta();
        //   double gammaCalculated = aOption.gamma();
        //   double thetaCalculated = aOption.theta();

        //   aOption.setPricingEngine(singleStrikeEngine);
        //   double npvExpected = aOption.NPV();
        //   double deltaExpected = aOption.delta();
        //   double gammaExpected = aOption.gamma();
        //   double thetaExpected = aOption.theta();

        //   if (System.Math.Abs(npvCalculated - npvExpected)/npvExpected > relTol)
        //   {
        //      QAssert.Fail("failed to reproduce price with FD multi strike engine"
        //                 + "\n    calculated: " + npvCalculated
        //                 + "\n    expected:   " + npvExpected
        //                 + "\n    error:      " + relTol);
        //   }
        //   if (System.Math.Abs(deltaCalculated - deltaExpected)/deltaExpected > relTol)
        //   {
        //      QAssert.Fail("failed to reproduce delta with FD multi strike engine"
        //                 + "\n    calculated: " + deltaCalculated
        //                 + "\n    expected:   " + deltaExpected
        //                 + "\n    error:      " + relTol);
        //   }
        //   if (System.Math.Abs(gammaCalculated - gammaExpected)/gammaExpected > relTol)
        //   {
        //      QAssert.Fail("failed to reproduce gamma with FD multi strike engine"
        //                 + "\n    calculated: " + gammaCalculated
        //                 + "\n    expected:   " + gammaExpected
        //                 + "\n    error:      " + relTol);
        //   }
        //   if (System.Math.Abs(thetaCalculated - thetaExpected)/thetaExpected > relTol)
        //   {
        //      QAssert.Fail( "failed to reproduce theta with FD multi strike engine"
        //                 + "\n    calculated: " + thetaCalculated
        //                 + "\n    expected:   " + thetaExpected
        //                 + "\n    error:      " +  relTol);
        //   }
        // }
        //}
        //}

        [Fact]
        public void testAnalyticPiecewiseTimeDependent()
        {
            // Testing analytic piecewise time dependent Heston prices

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(27, Month.December, 2004);
                Settings.setEvaluationDate(settlementDate);
                DayCounter dayCounter = new ActualActual();
                var exerciseDate = new Date(28, Month.March, 2005);

                StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 1.0);
                Exercise exercise = new EuropeanExercise(exerciseDate);

                var dates = new List<Date>();
                dates.Add(settlementDate);
                dates.Add(new Date(01, Month.January, 2007));
                var irates = new List<double>();
                irates.Add(0.0);
                irates.Add(0.2);
                var riskFreeTS = new Handle<YieldTermStructure>(
                   new InterpolatedZeroCurve<Linear>(dates, irates, dayCounter));

                var qrates = new List<double>();
                qrates.Add(0.0);
                qrates.Add(0.3);
                var dividendTS = new Handle<YieldTermStructure>(
                   new InterpolatedZeroCurve<Linear>(dates, qrates, dayCounter));


                var v0 = 0.1;
                var s0 = new Handle<Quote>(new SimpleQuote(1.0));

                var theta = new ConstantParameter(0.09, new PositiveConstraint());
                var kappa = new ConstantParameter(3.16, new PositiveConstraint());
                var sigma = new ConstantParameter(4.40, new PositiveConstraint());
                var rho = new ConstantParameter(-0.8, new BoundaryConstraint(-1.0, 1.0));

                var model = new PiecewiseTimeDependentHestonModel(riskFreeTS, dividendTS,
                                                                                                s0, v0, theta, kappa, sigma, rho, new TimeGrid(20.0, 2));

                var option = new VanillaOption(payoff, exercise);
                option.setPricingEngine(new AnalyticPTDHestonEngine(model));

                var calculated = option.NPV();
                var hestonProcess = new HestonProcess(riskFreeTS, dividendTS, s0, v0,
                                                                kappa.value(0.0), theta.value(0.0), sigma.value(0.0), rho.value(0.0));
                var hestonModel = new HestonModel(hestonProcess);
                option.setPricingEngine(new AnalyticHestonEngine(hestonModel));

                var expected = option.NPV();

                if (System.Math.Abs(calculated - expected) > 1e-12)
                {
                    QAssert.Fail("failed to reproduce heston prices "
                                 + "\n    calculated: " + calculated
                                 + "\n    expected:   " + expected);
                }
            }
        }

        [Fact]
        public void testDAXCalibrationOfTimeDependentModel()
        {
            // Testing time-dependent Heston model calibration

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(5, Month.July, 2002);
                Settings.setEvaluationDate(settlementDate);

                var marketData = getDAXCalibrationMarketData();

                var riskFreeTS = marketData.riskFreeTS;
                var dividendTS = marketData.dividendYield;
                var s0 = marketData.s0;

                var options = marketData.options;

                var modelTimes = new List<double>();
                modelTimes.Add(0.25);
                modelTimes.Add(10.0);
                var modelGrid = new TimeGrid(modelTimes, modelTimes.Count);

                var v0 = 0.1;
                var sigma = new ConstantParameter(0.5, new PositiveConstraint());
                var theta = new ConstantParameter(0.1, new PositiveConstraint());
                var rho = new ConstantParameter(-0.5, new BoundaryConstraint(-1.0, 1.0));

                List<double> pTimes = new InitializedList<double>(1, 0.25);
                var kappa = new PiecewiseConstantParameter(pTimes, new PositiveConstraint());

                for (var i = 0; i < pTimes.Count + 1; ++i)
                {
                    kappa.setParam(i, 10.0);
                }

                var model = new PiecewiseTimeDependentHestonModel(riskFreeTS, dividendTS,
                                                                                                s0, v0, theta, kappa, sigma, rho, modelGrid);

                IPricingEngine engine = new AnalyticPTDHestonEngine(model);
                for (var i = 0; i < options.Count; ++i)
                    options[i].setPricingEngine(engine);

                var om = new LevenbergMarquardt(1e-8, 1e-8, 1e-8);
                model.calibrate(options, om, new EndCriteria(400, 40, 1.0e-8, 1.0e-8, 1.0e-8));

                double sse = 0;
                for (var i = 0; i < 13 * 8; ++i)
                {
                    var diff = options[i].calibrationError() * 100.0;
                    sse += diff * diff;
                }

                var expected = 74.4;
                if (System.Math.Abs(sse - expected) > 1.0)
                {
                    QAssert.Fail("Failed to reproduce calibration error"
                                 + "\n    calculated: " + sse
                                 + "\n    expected:   " + expected);
                }
            }
        }

        [Fact]
        public void testAlanLewisReferencePrices()
        {
            // Testing Alan Lewis reference prices

            /*
               * testing Alan Lewis reference prices posted in
               * http://wilmott.com/messageview.cfm?catid=34&threadid=90957
            */

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(5, Month.July, 2002);
                Settings.setEvaluationDate(settlementDate);

                var maturityDate = new Date(5, Month.July, 2003);
                Exercise exercise = new EuropeanExercise(maturityDate);

                DayCounter dayCounter = new Actual365Fixed();
                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.01, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dayCounter));

                var s0 = new Handle<Quote>(new SimpleQuote(100.0));

                var v0 = 0.04;
                var rho = -0.5;
                var sigma = 1.0;
                var kappa = 4.0;
                var theta = 0.25;

                var process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);
                var model = new HestonModel(process);

                IPricingEngine laguerreEngine = new AnalyticHestonEngine(model, 128);

                IPricingEngine gaussLobattoEngine = new AnalyticHestonEngine(model, Const.QL_EPSILON, 100000);

                double[] strikes = { 80, 90, 100, 110, 120 };
                Option.Type[] types = { QLNet.Option.Type.Put, QLNet.Option.Type.Call };
                IPricingEngine[] engines = { laguerreEngine, gaussLobattoEngine };

                double[][] expectedResults =
                {
               new double[2]{
                  7.958878113256768285213263077598987193482161301733,
                  26.774758743998854221382195325726949201687074848341
               },
               new double[2]{
                  12.017966707346304987709573290236471654992071308187,
                  20.933349000596710388139445766564068085476194042256
               },
               new double[2]{
                  17.055270961270109413522653999411000974895436309183,
                  16.070154917028834278213466703938231827658768230714
               },
               new double[2]{
                  23.017825898442800538908781834822560777763225722188,
                  12.132211516709844867860534767549426052805766831181
               },
               new double[2]{
                  29.811026202682471843340682293165857439167301370697,
                  9.024913483457835636553375454092357136489051667150
               }
            };

                var tol = 1e-12; // 3e-15 works on linux/ia32,
                                    // but keep some buffer for other platforms

                for (var i = 0; i < strikes.Length; ++i)
                {
                    var strike = strikes[i];

                    for (var j = 0; j < types.Length; ++j)
                    {
                        var type = types[j];

                        for (var k = 0; k < engines.Length; ++k)
                        {
                            var engine = engines[k];

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

                            var option = new VanillaOption(payoff, exercise);
                            option.setPricingEngine(engine);

                            var expected = expectedResults[i][j];
                            var calculated = option.NPV();
                            var relError = System.Math.Abs(calculated - expected) / expected;

                            if (relError > tol)
                            {
                                QAssert.Fail("failed to reproduce Alan Lewis Reference prices "
                                             + "\n    strike     : " + strike
                                             + "\n    option ExerciseType: " + type
                                             + "\n    engine ExerciseType: " + k
                                             + "\n    rel. error : " + relError);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testExpansionOnAlanLewisReference()
        {
            // Testing expansion on Alan Lewis reference prices

            using (var backup = new SavedSettings())
            {
                var settlementDate = new Date(5, Month.July, 2002);
                Settings.setEvaluationDate(settlementDate);

                var maturityDate = new Date(5, Month.July, 2003);
                Exercise exercise = new EuropeanExercise(maturityDate);

                DayCounter dayCounter = new Actual365Fixed();
                var riskFreeTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.01, dayCounter));
                var dividendTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.02, dayCounter));

                var s0 = new Handle<Quote>(new SimpleQuote(100.0));

                var v0 = 0.04;
                var rho = -0.5;
                var sigma = 1.0;
                var kappa = 4.0;
                var theta = 0.25;

                var process = new HestonProcess(riskFreeTS, dividendTS, s0, v0, kappa, theta, sigma, rho);
                var model = new HestonModel(process);

                IPricingEngine lpp2Engine = new HestonExpansionEngine(model, HestonExpansionEngine.HestonExpansionFormula.LPP2);
                //don't test Forde as it does not behave well on this example
                IPricingEngine lpp3Engine = new HestonExpansionEngine(model, HestonExpansionEngine.HestonExpansionFormula.LPP3);

                double[] strikes = { 80, 90, 100, 110, 120 };
                Option.Type[] types = { QLNet.Option.Type.Put, QLNet.Option.Type.Call };
                IPricingEngine[] engines = { lpp2Engine, lpp3Engine };

                double[][] expectedResults =
                {
               new double[2] {
                  7.958878113256768285213263077598987193482161301733,
                  26.774758743998854221382195325726949201687074848341
               },
               new double[2] {
                  12.017966707346304987709573290236471654992071308187,
                  20.933349000596710388139445766564068085476194042256
               },
               new double[2] {
                  17.055270961270109413522653999411000974895436309183,
                  16.070154917028834278213466703938231827658768230714
               },
               new double[2] {
                  23.017825898442800538908781834822560777763225722188,
                  12.132211516709844867860534767549426052805766831181
               },
               new double[2] {
                  29.811026202682471843340682293165857439167301370697,
                  9.024913483457835636553375454092357136489051667150
               }
            };

                double[] tol = { 1.003e-2, 3.645e-3 };

                for (var i = 0; i < strikes.Length; ++i)
                {
                    var strike = strikes[i];

                    for (var j = 0; j < types.Length; ++j)
                    {
                        var type = types[j];

                        for (var k = 0; k < engines.Length; ++k)
                        {
                            var engine = engines[k];

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

                            var option = new VanillaOption(payoff, exercise);
                            option.setPricingEngine(engine);

                            var expected = expectedResults[i][j];
                            var calculated = option.NPV();
                            var relError = System.Math.Abs(calculated - expected) / expected;

                            if (relError > tol[k])
                            {
                                QAssert.Fail("failed to reproduce Alan Lewis Reference prices "
                                             + "\n    strike     : " + strike
                                             + "\n    option ExerciseType: " + type
                                             + "\n    engine ExerciseType: " + k
                                             + "\n    rel. error : " + relError);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testExpansionOnFordeReference()
        {
            // Testing expansion on Forde reference prices

            using (var backup = new SavedSettings())
            {
                var forward = 100.0;
                var v0 = 0.04;
                var rho = -0.4;
                var sigma = 0.2;
                var kappa = 1.15;
                var theta = 0.04;

                double[] terms = { 0.1, 1.0, 5.0, 10.0 };
                double[] strikes = { 60, 80, 90, 100, 110, 120, 140 };

                double[][] referenceVols =
                {
               new double[7] {
                  0.27284673574924445,
                  0.22360758200372477,
                  0.21023988547031242,
                  0.1990674789471587,
                  0.19118230678920461,
                  0.18721342919371017,
                  0.1899869903378507
               },
               new double[7] {
                  0.25200775151345,
                  0.2127275920953156,
                  0.20286528150874591,
                  0.19479398358151515,
                  0.18872591728967686,
                  0.18470857955411824,
                  0.18204457060905446
               },
               new double[7] {
                  0.21637821506229973,
                  0.20077227130455172,
                  0.19721753043236154,
                  0.1942233023784151,
                  0.191693211401571,
                  0.18955229722896752,
                  0.18491727548069495
               },
               new double[7] {
                  0.20672925973965342,
                  0.198583062164427,
                  0.19668274423922746,
                  0.1950420231354201,
                  0.193610364344706,
                  0.1923502827886502,
                  0.18934360917857015
               }
            };

                double[][] tol =
                {
               new double[4] {
                  0.06,
                  0.03,
                  0.03,
                  0.02
               },
               new double[4] {
                  0.15,
                  0.08,
                  0.04,
                  0.02
               },
               new double[4] {
                  0.06,
                  0.08,
                  1.0,
                  1.0
               } //forde breaks down for long maturities
            };

                double[][] tolAtm =
                {
               new double[4] {
                  4e-6,
                  7e-4,
                  2e-3,
                  9e-4
               },
               new double[4] {
                  7e-6,
                  4e-4,
                  9e-4,
                  4e-4
               },
               new double[4] {
                  4e-4,
                  3e-2,
                  0.28,
                  1.0
               }
            };

                for (var j = 0; j < terms.Length; ++j)
                {
                    var term = terms[j];
                    HestonExpansion lpp2 = new LPP2HestonExpansion(kappa, theta, sigma, v0, rho, term);
                    HestonExpansion lpp3 = new LPP3HestonExpansion(kappa, theta, sigma, v0, rho, term);
                    HestonExpansion forde = new FordeHestonExpansion(kappa, theta, sigma, v0, rho, term);
                    HestonExpansion[] expansions = { lpp2, lpp3, forde };

                    for (var i = 0; i < strikes.Length; ++i)
                    {
                        var strike = strikes[i];
                        for (var k = 0; k < expansions.Length; ++k)
                        {
                            var expansion = expansions[k];

                            var expected = referenceVols[j][i];
                            var calculated = expansion.impliedVolatility(strike, forward);
                            var relError = System.Math.Abs(calculated - expected) / expected;
                            var refTol = strike == forward ? tolAtm[k][j] : tol[k][j];
                            if (relError > refTol)
                            {
                                QAssert.Fail("failed to reproduce Forde reference vols "
                                             + "\n    strike        : " + strike
                                             + "\n    expansion ExerciseType: " + k
                                             + "\n    rel. error    : " + relError);
                            }
                        }
                    }
                }
            }
        }
    }
}
