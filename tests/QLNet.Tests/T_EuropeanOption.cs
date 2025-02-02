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
using Xunit;
using QLNet.Instruments;
using QLNet.Methods.lattices;
using QLNet.PricingEngines.vanilla;
using QLNet.Processes;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_EuropeanOption : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;

        public T_EuropeanOption()
        {
            backup = new SavedSettings();
        }

        public void Dispose()
        {
            backup.Dispose();
        }
        #endregion

        enum EngineType
        {
            Analytic,
            JR, CRR, EQP, TGEO, TIAN, LR, JOSHI,
            FiniteDifferences,
            Integral,
            PseudoMonteCarlo, QuasiMonteCarlo
        }


        [Fact]
        public void testJRBinomialEngines()
        {
            // Testing JR binomial European engines against analytic results
            var engine = EngineType.JR;
            var steps = 251;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 0.002);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testCRRBinomialEngines()
        {
            // Testing CRR binomial European engines against analytic results
            var engine = EngineType.CRR;
            var steps = 501;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 0.02);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testEQPBinomialEngines()
        {
            // Testing EQP binomial European engines against analytic results
            var engine = EngineType.EQP;
            var steps = 501;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 0.02);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testTGEOBinomialEngines()
        {
            // Testing TGEO binomial European engines " against analytic results
            var engine = EngineType.TGEO;
            var steps = 251;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 0.002);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testTIANBinomialEngines()
        {
            // Testing TIAN binomial European engines against analytic results
            var engine = EngineType.TIAN;
            var steps = 251;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 0.002);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testLRBinomialEngines()
        {
            // Testing LR binomial European engines against analytic results
            var engine = EngineType.LR;
            var steps = 251;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 1.0e-6);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }
        [Fact]
        public void testJOSHIBinomialEngines()
        {
            // Testing Joshi binomial European engines against analytic results
            var engine = EngineType.JOSHI;
            var steps = 251;
            var samples = 0;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 1.0e-7);
            relativeTol.Add("delta", 1.0e-3);
            relativeTol.Add("gamma", 1.0e-4);
            relativeTol.Add("theta", 0.03);
            testEngineConsistency(engine, steps, samples, relativeTol, true);
        }


        [Fact]
        public void testFdEngines()
        {

            //("Testing finite-difference European engines against analytic results...");

            //SavedSettings backup;

            var engine = EngineType.FiniteDifferences;
            var timeSteps = 300;
            var gridPoints = 300;
            var relativeTol = new Dictionary<string, double>();
            relativeTol.Add("value", 1.0e-4);
            relativeTol.Add("delta", 1.0e-6);
            relativeTol.Add("gamma", 1.0e-6);
            relativeTol.Add("theta", 1.0e-4);
            testEngineConsistency(engine, timeSteps, gridPoints, relativeTol, true);
        }


        GeneralizedBlackScholesProcess makeProcess(Quote u, YieldTermStructure q, YieldTermStructure r, BlackVolTermStructure vol) =>
            new BlackScholesMertonProcess(new Handle<Quote>(u), new Handle<YieldTermStructure>(q),
                new Handle<YieldTermStructure>(r), new Handle<BlackVolTermStructure>(vol));

        VanillaOption makeOption(StrikedTypePayoff payoff, Exercise exercise, Quote u, YieldTermStructure q,
                                 YieldTermStructure r, BlackVolTermStructure vol, EngineType engineType, int binomialSteps, int samples)
        {

            var stochProcess = makeProcess(u, q, r, vol);

            IPricingEngine engine;
            switch (engineType)
            {
                case EngineType.Analytic:
                    engine = new AnalyticEuropeanEngine(stochProcess);
                    break;
                case EngineType.JR:
                    engine = new BinomialVanillaEngine<JarrowRudd>(stochProcess, binomialSteps);
                    break;
                case EngineType.CRR:
                    engine = new BinomialVanillaEngine<CoxRossRubinstein>(stochProcess, binomialSteps);
                    break;
                case EngineType.EQP:
                    engine = new BinomialVanillaEngine<AdditiveEQPBinomialTree>(stochProcess, binomialSteps);
                    break;
                case EngineType.TGEO:
                    engine = new BinomialVanillaEngine<Trigeorgis>(stochProcess, binomialSteps);
                    break;
                case EngineType.TIAN:
                    engine = new BinomialVanillaEngine<Tian>(stochProcess, binomialSteps);
                    break;
                case EngineType.LR:
                    engine = new BinomialVanillaEngine<LeisenReimer>(stochProcess, binomialSteps);
                    break;
                case EngineType.JOSHI:
                    engine = new BinomialVanillaEngine<Joshi4>(stochProcess, binomialSteps);
                    break;
                case EngineType.FiniteDifferences:
                    engine = new FDEuropeanEngine(stochProcess, binomialSteps, samples);
                    break;
                case EngineType.Integral:
                    engine = new IntegralEngine(stochProcess);
                    break;
                //case EngineType.PseudoMonteCarlo:
                //  engine = MakeMCEuropeanEngine<PseudoRandom>(stochProcess)
                //      .withSteps(1)
                //      .withSamples(samples)
                //      .withSeed(42);
                //  break;
                //case EngineType.QuasiMonteCarlo:
                //  engine = MakeMCEuropeanEngine<LowDiscrepancy>(stochProcess)
                //      .withSteps(1)
                //      .withSamples(samples);
                //  break;
                default:
                    throw new ArgumentException("unknown engine ExerciseType");
            }

            VanillaOption option = new EuropeanOption(payoff, exercise);
            option.setPricingEngine(engine);
            return option;
        }


        void testEngineConsistency(EngineType engine, int binomialSteps, int samples, Dictionary<string, double> tolerance,
                                   bool testGreeks)
        {

            //QL_TEST_START_TIMING

            Dictionary<string, double> calculated = new Dictionary<string, double>(), expected = new Dictionary<string, double>();

            // test options
            Option.Type[] types = { QLNet.Option.Type.Call, QLNet.Option.Type.Put };
            double[] strikes = { 75.0, 100.0, 125.0 };
            int[] lengths = { 1 };

            // test data
            double[] underlyings = { 100.0 };
            double[] qRates = { 0.00, 0.05 };
            double[] rRates = { 0.01, 0.05, 0.15 };
            double[] vols = { 0.11, 0.50, 1.20 };

            DayCounter dc = new Actual360();
            var today = Date.Today;

            var spot = new SimpleQuote(0.0);
            var vol = new SimpleQuote(0.0);
            var volTS = Utilities.flatVol(today, vol, dc);
            var qRate = new SimpleQuote(0.0);
            var qTS = Utilities.flatRate(today, qRate, dc);
            var rRate = new SimpleQuote(0.0);
            var rTS = Utilities.flatRate(today, rRate, dc);

            for (var i = 0; i < types.Length; i++)
            {
                for (var j = 0; j < strikes.Length; j++)
                {
                    for (var k = 0; k < lengths.Length; k++)
                    {
                        var exDate = today + lengths[k] * 360;
                        Exercise exercise = new EuropeanExercise(exDate);
                        StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);
                        // reference option
                        var refOption = makeOption(payoff, exercise, spot, qTS, rTS, volTS,
                                                             EngineType.Analytic, 0, 0);
                        // option to check
                        var option = makeOption(payoff, exercise, spot, qTS, rTS, volTS,
                                                          engine, binomialSteps, samples);

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

                                        expected.Clear();
                                        calculated.Clear();

                                        // FLOATING_POINT_EXCEPTION
                                        expected.Add("value", refOption.NPV());
                                        calculated.Add("value", option.NPV());

                                        if (testGreeks && option.NPV() > spot.value() * 1.0e-5)
                                        {
                                            expected.Add("delta", refOption.delta());
                                            expected.Add("gamma", refOption.gamma());
                                            expected.Add("theta", refOption.theta());
                                            calculated.Add("delta", option.delta());
                                            calculated.Add("gamma", option.gamma());
                                            calculated.Add("theta", option.theta());
                                        }
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



        void REPORT_FAILURE(string greekName, StrikedTypePayoff payoff, Exercise exercise, double s, double q, double r,
                            Date today, double v, double expected, double calculated, double error, double tolerance)
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
    }
}
