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
using QLNet.Instruments;
using QLNet.PricingEngines.Forward;
using QLNet.Methods.lattices;
using QLNet.Time;
using QLNet.PricingEngines.vanilla;
using QLNet.Processes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_ForwardOption
    {
        void REPORT_FAILURE(string greekName,
                            StrikedTypePayoff payoff,
                            Exercise exercise,
                            double s,
                            double q,
                            double r,
                            Date today,
                            double v,
                            double moneyness,
                            Date reset,
                            double expected,
                            double calculated,
                            double error,
                            double tolerance)
        {
            QAssert.Fail("Forward " + exercise + " "
                         + payoff.optionType() + " option with "
                         + payoff + " payoff:\n"
                         + "    spot value:       " + s + "\n"
                         + "    strike:           " + payoff.strike() + "\n"
                         + "    moneyness:        " + moneyness + "\n"
                         + "    dividend yield:   " + q + "\n"
                         + "    risk-free rate:   " + r + "\n"
                         + "    reference date:   " + today + "\n"
                         + "    reset date:       " + reset + "\n"
                         + "    maturity:         " + exercise.lastDate() + "\n"
                         + "    volatility:       " + v + "\n\n"
                         + "    expected " + greekName + ":   " + expected + "\n"
                         + "    calculated " + greekName + ": " + calculated + "\n"
                         + "    error:            " + error + "\n"
                         + "    tolerance:        " + tolerance);
        }


        [JetBrains.Annotations.PublicAPI] public class ForwardOptionData
        {
            public ForwardOptionData(Option.Type type_, double moneyness_, double s_, double q_, double r_, double start_,
                                     double t_, double v_, double result_, double tol_)
            {
                type = type_;
                moneyness = moneyness_;
                s = s_;
                q = q_;
                r = r_;
                start = start_;
                t = t_;
                v = v_;
                result = result_;
                tol = tol_;
            }

            public QLNet.Option.Type type;
            public double moneyness;
            public double s;          // spot
            public double q;          // dividend
            public double r;          // risk-free rate
            public double start;      // time to reset
            public double t;          // time to maturity
            public double v;          // volatility
            public double result;     // expected result
            public double tol;        // tolerance
        }

        [Fact]
        public void testValues()
        {
            // Testing forward option values...

            /* The data below are from
               "Option pricing formulas", E.G. Haug, McGraw-Hill 1998
            */
            ForwardOptionData[] values =
            {
            //  ExerciseType, moneyness, spot,  div, rate,start,   t,  vol, result, tol
            // "Option pricing formulas", pag. 37
            new ForwardOptionData(QLNet.Option.Type.Call, 1.1, 60.0, 0.04, 0.08, 0.25, 1.0, 0.30, 4.4064, 1.0e-4),
            // "Option pricing formulas", VBA code
            new ForwardOptionData(QLNet.Option.Type.Put, 1.1, 60.0, 0.04, 0.08, 0.25, 1.0, 0.30, 8.2971, 1.0e-4)
         };

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(0.0);
            var qRate = new SimpleQuote(0.0);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
            var rRate = new SimpleQuote(0.0);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
            var vol = new SimpleQuote(0.0);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS), new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new ForwardVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));  // AnalyticEuropeanEngine

            for (var i = 0; i < values.Length; i++)
            {

                StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, 0.0);
                var exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
                Exercise exercise = new EuropeanExercise(exDate);
                var reset = today + Convert.ToInt32(values[i].start * 360 + 0.5);

                spot.setValue(values[i].s);
                qRate.setValue(values[i].q);
                rRate.setValue(values[i].r);
                vol.setValue(values[i].v);

                var option = new ForwardVanillaOption(values[i].moneyness, reset, payoff, exercise);
                option.setPricingEngine(engine);

                var calculated = option.NPV();
                var error = System.Math.Abs(calculated - values[i].result);
                var tolerance = 1e-4;
                if (error > tolerance)
                {
                    REPORT_FAILURE("value", payoff, exercise, values[i].s,
                                   values[i].q, values[i].r, today,
                                   values[i].v, values[i].moneyness, reset,
                                   values[i].result, calculated,
                                   error, tolerance);
                }
            }
        }

        [Fact]
        public void testPerformanceValues()
        {
            // Testing forward performance option values...

            /* The data below are the performance equivalent of the
               forward options tested above and taken from
               "Option pricing formulas", E.G. Haug, McGraw-Hill 1998
            */
            ForwardOptionData[] values =
            {
            //  ExerciseType, moneyness, spot,  div, rate,start, maturity,  vol,                       result, tol
            new ForwardOptionData(QLNet.Option.Type.Call, 1.1, 60.0, 0.04, 0.08, 0.25,      1.0, 0.30, 4.4064 / 60 * System.Math.Exp(-0.04 * 0.25), 1.0e-4),
            new ForwardOptionData(QLNet.Option.Type.Put, 1.1, 60.0, 0.04, 0.08, 0.25,      1.0, 0.30, 8.2971 / 60 * System.Math.Exp(-0.04 * 0.25), 1.0e-4)
         };

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(0.0);
            var qRate = new SimpleQuote(0.0);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, qRate, dc));
            var rRate = new SimpleQuote(0.0);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(today, rRate, dc));
            var vol = new SimpleQuote(0.0);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(today, vol, dc));

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
                                                                                   new Handle<YieldTermStructure>(qTS), new Handle<YieldTermStructure>(rTS),
                                                                                   new Handle<BlackVolTermStructure>(volTS));

            IPricingEngine engine = new ForwardPerformanceVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));     // AnalyticEuropeanEngine

            for (var i = 0; i < values.Length; i++)
            {
                StrikedTypePayoff payoff = new PlainVanillaPayoff(values[i].type, 0.0);
                var exDate = today + Convert.ToInt32(values[i].t * 360 + 0.5);
                Exercise exercise = new EuropeanExercise(exDate);
                var reset = today + Convert.ToInt32(values[i].start * 360 + 0.5);

                spot.setValue(values[i].s);
                qRate.setValue(values[i].q);
                rRate.setValue(values[i].r);
                vol.setValue(values[i].v);

                var option = new ForwardVanillaOption(values[i].moneyness, reset, payoff, exercise);
                option.setPricingEngine(engine);

                var calculated = option.NPV();
                var error = System.Math.Abs(calculated - values[i].result);
                var tolerance = 1e-4;
                if (error > tolerance)
                {
                    REPORT_FAILURE("value", payoff, exercise, values[i].s,
                                   values[i].q, values[i].r, today,
                                   values[i].v, values[i].moneyness, reset,
                                   values[i].result, calculated,
                                   error, tolerance);
                }
            }
        }

        private void testForwardGreeks(Type engine_type)
        {
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
            int[] startMonths = { 6, 9 };
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

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

            IPricingEngine engine = engine_type == typeof(ForwardVanillaEngine) ? new ForwardVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process)) :
                                    new ForwardPerformanceVanillaEngine(stochProcess, process => new AnalyticEuropeanEngine(process));

            for (var i = 0; i < types.Length; i++)
            {
                for (var j = 0; j < moneyness.Length; j++)
                {
                    for (var k = 0; k < lengths.Length; k++)
                    {
                        for (var h = 0; h < startMonths.Length; h++)
                        {

                            var exDate = today + new Period(lengths[k], TimeUnit.Years);
                            Exercise exercise = new EuropeanExercise(exDate);

                            var reset = today + new Period(startMonths[h], TimeUnit.Months);

                            StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], 0.0);

                            var option = new ForwardVanillaOption(moneyness[j], reset, payoff, exercise);
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
                                                //std::map<std::string,double>::iterator it;
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
                                                                       moneyness[j], reset,
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
        public void testGreeks()
        {
            // Testing forward option greeks
            var backup = new SavedSettings();

            testForwardGreeks(typeof(ForwardVanillaEngine));
        }

        [Fact]
        public void testPerformanceGreeks()
        {
            // Testing forward performance option greeks
            var backup = new SavedSettings();

            testForwardGreeks(typeof(ForwardPerformanceVanillaEngine));
        }

        class TestBinomialEngine : BinomialVanillaEngine<CoxRossRubinstein>
        {
            public TestBinomialEngine(GeneralizedBlackScholesProcess process) :
               base(process, 300) // fixed steps
            { }
        }

        // verify than if engine
        [Fact]
        public void testGreeksInitialization()
        {
            // Testing forward option greeks initialization
            DayCounter dc = new Actual360();
            var backup = new SavedSettings();
            var today = Date.Today;
            Settings.setEvaluationDate(today);

            var spot = new SimpleQuote(100.0);
            var qRate = new SimpleQuote(0.04);
            var qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
            var rRate = new SimpleQuote(0.01);
            var rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
            var vol = new SimpleQuote(0.11);
            var volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

            var stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

            IPricingEngine engine = new ForwardVanillaEngine(stochProcess, process => new TestBinomialEngine(process));
            var exDate = today + new Period(1, TimeUnit.Years);
            Exercise exercise = new EuropeanExercise(exDate);
            var reset = today + new Period(6, TimeUnit.Months);
            StrikedTypePayoff payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, 0.0);

            var option = new ForwardVanillaOption(0.9, reset, payoff, exercise);
            option.setPricingEngine(engine);

            IPricingEngine ctrlengine = new TestBinomialEngine(stochProcess);
            var ctrloption = new VanillaOption(payoff, exercise);
            ctrloption.setPricingEngine(ctrlengine);

            double? delta = 0;
            try
            {
                delta = ctrloption.delta();
            }
            catch (Exception)
            {
                // if normal option can't calculate delta,
                // nor should forward
                try
                {
                    delta = option.delta();
                }
                catch (Exception)
                {
                    delta = null;
                }
                QLNet.Utils.QL_REQUIRE(delta == null, () => "Forward delta invalid");
            }

            double? rho = 0;
            try
            {
                rho = ctrloption.rho();
            }
            catch (Exception)
            {
                // if normal option can't calculate rho,
                // nor should forward
                try
                {
                    rho = option.rho();
                }
                catch (Exception)
                {
                    rho = null;
                }
                QLNet.Utils.QL_REQUIRE(rho == null, () => "Forward rho invalid");
            }

            double? divRho = 0;
            try
            {
                divRho = ctrloption.dividendRho();
            }
            catch (Exception)
            {
                // if normal option can't calculate divRho,
                // nor should forward
                try
                {
                    divRho = option.dividendRho();
                }
                catch (Exception)
                {
                    divRho = null;
                }
                QLNet.Utils.QL_REQUIRE(divRho == null, () => "Forward dividendRho invalid");
            }

            double? vega = 0;
            try
            {
                vega = ctrloption.vega();
            }
            catch (Exception)
            {
                // if normal option can't calculate vega,
                // nor should forward
                try
                {
                    vega = option.vega();
                }
                catch (Exception)
                {
                    vega = null;
                }
                QLNet.Utils.QL_REQUIRE(vega == null, () => "Forward vega invalid");
            }
        }
    }
}
