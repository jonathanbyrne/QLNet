﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Instruments;
using QLNet.Math.RandomNumbers;
using QLNet.Time;
using QLNet.Math.statistics;
using QLNet.PricingEngines.asian;
using QLNet.Processes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_AsianOptions
    {
        internal void REPORT_FAILURE(string greekName, Average.Type averageType,
                                   double? runningAccumulator, int? pastFixings,
                                   List<Date> fixingDates, StrikedTypePayoff payoff,
                                   Exercise exercise, double s, double q, double r,
                                   Date today, double v, double expected,
                                   double calculated, double tolerance)
        {
            QAssert.Fail(exercise + " "
                         + exercise
                         + " Asian option with "
                         + averageType + " and "
                         + payoff + " payoff:\n"
                         + "    running variable: "
                         + runningAccumulator + "\n"
                         + "    past fixings:     "
                         + pastFixings + "\n"
                         + "    future fixings:   " + fixingDates.Count() + "\n"
                         + "    underlying value: " + s + "\n"
                         + "    strike:           " + payoff.strike() + "\n"
                         + "    dividend yield:   " + q + "\n"
                         + "    risk-free rate:   " + r + "\n"
                         + "    reference date:   " + today + "\n"
                         + "    maturity:         " + exercise.lastDate() + "\n"
                         + "    volatility:       " + v + "\n\n"
                         + "    expected   " + greekName + ": " + expected + "\n"
                         + "    calculated " + greekName + ": " + calculated + "\n"
                         + "    error:            " + System.Math.Abs(expected - calculated)
                         + "\n"
                         + "    tolerance:        " + tolerance);
        }

        public string averageTypeToString(Average.Type averageType)
        {

            if (averageType == Average.Type.Geometric)
            {
                return "Geometric Averaging";
            }
            else if (averageType == Average.Type.Arithmetic)
            {
                return "Arithmetic Averaging";
            }
            else
            {
                QLNet.Utils.QL_FAIL("unknown averaging");
            }

            return string.Empty;
        }

        [Fact]
        public void testAnalyticContinuousGeometricAveragePrice()
        {
            // Testing analytic continuous geometric average-price Asians
            // data from "Option Pricing Formulas", Haug, pag.96-97

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(80.0);
            var qRate = new SimpleQuote(-0.03);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.05);
            var rTS = Utilities.flatRate(today, rRate, dc);
            var vol = new SimpleQuote(0.20);
            var volTS = Utilities.flatVol(today, vol, dc);

            var stochProcess = new
            BlackScholesMertonProcess(new Handle<Quote>(spot),
                                      new Handle<YieldTermStructure>(qTS),
                                      new Handle<YieldTermStructure>(rTS),
                                      new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new
            AnalyticContinuousGeometricAveragePriceAsianEngine(stochProcess);

            var averageType = Average.Type.Geometric;
            var type = QLNet.Option.Type.Put;
            var strike = 85.0;
            var exerciseDate = today + 90;

            int? pastFixings = null;
            double? runningAccumulator = null;

            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

            Exercise exercise = new EuropeanExercise(exerciseDate);

            var option = new ContinuousAveragingAsianOption(averageType, payoff, exercise);
            option.setPricingEngine(engine);

            var calculated = option.NPV();
            var expected = 4.6922;
            var tolerance = 1.0e-4;
            if (System.Math.Abs(calculated - expected) > tolerance)
            {
                REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                               new List<Date>(), payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated, tolerance);
            }

            // trying to approximate the continuous version with the discrete version
            runningAccumulator = 1.0;
            pastFixings = 0;
            List<Date> fixingDates = new InitializedList<Date>(exerciseDate - today + 1);
            for (var i = 0; i < fixingDates.Count; i++)
            {
                fixingDates[i] = today + i;
            }
            IPricingEngine engine2 = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

            var option2 = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                    pastFixings, fixingDates, payoff, exercise);

            option2.setPricingEngine(engine2);

            calculated = option2.NPV();
            tolerance = 3.0e-3;
            if (System.Math.Abs(calculated - expected) > tolerance)
            {
                REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                               fixingDates, payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated, tolerance);
            }

        }

        [Fact]
        public void testAnalyticContinuousGeometricAveragePriceGreeks()
        {
            // Testing analytic continuous geometric average-price Asian greeks
            using (var backup = new SavedSettings())
            {
                Dictionary<string, double> calculated, expected, tolerance;
                calculated = new Dictionary<string, double>(6);
                expected = new Dictionary<string, double>(6);
                tolerance = new Dictionary<string, double>(6);
                tolerance["delta"] = 1.0e-5;
                tolerance["gamma"] = 1.0e-5;
                tolerance["theta"] = 1.0e-5;
                tolerance["rho"] = 1.0e-5;
                tolerance["divRho"] = 1.0e-5;
                tolerance["vega"] = 1.0e-5;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] underlyings = { 100.0 };
                double[] strikes = { 90.0, 100.0, 110.0 };
                double[] qRates = { 0.04, 0.05, 0.06 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.11, 0.50, 1.20 };

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

                var process = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {

                            var maturity = new EuropeanExercise(today + new Period(lengths[k], TimeUnit.Years));
                            var payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            IPricingEngine engine = new AnalyticContinuousGeometricAveragePriceAsianEngine(process);

                            var option = new ContinuousAveragingAsianOption(Average.Type.Geometric,
                                                                                                       payoff, maturity);
                            option.setPricingEngine(engine);

                            int? pastFixings = null;
                            double? runningAverage = null;

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
                                            calculated["divRho"] = option.dividendRho();
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

                                                // perturb rates and get rho and dividend rho
                                                var dr = r * 1.0e-4;
                                                rRate.setValue(r + dr);
                                                value_p = option.NPV();
                                                rRate.setValue(r - dr);
                                                value_m = option.NPV();
                                                rRate.setValue(r);
                                                expected["rho"] = (value_p - value_m) / (2 * dr);

                                                var dq = q * 1.0e-4;
                                                qRate.setValue(q + dq);
                                                value_p = option.NPV();
                                                qRate.setValue(q - dq);
                                                value_m = option.NPV();
                                                qRate.setValue(q);
                                                expected["divRho"] = (value_p - value_m) / (2 * dq);

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
                                                foreach (var kvp in calculated)
                                                {
                                                    var greek = kvp.Key;
                                                    double expct = expected[greek],
                                                           calcl = calculated[greek],
                                                           tol = tolerance[greek];
                                                    var error = Utilities.relativeError(expct, calcl, u);
                                                    if (error > tol)
                                                    {
                                                        REPORT_FAILURE(greek, Average.Type.Geometric,
                                                                       runningAverage, pastFixings,
                                                                       new List<Date>(),
                                                                       payoff, maturity,
                                                                       u, q, r, today, v,
                                                                       expct, calcl, tol);
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
        public void testAnalyticDiscreteGeometricAveragePrice()
        {
            // Testing analytic discrete geometric average-price Asians
            // data from "Implementing Derivatives Model",
            // Clewlow, Strickland, p.118-123

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(100.0);
            var qRate = new SimpleQuote(0.03);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.06);
            var rTS = Utilities.flatRate(today, rRate, dc);
            var vol = new SimpleQuote(0.20);
            var volTS = Utilities.flatVol(today, vol, dc);

            var stochProcess = new
            BlackScholesMertonProcess(new Handle<Quote>(spot),
                                      new Handle<YieldTermStructure>(qTS),
                                      new Handle<YieldTermStructure>(rTS),
                                      new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

            var averageType = Average.Type.Geometric;
            var runningAccumulator = 1.0;
            var pastFixings = 0;
            var futureFixings = 10;
            var type = QLNet.Option.Type.Call;
            var strike = 100.0;
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

            var exerciseDate = today + 360;
            Exercise exercise = new EuropeanExercise(exerciseDate);

            List<Date> fixingDates = new InitializedList<Date>(futureFixings);
            var dt = (int)(360 / futureFixings + 0.5);
            fixingDates[0] = today + dt;
            for (var j = 1; j < futureFixings; j++)
            {
                fixingDates[j] = fixingDates[j - 1] + dt;
            }

            var option = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                   pastFixings, fixingDates, payoff, exercise);
            option.setPricingEngine(engine);

            var calculated = option.NPV();
            var expected = 5.3425606635;
            var tolerance = 1e-10;
            if (System.Math.Abs(calculated - expected) > tolerance)
            {
                REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                               fixingDates, payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated, tolerance);
            }
        }

        [Fact]
        public void testAnalyticDiscreteGeometricAverageStrike()
        {
            // Testing analytic discrete geometric average-strike Asians

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(100.0);
            var qRate = new SimpleQuote(0.03);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.06);
            var rTS = Utilities.flatRate(today, rRate, dc);
            var vol = new SimpleQuote(0.20);
            var volTS = Utilities.flatVol(today, vol, dc);

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS),
                                                                                   new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new AnalyticDiscreteGeometricAverageStrikeAsianEngine(stochProcess);

            var averageType = Average.Type.Geometric;
            var runningAccumulator = 1.0;
            var pastFixings = 0;
            var futureFixings = 10;
            var type = QLNet.Option.Type.Call;
            var strike = 100.0;
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

            var exerciseDate = today + 360;
            Exercise exercise = new EuropeanExercise(exerciseDate);

            List<Date> fixingDates = new InitializedList<Date>(futureFixings);
            var dt = (int)(360 / futureFixings + 0.5);
            fixingDates[0] = today + dt;
            for (var j = 1; j < futureFixings; j++)
            {
                fixingDates[j] = fixingDates[j - 1] + dt;
            }

            var option = new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                                                   pastFixings, fixingDates, payoff, exercise);
            option.setPricingEngine(engine);

            var calculated = option.NPV();
            var expected = 4.97109;
            var tolerance = 1e-5;
            if (System.Math.Abs(calculated - expected) > tolerance)
            {
                REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                               fixingDates, payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated, tolerance);
            }

        }

        [Fact(Skip = "Incomplete")]
        public void testMCDiscreteGeometricAveragePrice()
        {
            // Testing Monte Carlo discrete geometric average-price Asians
            // data from "Implementing Derivatives Model",
            // Clewlow, Strickland, p.118-123

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(100.0);
            var qRate = new SimpleQuote(0.03);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.06);
            var rTS = Utilities.flatRate(today, rRate, dc);
            var vol = new SimpleQuote(0.20);
            var volTS = Utilities.flatVol(today, vol, dc);

            var stochProcess =
               new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                             new Handle<YieldTermStructure>(qTS),
                                             new Handle<YieldTermStructure>(rTS),
                                             new Handle<BlackVolTermStructure>(volTS));

            var tolerance = 4.0e-3;

            var engine =
               new MakeMCDiscreteGeometricAPEngine
            <LowDiscrepancy, Statistics>(stochProcess)
            .withStepsPerYear(1)
            .withSamples(8191)
            .value();

            var averageType = Average.Type.Geometric;
            var runningAccumulator = 1.0;
            var pastFixings = 0;
            var futureFixings = 10;
            var type = QLNet.Option.Type.Call;
            var strike = 100.0;
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

            var exerciseDate = today + 360;
            Exercise exercise = new EuropeanExercise(exerciseDate);

            List<Date> fixingDates = new InitializedList<Date>(futureFixings);
            var dt = (int)(360 / futureFixings + 0.5);
            fixingDates[0] = today + dt;
            for (var j = 1; j < futureFixings; j++)
            {
                fixingDates[j] = fixingDates[j - 1] + dt;
            }

            var option =
               new DiscreteAveragingAsianOption(averageType, runningAccumulator,
                                                pastFixings, fixingDates,
                                                payoff, exercise);
            option.setPricingEngine(engine);

            var calculated = option.NPV();

            IPricingEngine engine2 = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);
            option.setPricingEngine(engine2);
            var expected = option.NPV();

            if (System.Math.Abs(calculated - expected) > tolerance)
            {
                REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
                               fixingDates, payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated, tolerance);
            }
        }

        [Fact]
        public void testAnalyticDiscreteGeometricAveragePriceGreeks()
        {
            // Testing discrete-averaging geometric Asian greeks

            using (var backup = new SavedSettings())
            {
                Dictionary<string, double> calculated, expected, tolerance;
                calculated = new Dictionary<string, double>(6);
                expected = new Dictionary<string, double>(6);
                tolerance = new Dictionary<string, double>(6);
                tolerance["delta"] = 1.0e-5;
                tolerance["gamma"] = 1.0e-5;
                tolerance["theta"] = 1.0e-5;
                tolerance["rho"] = 1.0e-5;
                tolerance["divRho"] = 1.0e-5;
                tolerance["vega"] = 1.0e-5;

                Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
                double[] underlyings = { 100.0 };
                double[] strikes = { 90.0, 100.0, 110.0 };
                double[] qRates = { 0.04, 0.05, 0.06 };
                double[] rRates = { 0.01, 0.05, 0.15 };
                int[] lengths = { 1, 2 };
                double[] vols = { 0.11, 0.50, 1.20 };

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

                var process = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

                for (var i = 0; i < types.Length; i++)
                {
                    for (var j = 0; j < strikes.Length; j++)
                    {
                        for (var k = 0; k < lengths.Length; k++)
                        {
                            var maturity = new EuropeanExercise(today + new Period(lengths[k], TimeUnit.Years));

                            var payoff = new PlainVanillaPayoff(types[i], strikes[j]);

                            double runningAverage = 120;
                            var pastFixings = 1;

                            var fixingDates = new List<Date>();
                            for (var d = today + new Period(3, TimeUnit.Months);
                                 d <= maturity.lastDate();
                                 d += new Period(3, TimeUnit.Months))
                            {
                                fixingDates.Add(d);
                            }

                            IPricingEngine engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(process);

                            var option = new DiscreteAveragingAsianOption(Average.Type.Geometric,
                                                                                                   runningAverage, pastFixings, fixingDates, payoff, maturity);

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
                                            calculated["divRho"] = option.dividendRho();
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

                                                // perturb rates and get rho and dividend rho
                                                var dr = r * 1.0e-4;
                                                rRate.setValue(r + dr);
                                                value_p = option.NPV();
                                                rRate.setValue(r - dr);
                                                value_m = option.NPV();
                                                rRate.setValue(r);
                                                expected["rho"] = (value_p - value_m) / (2 * dr);

                                                var dq = q * 1.0e-4;
                                                qRate.setValue(q + dq);
                                                value_p = option.NPV();
                                                qRate.setValue(q - dq);
                                                value_m = option.NPV();
                                                qRate.setValue(q);
                                                expected["divRho"] = (value_p - value_m) / (2 * dq);

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
                                                foreach (var kvp in calculated)
                                                {
                                                    var greek = kvp.Key;
                                                    double expct = expected[greek],
                                                           calcl = calculated[greek],
                                                           tol = tolerance[greek];
                                                    var error = Utilities.relativeError(expct, calcl, u);
                                                    if (error > tol)
                                                    {
                                                        REPORT_FAILURE(greek, Average.Type.Geometric,
                                                                       runningAverage, pastFixings,
                                                                       new List<Date>(),
                                                                       payoff, maturity,
                                                                       u, q, r, today, v,
                                                                       expct, calcl, tol);
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
        public void testIssue115()
        {
            var timer = DateTime.Now;

            // set up dates
            Calendar calendar = new TARGET();
            var todaysDate = new Date(1, Month.January, 2017);
            var settlementDate = new Date(1, Month.January, 2017);
            var maturity = new Date(17, Month.May, 2018);
            Settings.setEvaluationDate(todaysDate);

            // our options
            var type = QLNet.Option.Type.Call;
            double underlying = 100;
            double strike = 100;
            var dividendYield = 0.00;
            var riskFreeRate = 0.06;
            var volatility = 0.20;

            DayCounter dayCounter = new Actual365Fixed();
            Exercise europeanExercise = new EuropeanExercise(maturity);

            double? accumulator = underlying;
            int? pastfixingcount = 1;
            var fixings = new List<Date>();
            fixings.Add(new Date(1, 1, 2018));

            var underlyingH = new Handle<Quote>(new SimpleQuote(underlying));
            // bootstrap the yield/dividend/vol curves
            var flatTermStructure = new Handle<YieldTermStructure>(new FlatForward(settlementDate, riskFreeRate, dayCounter));
            var flatDividendTS = new Handle<YieldTermStructure>(new FlatForward(settlementDate, dividendYield, dayCounter));
            var flatVolTS = new Handle<BlackVolTermStructure>(new BlackConstantVol(settlementDate, calendar, volatility, dayCounter));
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);
            var bsmProcess = new BlackScholesMertonProcess(underlyingH, flatDividendTS, flatTermStructure, flatVolTS);

            // options
            var europeanOption = new VanillaOption(payoff, europeanExercise);
            var callpayoff = new PlainVanillaPayoff(type, strike);

            var asianoption = new DiscreteAveragingAsianOption(
               Average.Type.Arithmetic,
               accumulator,
               pastfixingcount,
               fixings,
               callpayoff,
               europeanExercise);

            var minSamples = 10000;
            var maxSamples = 10000;
            ulong seed = 42;
            var tolerance = 1.0;

            var pricingengine = new MCDiscreteArithmeticAPEngine<PseudoRandom, GeneralStatistics>(
               bsmProcess,
               252,
               false,
               false,
               false,
               minSamples,
               tolerance,
               maxSamples,
               seed);

            asianoption.setPricingEngine(pricingengine);

            var price = asianoption.NPV();
        }

        //    public struct DiscreteAverageData
        //    {
        //        public QLNet.Option.Type ExerciseType;
        //        public double underlying;
        //        public double strike;
        //        public double dividendYield;
        //        public double riskFreeRate;
        //        public double first;
        //        public double length;
        //        public int fixings;
        //        public double volatility;
        //        public bool controlVariate;
        //        public double result;

        //        public DiscreteAverageData(Option.Type Type,
        //                                    double Underlying,
        //                                    double Strike,
        //                                    double DividendYield,
        //                                    double RiskFreeRate,
        //                                    double First,
        //                                    double Length,
        //                                    int Fixings,
        //                                    double Volatility,
        //                                    bool ControlVariate,
        //                                    double Result)
        //        {
        //            ExerciseType = Type;
        //            underlying = Underlying;
        //            strike = Strike;
        //            dividendYield = DividendYield;
        //            riskFreeRate = RiskFreeRate;
        //            first = First;
        //            length = Length;
        //            fixings = Fixings;
        //            volatility = Volatility;
        //            controlVariate = ControlVariate;
        //            result = Result;
        //        }
        //    }



        //    [TestMethod()]
        //    public void testAnalyticContinuousGeometricAveragePrice()
        //    {

        //        //("Testing analytic continuous geometric average-price Asians...");
        //        // data from "Option Pricing Formulas", Haug, pag.96-97

        //        DayCounter dc = new Actual360();
        //        Date today = Date.Today;

        //        SimpleQuote spot = new SimpleQuote(80.0);
        //        SimpleQuote qRate = new SimpleQuote(-0.03);
        //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
        //        SimpleQuote rRate = new SimpleQuote(0.05);
        //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
        //        SimpleQuote vol = new SimpleQuote(0.20);
        //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

        //        BlackScholesMertonProcess stochProcess = new
        //            BlackScholesMertonProcess(new Handle<Quote>(spot),
        //                                      new Handle<YieldTermStructure>(qTS),
        //                                      new Handle<YieldTermStructure>(rTS),
        //                                      new Handle<BlackVolTermStructure>(volTS));

        //        IPricingEngine engine = new
        //            AnalyticContinuousGeometricAveragePriceAsianEngine(stochProcess);

        //        Average.Type averageType = Average.Type.Geometric;
        //        QLNet.Option.Type ExerciseType = QLNet.Option.Type.Put;
        //        double strike = 85.0;
        //        Date exerciseDate = today + 90;

        //        int pastFixings = 0; //Null<int>();
        //        double runningAccumulator = 0.0; //Null<Real>();

        //        StrikedTypePayoff payoff = new PlainVanillaPayoff(ExerciseType, strike);

        //        Exercise exercise = new EuropeanExercise(exerciseDate);

        //        ContinuousAveragingAsianOption option =
        //            new ContinuousAveragingAsianOption(averageType, payoff, exercise);
        //        option.setPricingEngine(engine);

        //        double calculated = option.NPV();
        //        double expected = 4.6922;
        //        double tolerance = 1.0e-4;
        //        if (System.Math.Abs(calculated - expected) > tolerance)
        //        {
        //            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
        //                           new List<Date>(), payoff, exercise, spot.value(),
        //                           qRate.value(), rRate.value(), today,
        //                           vol.value(), expected, calculated, tolerance);
        //        }

        //        // trying to approximate the continuous version with the discrete version
        //        runningAccumulator = 1.0;
        //        pastFixings = 0;
        //        List<Date> fixingDates = new InitializedList<Date>(exerciseDate - today + 1);
        //        for (int i = 0; i < fixingDates.Count; i++)
        //        {
        //            fixingDates[i] = today + i;
        //        }
        //        IPricingEngine engine2 = new
        //            AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

        //        DiscreteAveragingAsianOption option2 =
        //            new DiscreteAveragingAsianOption(averageType,
        //                                             runningAccumulator, pastFixings,
        //                                             fixingDates,
        //                                             payoff,
        //                                             exercise);
        //        option2.setPricingEngine(engine2);

        //        calculated = option2.NPV();
        //        tolerance = 3.0e-3;
        //        /*if (System.Math.Abs(calculated - expected) > tolerance)
        //        {
        //            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
        //                           fixingDates, payoff, exercise, spot.value(),
        //                           qRate.value(), rRate.value(), today,
        //                           vol.value(), expected, calculated, tolerance);
        //        }*/

        //    }



        //    [TestMethod()]
        //    public void testMCDiscreteArithmeticAveragePrice() {

        //        //BOOST_MESSAGE("Testing Monte Carlo discrete arithmetic average-price Asians...");

        //        //QL_TEST_START_TIMING

        //        // data from "Asian Option", Levy, 1997
        //        // in "Exotic Options: The State of the Art",
        //        // edited by Clewlow, Strickland

        //        DiscreteAverageData[] cases4 = {
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 2,0.13, true, 1.3942835683),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 4,0.13, true, 1.5852442983),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 8,0.13, true, 1.66970673),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 12,0.13, true, 1.6980019214),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 26,0.13, true, 1.7255070456),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 52,0.13, true, 1.7401553533),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 100,0.13, true, 1.7478303712),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 250,0.13, true, 1.7490291943),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 500,0.13, true, 1.7515113291),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 1000,0.13, true, 1.7537344885),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 2,0.13, true, 1.8496053697),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 4,0.13, true, 2.0111495205),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 8,0.13, true, 2.0852138818),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 12,0.13, true, 2.1105094397),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 26,0.13, true, 2.1346526695),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 52,0.13, true, 2.147489651),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 100,0.13, true, 2.154728109),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 250,0.13, true, 2.1564276565),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 500,0.13, true, 2.1594238588),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 1000,0.13, true, 2.1595367326),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 2,0.13, true, 2.63315092584),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 4,0.13, true, 2.76723962361),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 8,0.13, true, 2.83124836881),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 12,0.13, true, 2.84290301412),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 26,0.13, true, 2.88179560417),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 52,0.13, true, 2.88447044543),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 100,0.13, true, 2.89985329603),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 250,0.13, true, 2.90047296063),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 500,0.13, true, 2.89813412160),
        //        new DiscreteAverageData(QLNet.Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 1000,0.13, true, 2.89703362437)
        //        };

        //        DayCounter dc = new Actual360();
        //        Date today = Date.Today ;

        //        SimpleQuote spot = new SimpleQuote(100.0);
        //        SimpleQuote qRate = new SimpleQuote(0.03);
        //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
        //        SimpleQuote rRate = new SimpleQuote(0.06);
        //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
        //        SimpleQuote vol = new SimpleQuote(0.20);
        //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);



        //        Average.Type averageType = Average.Type.Arithmetic;
        //        double runningSum = 0.0;
        //        int pastFixings = 0;
        //        for (int l=0; l<cases4.Length ; l++) {

        //            StrikedTypePayoff payoff = new
        //                PlainVanillaPayoff(cases4[l].ExerciseType, cases4[l].strike);

        //            double dt = cases4[l].length/(cases4[l].fixings-1);
        //            List<double> timeIncrements = new QLNet.InitializedList<double>(cases4[l].fixings);
        //            List<Date> fixingDates = new QLNet.InitializedList<Date>(cases4[l].fixings);
        //            timeIncrements[0] = cases4[l].first;
        //            fixingDates[0] = today + (int)(timeIncrements[0]*360+0.5);
        //            for (int i=1; i<cases4[l].fixings; i++) {
        //                timeIncrements[i] = i*dt + cases4[l].first;
        //                fixingDates[i] = today + (int)(timeIncrements[i]*360+0.5);
        //            }
        //            Exercise exercise = new EuropeanExercise(fixingDates[cases4[l].fixings-1]);

        //            spot.setValue(cases4[l].underlying);
        //            qRate.setValue(cases4[l].dividendYield);
        //            rRate.setValue(cases4[l].riskFreeRate);
        //            vol.setValue(cases4[l].volatility);

        //            BlackScholesMertonProcess stochProcess =
        //                new BlackScholesMertonProcess(new Handle<Quote>(spot),
        //                                            new Handle<YieldTermStructure>(qTS),
        //                                            new Handle<YieldTermStructure>(rTS),
        //                                            new Handle<BlackVolTermStructure>(volTS));

        //            ulong seed=42;
        //            const int nrTrails = 5000;
        //            LowDiscrepancy.icInstance = new InverseCumulativeNormal();
        //            IRNG rsg = (IRNG)new LowDiscrepancy().make_sequence_generator(nrTrails,seed);

        //            new PseudoRandom().make_sequence_generator(nrTrails,seed);

        //            IPricingEngine engine =
        //                new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy, Statistics>(stochProcess)
        //                    .withStepsPerYear(1)
        //                    .withSamples(2047)
        //                    .withControlVariate()
        //                    .value();
        //            DiscreteAveragingAsianOption option=
        //                new DiscreteAveragingAsianOption(averageType, runningSum,
        //                                                pastFixings, fixingDates,
        //                                                payoff, exercise);
        //            option.setPricingEngine(engine);

        //            double calculated = option.NPV();
        //            double expected = cases4[l].result;
        //            double tolerance = 2.0e-2;
        //            if (System.Math.Abs(calculated-expected) > tolerance) {
        //                REPORT_FAILURE("value", averageType, runningSum, pastFixings,
        //                            fixingDates, payoff, exercise, spot.value(),
        //                            qRate.value(), rRate.value(), today,
        //                            vol.value(), expected, calculated, tolerance);
        //            }
        //        }
        //    }

        //    [TestMethod()]
        //    public void testMCDiscreteArithmeticAverageStrike() {

        //        //BOOST_MESSAGE("Testing Monte Carlo discrete arithmetic average-strike Asians...");

        //        //QL_TEST_START_TIMING

        //        // data from "Asian Option", Levy, 1997
        //        // in "Exotic Options: The State of the Art",
        //        // edited by Clewlow, Strickland
        //        DiscreteAverageData[] cases5 = {
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 2,
        //              0.13, true, 1.51917595129 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 4,
        //              0.13, true, 1.67940165674 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 8,
        //              0.13, true, 1.75371215251 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 12,
        //              0.13, true, 1.77595318693 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 26,
        //              0.13, true, 1.81430536630 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 52,
        //              0.13, true, 1.82269246898 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 100,
        //              0.13, true, 1.83822402464 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 250,
        //              0.13, true, 1.83875059026 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 500,
        //              0.13, true, 1.83750703638 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 1000,
        //              0.13, true, 1.83887181884 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 2,
        //              0.13, true, 1.51154400089 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 4,
        //              0.13, true, 1.67103508506 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 8,
        //              0.13, true, 1.74529684070 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 12,
        //              0.13, true, 1.76667074564 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 26,
        //              0.13, true, 1.80528400613 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 52,
        //              0.13, true, 1.81400883891 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 100,
        //              0.13, true, 1.82922901451 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 250,
        //              0.13, true, 1.82937111773 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 500,
        //              0.13, true, 1.82826193186 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 1000,
        //              0.13, true, 1.82967846654 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 2,
        //              0.13, true, 1.49648170891 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 4,
        //              0.13, true, 1.65443100462 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 8,
        //              0.13, true, 1.72817806731 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 12,
        //              0.13, true, 1.74877367895 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 26,
        //              0.13, true, 1.78733801988 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 52,
        //              0.13, true, 1.79624826757 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 100,
        //              0.13, true, 1.81114186876 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 250,
        //              0.13, true, 1.81101152587 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 500,
        //              0.13, true, 1.81002311939 ),
        //            new DiscreteAverageData(QLNet.Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 1000,
        //              0.13, true, 1.81145760308 )
        //        };

        //        DayCounter dc = new Actual360();
        //        Date today = Date.Today ;

        //        SimpleQuote spot = new SimpleQuote(100.0);
        //        SimpleQuote qRate = new SimpleQuote(0.03);
        //        YieldTermStructure qTS =Utilities.flatRate(today, qRate, dc);
        //        SimpleQuote rRate = new SimpleQuote(0.06);
        //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
        //        SimpleQuote vol = new SimpleQuote(0.20);
        //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

        //        Average.Type averageType = QLNet.Average.Type.Arithmetic;
        //        double runningSum = 0.0;
        //        int pastFixings = 0;
        //        for (int l=0; l<cases5.Length; l++) {

        //            StrikedTypePayoff payoff =
        //                new PlainVanillaPayoff(cases5[l].ExerciseType, cases5[l].strike);

        //            double dt = cases5[l].length/(cases5[l].fixings-1);
        //            List<double> timeIncrements = new InitializedList<double>(cases5[l].fixings);
        //            List<Date> fixingDates = new InitializedList<Date>(cases5[l].fixings);
        //            timeIncrements[0] = cases5[l].first;
        //            fixingDates[0] = today + (int)(timeIncrements[0]*360+0.5);
        //            for (int i=1; i<cases5[l].fixings; i++) {
        //                timeIncrements[i] = i*dt + cases5[l].first;
        //                fixingDates[i] = today + (int)(timeIncrements[i]*360+0.5);
        //            }
        //            Exercise exercise = new EuropeanExercise(fixingDates[cases5[l].fixings-1]);

        //            spot.setValue(cases5[l].underlying);
        //            qRate.setValue(cases5[l].dividendYield);
        //            rRate.setValue(cases5[l].riskFreeRate);
        //            vol.setValue(cases5[l].volatility);

        //            BlackScholesMertonProcess stochProcess =
        //                new BlackScholesMertonProcess(new Handle<Quote>(spot),
        //                                            new Handle<YieldTermStructure>(qTS),
        //                                            new Handle<YieldTermStructure>(rTS),
        //                                            new Handle<BlackVolTermStructure>(volTS));

        //            IPricingEngine engine =
        //                new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy,Statistics>(stochProcess)
        //                .withSeed(3456789)
        //                .withSamples(1023)
        //                .value() ;

        //            DiscreteAveragingAsianOption option =
        //                new DiscreteAveragingAsianOption(averageType, runningSum,
        //                                                pastFixings, fixingDates,
        //                                                payoff, exercise);
        //            option.setPricingEngine(engine);

        //            double calculated = option.NPV();
        //            double expected = cases5[l].result;
        //            double tolerance = 2.0e-2;
        //            if (System.Math.Abs(calculated-expected) > tolerance) {
        //                REPORT_FAILURE("value", averageType, runningSum, pastFixings,
        //                               fixingDates, payoff, exercise, spot.value(),
        //                               qRate.value(), rRate.value(), today,
        //                               vol.value(), expected, calculated, tolerance);
        //            }
        //        }
        //    }


        //    [TestMethod()]
        //    public void testPastFixings() {

        //        //BOOST_MESSAGE("Testing use of past fixings in Asian options...");
        //        DayCounter dc = new Actual360();
        //        Date today = Date.Today ;

        //        SimpleQuote spot = new SimpleQuote(100.0);
        //        SimpleQuote qRate = new SimpleQuote(0.03);
        //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
        //        SimpleQuote rRate = new SimpleQuote(0.06);
        //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
        //        SimpleQuote vol = new SimpleQuote(0.20);
        //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

        //        StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Put, 100.0);


        //        Exercise exercise = new EuropeanExercise(today + new Period(1,TimeUnit.Years));

        //        BlackScholesMertonProcess stochProcess =
        //            new BlackScholesMertonProcess(new Handle<Quote>(spot),
        //                                          new Handle<YieldTermStructure>(qTS),
        //                                          new Handle<YieldTermStructure>(rTS),
        //                                          new Handle<BlackVolTermStructure>(volTS));

        //        // MC arithmetic average-price
        //        double runningSum = 0.0;
        //        int pastFixings = 0;
        //        List<Date> fixingDates1 = new InitializedList<Date>();
        //        for (int i=0; i<=12; ++i)
        //            fixingDates1.Add(today + new Period(i,TimeUnit.Months));

        //        DiscreteAveragingAsianOption option1 =
        //            new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
        //                                             pastFixings, fixingDates1,
        //                                             payoff, exercise);

        //        pastFixings = 2;
        //        runningSum = pastFixings * spot.value() * 0.8;
        //        List<Date> fixingDates2 = new InitializedList<Date>();
        //        for (int i=-2; i<=12; ++i)
        //            fixingDates2.Add(today + new Period(i,TimeUnit.Months));

        //        DiscreteAveragingAsianOption option2 =
        //            new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
        //                                             pastFixings, fixingDates2,
        //                                             payoff, exercise);

        //        IPricingEngine engine =
        //           new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy,Statistics>(stochProcess)
        //            .withStepsPerYear(1)
        //            .withSamples(2047)
        //            .value() ;

        //        option1.setPricingEngine(engine);
        //        option2.setPricingEngine(engine);

        //        double price1 = option1.NPV();
        //        double price2 = option2.NPV();

        //        if (Utils.close(price1, price2)) {
        //            QAssert.Fail(
        //                 "past fixings had no effect on arithmetic average-price option"
        //                 + "\n  without fixings: " + price1
        //                 + "\n  with fixings:    " + price2);
        //        }

        //        // MC arithmetic average-strike
        //        engine = new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy,Statistics>(stochProcess)
        //            .withSamples(2047)
        //            .value();

        //        option1.setPricingEngine(engine);
        //        option2.setPricingEngine(engine);

        //        price1 = option1.NPV();
        //        price2 = option2.NPV();

        //        if (Utils.close(price1, price2)) {
        //            QAssert.Fail(
        //                 "past fixings had no effect on arithmetic average-strike option"
        //                 + "\n  without fixings: " + price1
        //                 + "\n  with fixings:    " + price2);
        //        }

        //        // analytic geometric average-price
        //        double runningProduct = 1.0;
        //        pastFixings = 0;

        //        DiscreteAveragingAsianOption option3 =
        //            new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
        //                                             pastFixings, fixingDates1,
        //                                             payoff, exercise);

        //        pastFixings = 2;
        //        runningProduct = spot.value() * spot.value();

        //        DiscreteAveragingAsianOption option4 =
        //            new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
        //                                             pastFixings, fixingDates2,
        //                                             payoff, exercise);

        //        engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

        //        option3.setPricingEngine(engine);
        //        option4.setPricingEngine(engine);

        //        double price3 = option3.NPV();
        //        double price4 = option4.NPV();

        //        if (Utils.close(price3, price4)) {
        //            QAssert.Fail(
        //                 "past fixings had no effect on geometric average-price option"
        //                 + "\n  without fixings: " + price3
        //                 + "\n  with fixings:    " + price4);
        //        }

        //        // MC geometric average-price
        //        engine = new MakeMCDiscreteGeometricAPEngine<LowDiscrepancy,Statistics>(stochProcess)
        //                    .withStepsPerYear(1)
        //                    .withSamples(2047)
        //                    .value();

        //        option3.setPricingEngine(engine);
        //        option4.setPricingEngine(engine);

        //        price3 = option3.NPV();
        //        price4 = option4.NPV();

        //        if (Utils.close(price3, price4)) {
        //            QAssert.Fail(
        //                 "past fixings had no effect on geometric average-price option"
        //                 + "\n  without fixings: " + price3
        //                 + "\n  with fixings:    " + price4);
        //        }
        //    }

        //    public void suite() {
        //    //BOOST_TEST_SUITE("Asian option tests");
        //        testAnalyticContinuousGeometricAveragePrice();
        //        testAnalyticContinuousGeometricAveragePriceGreeks();
        //        testAnalyticDiscreteGeometricAveragePrice();
        //        testMCDiscreteGeometricAveragePrice();
        //        testMCDiscreteArithmeticAveragePrice();
        //        testMCDiscreteArithmeticAverageStrike();
        //        testAnalyticDiscreteGeometricAveragePriceGreeks();
        //        testPastFixings();

        //    }
    }
}
