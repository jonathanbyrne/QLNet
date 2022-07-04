//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using System.Linq;
using Xunit;
using QLNet.Math.randomnumbers;
using QLNet.Methods.montecarlo;
using QLNet.Instruments;
using QLNet.Math.Distributions;
using QLNet.processes;
using QLNet.Models.Equity;
using QLNet.Math.statistics;
using QLNet.Time;
using QLNet.Math.Interpolations;
using QLNet.Termstructures;
using QLNet.Math;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Models.Shortrate.Onefactormodels;
using QLNet.Pricingengines.vanilla;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_HybridHestonHullWhiteProcess : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_HybridHestonHullWhiteProcess()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        [Fact]
        public void testBsmHullWhiteEngine()
        {
            // Testing European option pricing for a BSM process with one-factor Hull-White model
            DayCounter dc = new Actual365Fixed();

            var today = Date.Today;
            var maturity = today + new Period(20, TimeUnit.Years);

            Settings.setEvaluationDate(today);

            var spot = new Handle<Quote>(new SimpleQuote(100.0));
            var qRate = new SimpleQuote(0.04);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
            var rRate = new SimpleQuote(0.0525);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
            var vol = new SimpleQuote(0.25);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

            // FLOATING_POINT_EXCEPTION
            var hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), 0.00883, 0.00526);

            var stochProcess = new BlackScholesMertonProcess(spot, qTS, rTS, volTS);

            Exercise exercise = new EuropeanExercise(maturity);

            var fwd = spot.link.value() * qTS.link.discount(maturity) / rTS.link.discount(maturity);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, fwd);

            var option = new EuropeanOption(payoff, exercise);

            var tol = 1e-8;
            double[] corr = { -0.75, -0.25, 0.0, 0.25, 0.75 };
            double[] expectedVol = { 0.217064577, 0.243995801, 0.256402830, 0.268236596, 0.290461343 };

            for (var i = 0; i < corr.Length; ++i)
            {
                IPricingEngine bsmhwEngine = new AnalyticBSMHullWhiteEngine(corr[i], stochProcess, hullWhiteModel);

                option.setPricingEngine(bsmhwEngine);
                var npv = option.NPV();

                var compVolTS = new Handle<BlackVolTermStructure>(
                   Utilities.flatVol(today, expectedVol[i], dc));

                var bsProcess = new BlackScholesMertonProcess(spot, qTS, rTS, compVolTS);
                IPricingEngine bsEngine = new AnalyticEuropeanEngine(bsProcess);

                var comp = new EuropeanOption(payoff, exercise);
                comp.setPricingEngine(bsEngine);

                var impliedVol = comp.impliedVolatility(npv, bsProcess, 1e-10, 100);

                if (System.Math.Abs(impliedVol - expectedVol[i]) > tol)
                {
                    QAssert.Fail("Failed to reproduce implied volatility"
                                 + "\n    calculated: " + impliedVol
                                 + "\n    expected  : " + expectedVol[i]);
                }
                if (System.Math.Abs((comp.NPV() - npv) / npv) > tol)
                {
                    QAssert.Fail("Failed to reproduce NPV"
                                 + "\n    calculated: " + npv
                                 + "\n    expected  : " + comp.NPV());
                }
                if (System.Math.Abs(comp.delta() - option.delta()) > tol)
                {
                    QAssert.Fail("Failed to reproduce NPV"
                                 + "\n    calculated: " + npv
                                 + "\n    expected  : " + comp.NPV());
                }
                if (System.Math.Abs((comp.gamma() - option.gamma()) / npv) > tol)
                {
                    QAssert.Fail("Failed to reproduce NPV"
                                 + "\n    calculated: " + npv
                                 + "\n    expected  : " + comp.NPV());
                }
                if (System.Math.Abs((comp.theta() - option.theta()) / npv) > tol)
                {
                    QAssert.Fail("Failed to reproduce NPV"
                                 + "\n    calculated: " + npv
                                 + "\n    expected  : " + comp.NPV());
                }
                if (System.Math.Abs((comp.vega() - option.vega()) / npv) > tol)
                {
                    QAssert.Fail("Failed to reproduce NPV"
                                 + "\n    calculated: " + npv
                                 + "\n    expected  : " + comp.NPV());
                }
            }
        }

        [Fact]
        public void testCompareBsmHWandHestonHW()
        {
            // Comparing European option pricing for a BSM process with one-factor Hull-White model
            DayCounter dc = new Actual365Fixed();
            var today = Date.Today;
            Settings.setEvaluationDate(today);

            var spot = new Handle<Quote>(new SimpleQuote(100.0));
            var dates = new List<Date>();
            List<double> rates = new List<double>(), divRates = new List<double>();

            for (var i = 0; i <= 40; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Years));
                // FLOATING_POINT_EXCEPTION
                rates.Add(0.01 + 0.0002 * System.Math.Exp(System.Math.Sin(i / 4.0)));
                divRates.Add(0.02 + 0.0001 * System.Math.Exp(System.Math.Sin(i / 5.0)));
            }

            var s0 = new Handle<Quote>(new SimpleQuote(100));
            var rTS = new Handle<YieldTermStructure>(
               new InterpolatedZeroCurve<Linear>(dates, rates, dc));
            var qTS = new Handle<YieldTermStructure>(
               new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

            var vol = new SimpleQuote(0.25);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

            var bsmProcess = new BlackScholesMertonProcess(spot, qTS, rTS, volTS);

            var hestonProcess = new HestonProcess(rTS, qTS, spot,
                                                            vol.value() * vol.value(), 1.0, vol.value() * vol.value(), 1e-4, 0.0);

            var hestonModel = new HestonModel(hestonProcess);

            var hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), 0.01, 0.01);

            IPricingEngine bsmhwEngine = new AnalyticBSMHullWhiteEngine(0.0, bsmProcess, hullWhiteModel);

            IPricingEngine hestonHwEngine = new AnalyticHestonHullWhiteEngine(hestonModel, hullWhiteModel, 128);

            var tol = 1e-5;
            double[] strike = { 0.25, 0.5, 0.75, 0.8, 0.9, 1.0, 1.1, 1.2, 1.5, 2.0, 4.0 };
            int[] maturity = { 1, 2, 3, 5, 10, 15, 20, 25, 30 };
            Option.Type[] types = { QLNet.Option.Type.Put, QLNet.Option.Type.Call };

            for (var i = 0; i < types.Length; ++i)
            {
                for (var j = 0; j < strike.Length; ++j)
                {
                    for (var l = 0; l < maturity.Length; ++l)
                    {
                        var maturityDate = today + new Period(maturity[l], TimeUnit.Years);

                        Exercise exercise = new EuropeanExercise(maturityDate);

                        var fwd = strike[j] * spot.link.value()
                                            * qTS.link.discount(maturityDate) / rTS.link.discount(maturityDate);

                        StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], fwd);

                        var option = new EuropeanOption(payoff, exercise);

                        option.setPricingEngine(bsmhwEngine);
                        var calculated = option.NPV();

                        option.setPricingEngine(hestonHwEngine);
                        var expected = option.NPV();

                        if (System.Math.Abs(calculated - expected) > calculated * tol &&
                            System.Math.Abs(calculated - expected) > tol)
                        {
                            QAssert.Fail("Failed to reproduce npvs"
                                         + "\n    calculated: " + calculated
                                         + "\n    expected  : " + expected
                                         + "\n    strike    : " + strike[j]
                                         + "\n    maturity  : " + maturity[l]
                                         + "\n    ExerciseType      : "
                                         + (types[i] == QLNet.Option.Type.Put ? "Put" : "Call"));
                        }
                    }
                }
            }
        }

        [Fact]
        public void testZeroBondPricing()
        {
            // Testing Monte-Carlo zero bond pricing

            DayCounter dc = new Actual360();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            // construct a strange yield curve to check drifts and discounting
            // of the joint stochastic process

            var dates = new List<Date>();
            var times = new List<double>();
            var rates = new List<double>();

            dates.Add(today);
            rates.Add(0.02);
            times.Add(0.0);
            for (var i = 120; i < 240; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Months));
                rates.Add(0.02 + 0.0002 * System.Math.Exp(System.Math.Sin(i / 8.0)));
                times.Add(dc.yearFraction(today, dates.Last()));
            }

            var maturity = dates.Last() + new Period(10, TimeUnit.Years);
            dates.Add(maturity);
            rates.Add(0.04);
            //times.Add(dc.yearFraction(today, dates.Last()));

            var s0 = new Handle<Quote>(new SimpleQuote(100));

            var ts = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
            var ds = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.0, dc));

            var hestonProcess = new HestonProcess(ts, ds, s0, 0.02, 1.0, 0.2, 0.5, -0.8);
            var hwProcess = new HullWhiteForwardProcess(ts, 0.05, 0.05);
            hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));
            var hwModel = new HullWhite(ts, 0.05, 0.05);

            var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, hwProcess, -0.4);

            var grid = new TimeGrid(times);

            var factors = jointProcess.factors();
            var steps = grid.size() - 1;
            var rsg = new SobolBrownianBridgeRsg(factors, steps);
            var generator = new MultiPathGenerator<SobolBrownianBridgeRsg>(
               jointProcess, grid, rsg, false);

            var m = 90;
            List<GeneralStatistics> zeroStat = new InitializedList<GeneralStatistics>(m);
            List<GeneralStatistics> optionStat = new InitializedList<GeneralStatistics>(m);

            var nrTrails = 8191;
            var optionTenor = 24;
            var strike = 0.5;

            for (var i = 0; i < nrTrails; ++i)
            {
                var path = generator.next();
                var value = path.value as MultiPath;
                Utils.QL_REQUIRE(value != null, () => "Invalid Path");

                for (var j = 1; j < m; ++j)
                {
                    var t = grid[j]; // zero end and option maturity
                    var T = grid[j + optionTenor]; // maturity of zero bond
                                                      // of option

                    var states = new Vector(3);
                    var optionStates = new Vector(3);
                    for (var k = 0; k < jointProcess.size(); ++k)
                    {
                        states[k] = value[k][j];
                        optionStates[k] = value[k][j + optionTenor];
                    }

                    var zeroBond
                       = 1.0 / jointProcess.numeraire(t, states);
                    var zeroOption = zeroBond * System.Math.Max(0.0, hwModel.discountBond(t, T, states[2]) - strike);

                    zeroStat[j].add(zeroBond);
                    optionStat[j].add(zeroOption);
                }
            }

            for (var j = 1; j < m; ++j)
            {
                var t = grid[j];
                var calculated = zeroStat[j].mean();
                var expected = ts.link.discount(t);

                if (System.Math.Abs(calculated - expected) > 0.03)
                {
                    QAssert.Fail("Failed to reproduce expected zero bond prices"
                                 + "\n   t:          " + t
                                 + "\n   calculated: " + calculated
                                 + "\n   expected:   " + expected);
                }

                var T = grid[j + optionTenor];

                calculated = optionStat[j].mean();
                expected = hwModel.discountBondOption(QLNet.Option.Type.Call, strike, t, T);

                if (System.Math.Abs(calculated - expected) > 0.0035)
                {
                    QAssert.Fail("Failed to reproduce expected zero bond option prices"
                                 + "\n   t:          " + t
                                 + "\n   T:          " + T
                                 + "\n   calculated: " + calculated
                                 + "\n   expected:   " + expected);
                }
            }
        }

        [Fact]
        public void testMcVanillaPricing()
        {
            // Testing Monte-Carlo vanilla option pricing
            DayCounter dc = new Actual360();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            // construct a strange yield curve to check drifts and discounting
            // of the joint stochastic process

            var dates = new List<Date>();
            var times = new List<double>();
            List<double> rates = new List<double>(), divRates = new List<double>();

            for (var i = 0; i <= 40; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Years));
                // FLOATING_POINT_EXCEPTION
                rates.Add(0.03 + 0.0003 * System.Math.Exp(System.Math.Sin(i / 4.0)));
                divRates.Add(0.02 + 0.0001 * System.Math.Exp(System.Math.Sin(i / 5.0)));
                times.Add(dc.yearFraction(today, dates.Last()));
            }

            var maturity = today + new Period(20, TimeUnit.Years);

            var s0 = new Handle<Quote>(new SimpleQuote(100));
            var rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates,
                                                                            rates, dc));
            var qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates,
                                                                            divRates, dc));
            var vol = new SimpleQuote(0.25);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

            var bsmProcess = new BlackScholesMertonProcess(s0, qTS, rTS, volTS);
            var hestonProcess = new HestonProcess(rTS, qTS, s0, 0.0625, 0.5, 0.0625, 1e-5, 0.3);
            var hwProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
            hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));

            var tol = 0.05;
            double[] corr = { -0.9, -0.5, 0.0, 0.5, 0.9 };
            double[] strike = { 100 };

            for (var i = 0; i < corr.Length; ++i)
            {
                for (var j = 0; j < strike.Length; ++j)
                {
                    var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess,
                                                                                                 hwProcess, corr[i]);

                    StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, strike[j]);
                    Exercise exercise = new EuropeanExercise(maturity);

                    var optionHestonHW = new VanillaOption(payoff, exercise);
                    var engine = new MakeMCHestonHullWhiteEngine<PseudoRandom, Statistics>(jointProcess)
                    .withSteps(1)
                    .withAntitheticVariate()
                    .withControlVariate()
                    .withAbsoluteTolerance(tol)
                    .withSeed(42).getAsPricingEngine();

                    optionHestonHW.setPricingEngine(engine);

                    var hwModel = new HullWhite(new Handle<YieldTermStructure>(rTS),
                                                      hwProcess.a(), hwProcess.sigma());

                    var optionBsmHW = new VanillaOption(payoff, exercise);
                    optionBsmHW.setPricingEngine(new AnalyticBSMHullWhiteEngine(corr[i], bsmProcess, hwModel));

                    var calculated = optionHestonHW.NPV();
                    var error = optionHestonHW.errorEstimate();
                    var expected = optionBsmHW.NPV();

                    if (corr[i] != 0.0 && System.Math.Abs(calculated - expected) > 3 * error
                        || corr[i] == 0.0 && System.Math.Abs(calculated - expected) > 1e-4)
                    {
                        QAssert.Fail("Failed to reproduce BSM-HW vanilla prices"
                                     + "\n   corr:       " + corr[i]
                                     + "\n   strike:     " + strike[j]
                                     + "\n   calculated: " + calculated
                                     + "\n   error:      " + error
                                     + "\n   expected:   " + expected);
                    }
                }
            }
        }

        [Fact]
        public void testMcPureHestonPricing()
        {
            // Testing Monte-Carlo Heston option pricing
            DayCounter dc = new Actual360();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            // construct a strange yield curve to check drifts and discounting
            // of the joint stochastic process

            var dates = new List<Date>();
            var times = new List<double>();
            List<double> rates = new List<double>(), divRates = new List<double>();

            for (var i = 0; i <= 100; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Months));
                // FLOATING_POINT_EXCEPTION
                rates.Add(0.02 + 0.0002 * System.Math.Exp(System.Math.Sin(i / 10.0)));
                divRates.Add(0.02 + 0.0001 * System.Math.Exp(System.Math.Sin(i / 20.0)));
                times.Add(dc.yearFraction(today, dates.Last()));
            }

            var maturity = today + new Period(2, TimeUnit.Years);

            var s0 = new Handle<Quote>(new SimpleQuote(100));
            var rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
            var qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

            var hestonProcess = new HestonProcess(rTS, qTS, s0, 0.08, 1.5, 0.0625, 0.5, -0.8);
            var hwProcess = new HullWhiteForwardProcess(rTS, 0.1, 1e-8);
            hwProcess.setForwardMeasureTime(dc.yearFraction(today, maturity + new Period(1, TimeUnit.Years)));

            var tol = 0.001;
            double[] corr = { -0.45, 0.45, 0.25 };
            double[] strike = { 100, 75, 50, 150 };

            for (var i = 0; i < corr.Length; ++i)
            {
                for (var j = 0; j < strike.Length; ++j)
                {
                    var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, hwProcess,
                                                                                                 corr[i], HybridHestonHullWhiteProcess.Discretization.Euler);

                    StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, strike[j]);
                    Exercise exercise = new EuropeanExercise(maturity);

                    var optionHestonHW = new VanillaOption(payoff, exercise);
                    var optionPureHeston = new VanillaOption(payoff, exercise);
                    optionPureHeston.setPricingEngine(new AnalyticHestonEngine(new HestonModel(hestonProcess)));

                    var expected = optionPureHeston.NPV();

                    optionHestonHW.setPricingEngine(
                       new MakeMCHestonHullWhiteEngine<PseudoRandom, Statistics>(jointProcess)
                       .withSteps(2)
                       .withAntitheticVariate()
                       .withControlVariate()
                       .withAbsoluteTolerance(tol)
                       .withSeed(42).getAsPricingEngine());

                    var calculated = optionHestonHW.NPV();
                    var error = optionHestonHW.errorEstimate();

                    if (System.Math.Abs(calculated - expected) > 3 * error
                        && System.Math.Abs(calculated - expected) > tol)
                    {
                        QAssert.Fail("Failed to reproduce pure heston vanilla prices"
                                     + "\n   corr:       " + corr[i]
                                     + "\n   strike:     " + strike[j]
                                     + "\n   calculated: " + calculated
                                     + "\n   error:      " + error
                                     + "\n   expected:   " + expected);
                    }
                }
            }
        }

        [Fact]
        public void testAnalyticHestonHullWhitePricing()
        {
            // Testing analytic Heston Hull-White option pricing
            DayCounter dc = new Actual360();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            // construct a strange yield curve to check drifts and discounting
            // of the joint stochastic process

            var dates = new List<Date>();
            var times = new List<double>();
            List<double> rates = new List<double>(), divRates = new List<double>();

            for (var i = 0; i <= 40; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Years));
                // FLOATING_POINT_EXCEPTION
                rates.Add(0.03 + 0.0001 * System.Math.Exp(System.Math.Sin(i / 4.0)));
                divRates.Add(0.02 + 0.0002 * System.Math.Exp(System.Math.Sin(i / 3.0)));
                times.Add(dc.yearFraction(today, dates.Last()));
            }

            var maturity = today + new Period(5, TimeUnit.Years);
            var s0 = new Handle<Quote>(new SimpleQuote(100));
            var rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
            var qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

            var hestonProcess = new HestonProcess(rTS, qTS, s0, 0.08, 1.5, 0.0625, 0.5, -0.8);
            var hestonModel = new HestonModel(hestonProcess);

            var hwFwdProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
            hwFwdProcess.setForwardMeasureTime(dc.yearFraction(today, maturity));
            var hullWhiteModel = new HullWhite(rTS, hwFwdProcess.a(), hwFwdProcess.sigma());

            var tol = 0.002;
            double[] strike = { 80, 120 };
            Option.Type[] types = { QLNet.Option.Type.Put, QLNet.Option.Type.Call };

            for (var i = 0; i < types.Length; ++i)
            {
                for (var j = 0; j < strike.Length; ++j)
                {
                    var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess,
                                                                                                 hwFwdProcess, 0.0, HybridHestonHullWhiteProcess.Discretization.Euler);

                    StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strike[j]);
                    Exercise exercise = new EuropeanExercise(maturity);

                    var optionHestonHW = new VanillaOption(payoff, exercise);
                    optionHestonHW.setPricingEngine(new MakeMCHestonHullWhiteEngine<PseudoRandom, Statistics>(jointProcess)
                                                    .withSteps(1)
                                                    .withAntitheticVariate()
                                                    .withControlVariate()
                                                    .withAbsoluteTolerance(tol)
                                                    .withSeed(42).getAsPricingEngine());

                    var optionPureHeston = new VanillaOption(payoff, exercise);
                    optionPureHeston.setPricingEngine(new AnalyticHestonHullWhiteEngine(hestonModel, hullWhiteModel, 128));

                    var calculated = optionHestonHW.NPV();
                    var error = optionHestonHW.errorEstimate();
                    var expected = optionPureHeston.NPV();

                    if (System.Math.Abs(calculated - expected) > 3 * error
                        && System.Math.Abs(calculated - expected) > tol)
                    {
                        QAssert.Fail("Failed to reproduce hw heston vanilla prices"
                                     + "\n   strike:     " + strike[j]
                                     + "\n   calculated: " + calculated
                                     + "\n   error:      " + error
                                     + "\n   expected:   " + expected);
                    }
                }
            }
        }

        [Fact]
        public void testCallableEquityPricing()
        {
            // Testing the pricing of a callable equity product

            /*
             For the definition of the example product see
             Alexander Giese, On the Pricing of Auto-Callable Equity
             Structures in the Presence of Stochastic Volatility and
             Stochastic Interest Rates .
             http://workshop.mathfinance.de/2006/papers/giese/slides.pdf
            */

            var maturity = 7;
            DayCounter dc = new Actual365Fixed();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            var spot = new Handle<Quote>(new SimpleQuote(100.0));
            var qRate = new SimpleQuote(0.04);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
            var rRate = new SimpleQuote(0.04);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));

            var hestonProcess = new HestonProcess(rTS, qTS, spot, 0.0625, 1.0, 0.24 * 0.24, 1e-4, 0.0);
            // FLOATING_POINT_EXCEPTION
            var hwProcess = new HullWhiteForwardProcess(rTS, 0.00883, 0.00526);
            hwProcess.setForwardMeasureTime(dc.yearFraction(today, today + new Period(maturity + 1, TimeUnit.Years)));

            var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess, hwProcess, -0.4);

            var schedule = new Schedule(today, today + new Period(maturity, TimeUnit.Years), new Period(1, TimeUnit.Years),
                                             new TARGET(), BusinessDayConvention.Following, BusinessDayConvention.Following, DateGeneration.Rule.Forward, false);

            List<double> times = new InitializedList<double>(maturity + 1);

            for (var i = 0; i <= maturity; ++i)
                times[i] = i;

            var grid = new TimeGrid(times, times.Count);

            List<double> redemption = new InitializedList<double>(maturity);
            for (var i = 0; i < maturity; ++i)
            {
                redemption[i] = 1.07 + 0.03 * i;
            }

            ulong seed = 42;
            IRNG rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                        , InverseCumulativeNormal>)
                       new PseudoRandom().make_sequence_generator(jointProcess.factors() * (grid.size() - 1), seed);

            var generator = new MultiPathGenerator<IRNG>(jointProcess, grid, rsg, false);
            var stat = new GeneralStatistics();

            double antitheticPayoff = 0;
            var nrTrails = 40000;
            for (var i = 0; i < nrTrails; ++i)
            {
                var antithetic = i % 2 != 0;

                var path = antithetic ? generator.antithetic() : generator.next();
                var value = path.value as MultiPath;
                Utils.QL_REQUIRE(value != null, () => "Invalid Path");

                double payoff = 0;
                for (var j = 1; j <= maturity; ++j)
                {
                    if (value[0][j] > spot.link.value())
                    {
                        var states = new Vector(3);
                        for (var k = 0; k < 3; ++k)
                        {
                            states[k] = value[k][j];
                        }
                        payoff = redemption[j - 1] / jointProcess.numeraire(grid[j], states);
                        break;
                    }
                    else if (j == maturity)
                    {
                        var states = new Vector(3);
                        for (var k = 0; k < 3; ++k)
                        {
                            states[k] = value[k][j];
                        }
                        payoff = 1.0 / jointProcess.numeraire(grid[j], states);
                    }
                }

                if (antithetic)
                {
                    stat.add(0.5 * (antitheticPayoff + payoff));
                }
                else
                {
                    antitheticPayoff = payoff;
                }
            }

            var expected = 0.938;
            var calculated = stat.mean();
            var error = stat.errorEstimate();

            if (System.Math.Abs(expected - calculated) > 3 * error)
            {
                QAssert.Fail("Failed to reproduce auto-callable equity structure price"
                             + "\n   calculated: " + calculated
                             + "\n   error:      " + error
                             + "\n   expected:   " + expected);
            }
        }

        [Fact]
        public void testDiscretizationError()
        {
            // Testing the discretization error of the Heston Hull-White process
            DayCounter dc = new Actual360();
            var today = Date.Today;

            Settings.setEvaluationDate(today);

            // construct a strange yield curve to check drifts and discounting
            // of the joint stochastic process

            var dates = new List<Date>();
            var times = new List<double>();
            List<double> rates = new List<double>(), divRates = new List<double>();

            for (var i = 0; i <= 31; ++i)
            {
                dates.Add(today + new Period(i, TimeUnit.Years));
                // FLOATING_POINT_EXCEPTION
                rates.Add(0.04 + 0.0001 * System.Math.Exp(System.Math.Sin(i)));
                divRates.Add(0.04 + 0.0001 * System.Math.Exp(System.Math.Sin(i)));
                times.Add(dc.yearFraction(today, dates.Last()));
            }

            var maturity = today + new Period(10, TimeUnit.Years);
            var v = 0.25;

            var s0 = new Handle<Quote>(new SimpleQuote(100));
            var vol = new SimpleQuote(v);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));
            var rTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, rates, dc));
            var qTS = new Handle<YieldTermStructure>(new InterpolatedZeroCurve<Linear>(dates, divRates, dc));

            var bsmProcess = new BlackScholesMertonProcess(s0, qTS, rTS, volTS);

            var hestonProcess = new HestonProcess(rTS, qTS, s0, v * v, 1, v * v, 1e-6, -0.4);

            var hwProcess = new HullWhiteForwardProcess(rTS, 0.01, 0.01);
            hwProcess.setForwardMeasureTime(20.1472222222222222);

            var tol = 0.05;
            double[] corr = { -0.85, 0.5 };
            double[] strike = { 50, 100, 125 };

            for (var i = 0; i < corr.Length; ++i)
            {
                for (var j = 0; j < strike.Length; ++j)
                {
                    StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, strike[j]);
                    Exercise exercise = new EuropeanExercise(maturity);

                    var optionBsmHW = new VanillaOption(payoff, exercise);
                    var hwModel = new HullWhite(rTS, hwProcess.a(), hwProcess.sigma());
                    optionBsmHW.setPricingEngine(new AnalyticBSMHullWhiteEngine(corr[i], bsmProcess, hwModel));

                    var expected = optionBsmHW.NPV();

                    var optionHestonHW = new VanillaOption(payoff, exercise);
                    var jointProcess = new HybridHestonHullWhiteProcess(hestonProcess,
                                                                                                 hwProcess, corr[i]);
                    optionHestonHW.setPricingEngine(
                       new MakeMCHestonHullWhiteEngine<PseudoRandom, Statistics>(jointProcess)
                       .withSteps(1)
                       .withAntitheticVariate()
                       .withAbsoluteTolerance(tol)
                       .withSeed(42).getAsPricingEngine());

                    var calculated = optionHestonHW.NPV();
                    var error = optionHestonHW.errorEstimate();

                    if (System.Math.Abs(calculated - expected) > 3 * error
                         && System.Math.Abs(calculated - expected) > 1e-5)
                    {
                        QAssert.Fail("Failed to reproduce discretization error"
                                     + "\n   corr:       " + corr[i]
                                     + "\n   strike:     " + strike[j]
                                     + "\n   calculated: " + calculated
                                     + "\n   error:      " + error
                                     + "\n   expected:   " + expected);
                    }
                }
            }
        }

        [Fact]
        public void testH1HWPricingEngine()
        {
            /*
             * Example taken from Lech Aleksander Grzelak,
             * Equity and Foreign Exchange Hybrid Models for Pricing Long-Maturity
             * Financial Derivatives,
             * http://repository.tudelft.nl/assets/uuid:a8e1a007-bd89-481a-aee3-0e22f15ade6b/PhDThesis_main.pdf
            */
            var today = new Date(15, Month.July, 2012);
            Settings.setEvaluationDate(today);
            var exerciseDate = new Date(13, Month.July, 2022);
            DayCounter dc = new Actual365Fixed();

            Exercise exercise = new EuropeanExercise(exerciseDate);

            var s0 = new Handle<Quote>(new SimpleQuote(100.0));

            var r = 0.02;
            var q = 0.00;
            var v0 = 0.05;
            var theta = 0.05;
            var kappa_v = 0.3;
            double[] sigma_v = { 0.3, 0.6 };
            var rho_sv = -0.30;
            var rho_sr = 0.6;
            var kappa_r = 0.01;
            var sigma_r = 0.01;

            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, r, dc));
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, q, dc));

            var flatVolTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, 0.20, dc));
            var bsProcess = new GeneralizedBlackScholesProcess(s0, qTS, rTS, flatVolTS);

            var hwProcess = new HullWhiteProcess(rTS, kappa_r, sigma_r);
            var hullWhiteModel = new HullWhite(new Handle<YieldTermStructure>(rTS), kappa_r, sigma_r);

            var tol = 0.0001;
            double[] strikes = { 40, 80, 100, 120, 180 };
            double[][] expected =
            {
            new double[] {0.267503, 0.235742, 0.228223, 0.223461, 0.217855},
            new double[]  {0.263626, 0.211625, 0.199907, 0.193502, 0.190025}
         };

            for (var j = 0; j < sigma_v.Length; ++j)
            {
                var hestonProcess = new HestonProcess(rTS, qTS, s0, v0, kappa_v, theta, sigma_v[j], rho_sv);
                var hestonModel = new HestonModel(hestonProcess);

                for (var i = 0; i < strikes.Length; ++i)
                {
                    StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, strikes[i]);

                    var option = new VanillaOption(payoff, exercise);

                    IPricingEngine analyticH1HWEngine = new AnalyticH1HWEngine(hestonModel, hullWhiteModel, rho_sr, 144);
                    option.setPricingEngine(analyticH1HWEngine);
                    var impliedH1HW = option.impliedVolatility(option.NPV(), bsProcess);

                    if (System.Math.Abs(expected[j][i] - impliedH1HW) > tol)
                    {
                        QAssert.Fail("Failed to reproduce H1HW implied volatility"
                                     + "\n   expected       : " + expected[j][i]
                                     + "\n   calculated     : " + impliedH1HW
                                     + "\n   tol            : " + tol
                                     + "\n   strike         : " + strikes[i]
                                     + "\n   sigma          : " + sigma_v[j]);
                    }
                }
            }
        }
    }
}
