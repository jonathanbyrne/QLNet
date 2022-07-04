/*
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
using QLNet.Instruments;
using QLNet.Pricingengines.vanilla;
using QLNet.Patterns;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.processes;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_DividendOption
    {
        internal void REPORT_FAILURE(string greekName, StrikedTypePayoff payoff, Exercise exercise, double s, double q,
                                   double r, Date today, double v, double expected, double calculated, double error,
                                   double tolerance)
        {
            QAssert.Fail(exercise + " "
                         + payoff.optionType() + " option with "
                         + payoff + " payoff:\n"
                         + "    spot value:       " + s + "\n"
                         + "    strike:           " + payoff.strike() + "\n"
                         + "    dividend yield:   " + q + "\n"
                         + "    risk-free rate:   " + r + "\n"
                         + "    reference date:   " + today + "\n"
                         + "    maturity:         " + exercise.lastDate() + "\n"
                         + "    volatility:       " + v + "\n\n"
                         + "    expected " + greekName + ":   " + expected + "\n"
                         + "    calculated " + greekName + ": " + calculated + "\n"
                         + "    error:            " + error + "\n"
                         + "    tolerance:        " + tolerance);
        }

        private void testFdGreeks<Engine>(Date today, Exercise exercise) where Engine : IFDEngine, new()
        {
            Dictionary<string, double> calculated = new Dictionary<string, double>(),
            expected = new Dictionary<string, double>(),
            tolerance = new Dictionary<string, double>();
            tolerance.Add("delta", 5.0e-3);
            tolerance.Add("gamma", 7.0e-3);
            // tolerance["theta"] = 1.0e-2;

            Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
            double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
            double[] underlyings = { 100.0 };
            double[] qRates = { 0.00, 0.10, 0.20 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            double[] vols = { 0.05, 0.20, 0.50 };

            DayCounter dc = new Actual360();

            var spot = new SimpleQuote(0.0);
            var qRate = new SimpleQuote(0.0);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
            var rRate = new SimpleQuote(0.0);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
            var vol = new SimpleQuote(0.0);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

            for (var i = 0; i < types.Length; i++)
            {
                for (var j = 0; j < strikes.Length; j++)
                {
                    var dividendDates = new List<Date>();
                    var dividends = new List<double>();
                    for (var d = today + new Period(3, TimeUnit.Months);
                         d < exercise.lastDate();
                         d += new Period(6, TimeUnit.Months))
                    {
                        dividendDates.Add(d);
                        dividends.Add(5.0);
                    }

                    StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                    var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                           qTS, rTS, volTS);

                    IPricingEngine engine = FastActivator<Engine>.Create().factory(stochProcess);
                    var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                    option.setPricingEngine(engine);

                    for (var l = 0; l < underlyings.Length; l++)
                    {
                        for (var m = 0; m < qRates.Length; m++)
                        {
                            for (var n = 0; n < rRates.Length; n++)
                            {
                                for (var p = 0; p < vols.Length; p++)
                                {
                                    var u = underlyings[l];
                                    double q = qRates[m],
                                           r = rRates[n];
                                    var v = vols[p];
                                    spot.setValue(u);
                                    qRate.setValue(q);
                                    rRate.setValue(r);
                                    vol.setValue(v);

                                    // FLOATING_POINT_EXCEPTION
                                    var value = option.NPV();
                                    calculated["delta"] = option.delta();
                                    calculated["gamma"] = option.gamma();
                                    // calculated["theta"]  = option.theta();

                                    if (value > spot.value() * 1.0e-5)
                                    {
                                        // perturb spot and get delta and gamma
                                        var du = u * 1.0e-4;
                                        spot.setValue(u + du);
                                        double value_p = option.NPV(),
                                               delta_p = option.delta();
                                        spot.setValue(u - du);
                                        double value_m = option.NPV(),
                                               delta_m = option.delta();
                                        spot.setValue(u);
                                        expected["delta"] = (value_p - value_m) / (2 * du);
                                        expected["gamma"] = (delta_p - delta_m) / (2 * du);

                                        // perturb date and get theta
                                        /*
                                           Time dT = dc.yearFraction(today-1, today+1);
                                           Settings::instance().evaluationDate() = today-1;
                                           value_m = option.NPV();
                                           Settings::instance().evaluationDate() = today+1;
                                           value_p = option.NPV();
                                           Settings::instance().evaluationDate() = today;
                                           expected["theta"] = (value_p - value_m)/dT;
                                        */

                                        // compare
                                        foreach (var greek in calculated.Keys)
                                        {
                                            double expct = expected[greek],
                                                   calcl = calculated[greek],
                                                   tol = tolerance[greek];
                                            var error = Utilities.relativeError(expct, calcl, u);
                                            if (error > tol)
                                            {
                                                REPORT_FAILURE(greek, payoff, exercise,
                                                               u, q, r, today, v,
                                                               expct, calcl, error, tol);
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

        private void testFdDegenerate<Engine>(Date today, Exercise exercise) where Engine : IFDEngine, new()
        {
            DayCounter dc = new Actual360();
            var spot = new SimpleQuote(54.625);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.052706, dc));
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, dc));
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(0.282922, dc));

            var process = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                              qTS, rTS, volTS);

            var timeSteps = 300;
            var gridPoints = 300;

            IPricingEngine engine = FastActivator<Engine>.Create().factory(process, timeSteps, gridPoints);

            StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 55.0);

            var tolerance = 3.0e-3;

            var dividends = new List<double>();
            var dividendDates = new List<Date>();

            var option1 = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
            option1.setPricingEngine(engine);

            // FLOATING_POINT_EXCEPTION
            var refValue = option1.NPV();

            for (var i = 0; i <= 6; i++)
            {
                dividends.Add(0.0);
                dividendDates.Add(today + i);

                var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                option.setPricingEngine(engine);
                var value = option.NPV();

                if (System.Math.Abs(refValue - value) > tolerance)
                    QAssert.Fail("NPV changed by null dividend :\n"
                                 + "    previous value: " + value + "\n"
                                 + "    current value:  " + refValue + "\n"
                                 + "    change:         " + (value - refValue));
            }
        }

        [Fact]
        public void testEuropeanValues()
        {
            // Testing dividend European option values with no dividends...
            using (var backup = new SavedSettings())
            {
                var tolerance = 1.0e-5;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
                double[] underlyings = { 100.0 };
                double[] qRates = { 0.00, 0.10, 0.30 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.05, 0.20, 0.70 };

                DayCounter dc = new Actual360();
                var today = Date.Today;
                Settings.setEvaluationDate(today);

                var spot = new SimpleQuote(0.0);
                var qRate = new SimpleQuote(0.0);
                var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
                var rRate = new SimpleQuote(0.0);
                var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
                var vol = new SimpleQuote(0.0);
                var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {
                            var exDate = today + new Period(lengths[k], TimeUnit.Years);
                            Exercise exercise = new EuropeanExercise(exDate);

                            var dividendDates = new List<Date>();
                            var dividends = new List<double>();
                            for (var d = today + new Period(3, TimeUnit.Months);
                                 d < exercise.lastDate();
                                 d += new Period(6, TimeUnit.Months))

                            {
                                dividendDates.Add(d);
                                dividends.Add(0.0);
                            }

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                                   qTS, rTS, volTS);

                            IPricingEngine ref_engine = new AnalyticEuropeanEngine(stochProcess);

                            IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

                            var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                            option.setPricingEngine(engine);

                            var ref_option = new VanillaOption(payoff, exercise);
                            ref_option.setPricingEngine(ref_engine);

                            for (var l = 0; l < underlyings.Length; l++)
                            {
                                for (var m = 0; m < qRates.Length; m++)
                                {
                                    for (var n = 0; n < rRates.Length; n++)
                                    {
                                        for (var p = 0; p < vols.Length; p++)
                                        {
                                            var u = underlyings[l];
                                            double q = qRates[m],
                                                   r = rRates[n];
                                            var v = vols[p];
                                            spot.setValue(u);
                                            qRate.setValue(q);
                                            rRate.setValue(r);
                                            vol.setValue(v);

                                            var calculated = option.NPV();
                                            var expected = ref_option.NPV();
                                            var error = System.Math.Abs(calculated - expected);
                                            if (error > tolerance)
                                            {
                                                REPORT_FAILURE("value start limit",
                                                               payoff, exercise,
                                                               u, q, r, today, v,
                                                               expected, calculated,
                                                               error, tolerance);
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

        // Reference pg. 253 - Hull - Options, Futures, and Other Derivatives 5th ed
        // Exercise 12.8
        // Doesn't quite work.  Need to deal with date conventions
        private void testEuropeanKnownValue()
        {
            // Testing dividend European option values with known value...
            using (var backup = new SavedSettings())
            {
                var tolerance = 1.0e-2;
                var expected = 3.67;

                DayCounter dc = new Actual360();
                var today = Date.Today;
                Settings.setEvaluationDate(today);

                var spot = new SimpleQuote(0.0);
                var qRate = new SimpleQuote(0.0);
                var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
                var rRate = new SimpleQuote(0.0);
                var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
                var vol = new SimpleQuote(0.0);
                var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

                var exDate = today + new Period(6, TimeUnit.Months);
                Exercise exercise = new EuropeanExercise(exDate);

                var dividendDates = new List<Date>();
                var dividends = new List<double>();
                dividendDates.Add(today + new Period(2, TimeUnit.Months));
                dividends.Add(0.50);
                dividendDates.Add(today + new Period(5, TimeUnit.Months));
                dividends.Add(0.50);

                StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 40.0);

                var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                       qTS, rTS, volTS);

                IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

                var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                option.setPricingEngine(engine);

                var u = 40.0;
                double q = 0.0, r = 0.09;
                var v = 0.30;
                spot.setValue(u);
                qRate.setValue(q);
                rRate.setValue(r);
                vol.setValue(v);

                var calculated = option.NPV();
                var error = System.Math.Abs(calculated - expected);
                if (error > tolerance)
                {
                    REPORT_FAILURE("value start limit",
                                   payoff, exercise,
                                   u, q, r, today, v,
                                   expected, calculated,
                                   error, tolerance);
                }
            }
        }

        [Fact]
        public void testEuropeanStartLimit()
        {
            // Testing dividend European option with a dividend on today's date...
            using (var backup = new SavedSettings())
            {
                var tolerance = 1.0e-5;
                var dividendValue = 10.0;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
                double[] underlyings = { 100.0 };
                double[] qRates = { 0.00, 0.10, 0.30 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.05, 0.20, 0.70 };

                DayCounter dc = new Actual360();
                var today = Date.Today;
                Settings.setEvaluationDate(today);

                var spot = new SimpleQuote(0.0);
                var qRate = new SimpleQuote(0.0);
                var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
                var rRate = new SimpleQuote(0.0);
                var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
                var vol = new SimpleQuote(0.0);
                var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {
                            var exDate = today + new Period(lengths[k], TimeUnit.Years);
                            Exercise exercise = new EuropeanExercise(exDate);

                            var dividendDates = new List<Date>();
                            var dividends = new List<double>();
                            dividendDates.Add(today);
                            dividends.Add(dividendValue);

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                                   qTS, rTS, volTS);

                            IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

                            IPricingEngine ref_engine = new AnalyticEuropeanEngine(stochProcess);

                            var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                            option.setPricingEngine(engine);

                            var ref_option = new VanillaOption(payoff, exercise);
                            ref_option.setPricingEngine(ref_engine);

                            for (var l = 0; l < underlyings.Length; l++)
                            {
                                for (var m = 0; m < qRates.Length; m++)
                                {
                                    for (var n = 0; n < rRates.Length; n++)
                                    {
                                        for (var p = 0; p < vols.Length; p++)
                                        {
                                            var u = underlyings[l];
                                            double q = qRates[m],
                                                   r = rRates[n];
                                            var v = vols[p];
                                            spot.setValue(u);
                                            qRate.setValue(q);
                                            rRate.setValue(r);
                                            vol.setValue(v);

                                            var calculated = option.NPV();
                                            spot.setValue(u - dividendValue);
                                            var expected = ref_option.NPV();
                                            var error = System.Math.Abs(calculated - expected);
                                            if (error > tolerance)
                                            {
                                                REPORT_FAILURE("value", payoff, exercise,
                                                               u, q, r, today, v,
                                                               expected, calculated,
                                                               error, tolerance);
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

        [Fact]
        public void testEuropeanGreeks()
        {
            // Testing dividend European option greeks...
            using (var backup = new SavedSettings())
            {
                Dictionary<string, double> calculated = new Dictionary<string, double>(),
                expected = new Dictionary<string, double>(),
                tolerance = new Dictionary<string, double>();
                tolerance["delta"] = 1.0e-5;
                tolerance["gamma"] = 1.0e-5;
                tolerance["theta"] = 1.0e-5;
                tolerance["rho"] = 1.0e-5;
                tolerance["vega"] = 1.0e-5;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
                double[] underlyings = { 100.0 };
                double[] qRates = { 0.00, 0.10, 0.30 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.05, 0.20, 0.40 };

                DayCounter dc = new Actual360();
                var today = Date.Today;
                Settings.setEvaluationDate(today);

                var spot = new SimpleQuote(0.0);
                var qRate = new SimpleQuote(0.0);
                var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
                var rRate = new SimpleQuote(0.0);
                var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
                var vol = new SimpleQuote(0.0);
                var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {
                            var exDate = today + new Period(lengths[k], TimeUnit.Years);
                            Exercise exercise = new EuropeanExercise(exDate);

                            var dividendDates = new List<Date>();
                            var dividends = new List<double>();
                            for (var d = today + new Period(3, TimeUnit.Months);
                                 d < exercise.lastDate();
                                 d += new Period(6, TimeUnit.Months))
                            {
                                dividendDates.Add(d);
                                dividends.Add(5.0);
                            }

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                                   qTS, rTS, volTS);

                            IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

                            var option = new DividendVanillaOption(payoff, exercise, dividendDates,
                                                                                     dividends);
                            option.setPricingEngine(engine);

                            for (var l = 0; l < underlyings.Length; l++)
                            {
                                for (var m = 0; m < qRates.Length; m++)
                                {
                                    for (var n = 0; n < rRates.Length; n++)
                                    {
                                        for (var p = 0; p < vols.Length; p++)
                                        {
                                            var u = underlyings[l];
                                            double q = qRates[m],
                                                   r = rRates[n];
                                            var v = vols[p];
                                            spot.setValue(u);
                                            qRate.setValue(q);
                                            rRate.setValue(r);
                                            vol.setValue(v);

                                            var value = option.NPV();
                                            calculated["delta"] = option.delta();
                                            calculated["gamma"] = option.gamma();
                                            calculated["theta"] = option.theta();
                                            calculated["rho"] = option.rho();
                                            calculated["vega"] = option.vega();

                                            if (value > spot.value() * 1.0e-5)
                                            {
                                                // perturb spot and get delta and gamma
                                                var du = u * 1.0e-4;
                                                spot.setValue(u + du);
                                                double value_p = option.NPV(),
                                                       delta_p = option.delta();
                                                spot.setValue(u - du);
                                                double value_m = option.NPV(),
                                                       delta_m = option.delta();
                                                spot.setValue(u);
                                                expected["delta"] = (value_p - value_m) / (2 * du);
                                                expected["gamma"] = (delta_p - delta_m) / (2 * du);

                                                // perturb risk-free rate and get rho
                                                var dr = r * 1.0e-4;
                                                rRate.setValue(r + dr);
                                                value_p = option.NPV();
                                                rRate.setValue(r - dr);
                                                value_m = option.NPV();
                                                rRate.setValue(r);
                                                expected["rho"] = (value_p - value_m) / (2 * dr);

                                                // perturb volatility and get vega
                                                var dv = v * 1.0e-4;
                                                vol.setValue(v + dv);
                                                value_p = option.NPV();
                                                vol.setValue(v - dv);
                                                value_m = option.NPV();
                                                vol.setValue(v);
                                                expected["vega"] = (value_p - value_m) / (2 * dv);

                                                // perturb date and get theta
                                                var dT = dc.yearFraction(today - 1, today + 1);
                                                Settings.setEvaluationDate(today - 1);
                                                value_m = option.NPV();
                                                Settings.setEvaluationDate(today + 1);
                                                value_p = option.NPV();
                                                Settings.setEvaluationDate(today);
                                                expected["theta"] = (value_p - value_m) / dT;

                                                // compare
                                                foreach (var it in calculated)
                                                {
                                                    var greek = it.Key;
                                                    double expct = expected[greek],
                                                           calcl = calculated[greek],
                                                           tol = tolerance[greek];
                                                    var error = Utilities.relativeError(expct, calcl, u);
                                                    if (error > tol)
                                                    {
                                                        REPORT_FAILURE(greek, payoff, exercise,
                                                                       u, q, r, today, v,
                                                                       expct, calcl, error, tol);
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
        }

        [Fact]
        public void testFdEuropeanValues()
        {
            // Testing finite-difference dividend European option values...
            using (var backup = new SavedSettings())
            {
                var tolerance = 1.0e-2;
                var gridPoints = 300;
                var timeSteps = 40;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
                double[] underlyings = { 100.0 };
                // Rate qRates[] = { 0.00, 0.10, 0.30 };
                // Analytic dividend may not be handling q correctly
                double[] qRates = { 0.00 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.05, 0.20, 0.40 };

                DayCounter dc = new Actual360();
                var today = Date.Today;
                Settings.setEvaluationDate(today);

                var spot = new SimpleQuote(0.0);
                var qRate = new SimpleQuote(0.0);
                var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
                var rRate = new SimpleQuote(0.0);
                var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
                var vol = new SimpleQuote(0.0);
                var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {
                            var exDate = today + new Period(lengths[k], TimeUnit.Years);
                            Exercise exercise = new EuropeanExercise(exDate);

                            var dividendDates = new List<Date>();
                            var dividends = new List<double>();
                            for (var d = today + new Period(3, TimeUnit.Months);
                                 d < exercise.lastDate();
                                 d += new Period(6, TimeUnit.Months))
                            {
                                dividendDates.Add(d);
                                dividends.Add(5.0);
                            }

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                                   qTS, rTS, volTS);

                            IPricingEngine engine = new FDDividendEuropeanEngine(stochProcess, timeSteps, gridPoints);

                            IPricingEngine ref_engine = new AnalyticDividendEuropeanEngine(stochProcess);

                            var option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                            option.setPricingEngine(engine);

                            var ref_option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
                            ref_option.setPricingEngine(ref_engine);

                            for (var l = 0; l < underlyings.Length; l++)
                            {
                                for (var m = 0; m < qRates.Length; m++)
                                {
                                    for (var n = 0; n < rRates.Length; n++)
                                    {
                                        for (var p = 0; p < vols.Length; p++)
                                        {
                                            var u = underlyings[l];
                                            double q = qRates[m],
                                                   r = rRates[n];
                                            var v = vols[p];
                                            spot.setValue(u);
                                            qRate.setValue(q);
                                            rRate.setValue(r);
                                            vol.setValue(v);
                                            // FLOATING_POINT_EXCEPTION
                                            var calculated = option.NPV();
                                            if (calculated > spot.value() * 1.0e-5)
                                            {
                                                var expected = ref_option.NPV();
                                                var error = System.Math.Abs(calculated - expected);
                                                if (error > tolerance)
                                                {
                                                    REPORT_FAILURE("value", payoff, exercise,
                                                                   u, q, r, today, v,
                                                                   expected, calculated,
                                                                   error, tolerance);
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

        [Fact]
        public void testFdEuropeanGreeks()
        {
            // Testing finite-differences dividend European option greeks...
            using (var backup = new SavedSettings())
            {
                var today = Date.Today;
                Settings.setEvaluationDate(today);
                int[] lengths = { 1, 2 };

                for (var i = 0; i < lengths.Length; i++)
                {
                    var exDate = today + new Period(lengths[i], TimeUnit.Years);
                    Exercise exercise = new EuropeanExercise(exDate);
                    testFdGreeks<FDDividendEuropeanEngine>(today, exercise);
                }
            }
        }

        [Fact]
        public void testFdAmericanGreeks()
        {
            // Testing finite-differences dividend American option greeks...
            using (var backup = new SavedSettings())
            {
                var today = Date.Today;
                Settings.setEvaluationDate(today);
                int[] lengths = { 1, 2 };

                for (var i = 0; i < lengths.Length; i++)
                {
                    var exDate = today + new Period(lengths[i], TimeUnit.Years);
                    Exercise exercise = new AmericanExercise(exDate);
                    testFdGreeks<FDDividendAmericanEngine>(today, exercise);
                }
            }
        }

        [Fact]
        public void testFdEuropeanDegenerate()
        {
            // Testing degenerate finite-differences dividend European option...
            using (var backup = new SavedSettings())
            {
                var today = new Date(27, Month.February, 2005);
                Settings.setEvaluationDate(today);
                var exDate = new Date(13, Month.April, 2005);

                Exercise exercise = new EuropeanExercise(exDate);

                testFdDegenerate<FDDividendEuropeanEngine>(today, exercise);
            }
        }

        [Fact]
        public void testFdAmericanDegenerate()
        {
            // Testing degenerate finite-differences dividend American option...
            using (var backup = new SavedSettings())
            {
                var today = new Date(27, Month.February, 2005);
                Settings.setEvaluationDate(today);
                var exDate = new Date(13, Month.April, 2005);

                Exercise exercise = new AmericanExercise(exDate);

                testFdDegenerate<FDDividendAmericanEngine>(today, exercise);
            }
        }
    }
}
