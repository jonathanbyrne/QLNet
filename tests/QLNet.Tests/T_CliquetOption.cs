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

using Xunit;
using System;
using System.Collections.Generic;
using QLNet.Instruments;
using QLNet.Pricingengines.Cliquet;
using QLNet.Pricingengines.Forward;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.processes;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_CliquetOption
    {
        private void REPORT_FAILURE(string greekName,
                                    StrikedTypePayoff payoff,
                                    Exercise exercise,
                                    double s,
                                    double q,
                                    double r,
                                    Date today,
                                    double v,
                                    double expected,
                                    double calculated,
                                    double error,
                                    double tolerance)
        {
            QAssert.Fail(payoff.optionType() + " option :\n"
                         + "    spot value: " + s + "\n"
                         + "    moneyness:        " + payoff.strike() + "\n"
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

        [Fact]
        public void testValues()
        {
            // Testing Cliquet option values

            var today = Date.Today;
            DayCounter dc = new Actual360();

            var spot = new SimpleQuote(60.0);
            var qRate = new SimpleQuote(0.04);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.08);
            var rTS = Utilities.flatRate(today, rRate, dc);
            var vol = new SimpleQuote(0.30);
            var volTS = Utilities.flatVol(today, vol, dc);

            var process = new BlackScholesMertonProcess(
               new Handle<Quote>(spot),
               new Handle<YieldTermStructure>(qTS),
               new Handle<YieldTermStructure>(rTS),
               new Handle<BlackVolTermStructure>(volTS));
            IPricingEngine engine = new AnalyticCliquetEngine(process);

            var reset = new List<Date>();
            reset.Add(today + 90);
            var maturity = today + 360;
            var type = QLNet.Option.Type.Call;
            var moneyness = 1.1;

            var payoff = new PercentageStrikePayoff(type, moneyness);
            var exercise = new EuropeanExercise(maturity);

            var option = new CliquetOption(payoff, exercise, reset);
            option.setPricingEngine(engine);

            var calculated = option.NPV();
            var expected = 4.4064; // Haug, p.37
            var error = System.Math.Abs(calculated - expected);
            var tolerance = 1e-4;
            if (error > tolerance)
            {
                REPORT_FAILURE("value", payoff, exercise, spot.value(),
                               qRate.value(), rRate.value(), today,
                               vol.value(), expected, calculated,
                               error, tolerance);
            }
        }

        [Fact]
        public void testGreeks()
        {
            // Testing Cliquet option greek
            testOptionGreeks(process => new AnalyticCliquetEngine(process));
        }

        [Fact]
        public void testPerformanceGreeks()
        {
            // Testing Performance option greek
            testOptionGreeks(process => new AnalyticPerformanceEngine(process));
        }

        private void testOptionGreeks(ForwardVanillaEngine.GetOriginalEngine getEngine)
        {

            var backup = new SavedSettings();

            Dictionary<string, double> calculated = new Dictionary<string, double>(),
            expected = new Dictionary<string, double>(),
            tolerance = new Dictionary<string, double>();
            tolerance["delta"] = 1.0e-5;
            tolerance["gamma"] = 1.0e-5;
            tolerance["theta"] = 1.0e-5;
            tolerance["rho"] = 1.0e-5;
            tolerance["divRho"] = 1.0e-5;
            tolerance["vega"] = 1.0e-5;

            Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
            double[] moneyness = { 0.9, 1.0, 1.1 };
            double[] underlyings = { 100.0 };
            double[] qRates = { 0.04, 0.05, 0.06 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            int[] lengths = { 1, 2 };
            Frequency[] frequencies = { Frequency.Semiannual, Frequency.Quarterly, };
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
                for (var j = 0; j < moneyness.Length; j++)
                {
                    for (var k = 0; k < lengths.Length; k++)
                    {
                        for (var kk = 0; kk < frequencies.Length; kk++)
                        {
                            var maturity = new EuropeanExercise(today + new Period(lengths[k], TimeUnit.Years));

                            var payoff = new PercentageStrikePayoff(types[i], moneyness[j]);

                            var reset = new List<Date>();
                            for (var d = today + new Period(frequencies[kk]);
                                 d < maturity.lastDate();
                                 d += new Period(frequencies[kk]))
                                reset.Add(d);

                            var engine = getEngine(process);

                            var option = new CliquetOption(payoff, maturity, reset);
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
                                                foreach (var it in calculated)
                                                {
                                                    var greek = it.Key;
                                                    double expct = expected[greek],
                                                           calcl = calculated[greek],
                                                           tol = tolerance[greek];
                                                    var error = Utilities.relativeError(expct, calcl, u);
                                                    if (error > tol)
                                                    {
                                                        REPORT_FAILURE(greek, payoff, maturity,
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
    }
}
