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
using Xunit;
using QLNet.PricingEngines;
using QLNet.Instruments;
using QLNet.PricingEngines.vanilla;
using QLNet.Processes;
using QLNet.Quotes;
using QLNet.Time;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_BlackDeltaCalculator
    {
        private int timeToDays(double t) =>
            // FLOATING_POINT_EXCEPTION
            (int)(t * 360 + 0.5);

        private struct DeltaData
        {
            public QLNet.Option.Type ot;
            public DeltaVolQuote.DeltaType dt;
            public double spot;
            public double dDf;   // domestic discount
            public double fDf;   // foreign  discount
            public double stdDev;
            public double strike;
            public double value;

            public DeltaData(Option.Type ot, DeltaVolQuote.DeltaType dt, double spot, double dDf, double fDf,
                             double stdDev, double strike, double value) : this()
            {
                this.ot = ot;
                this.dt = dt;
                this.spot = spot;
                this.dDf = dDf;
                this.fDf = fDf;
                this.stdDev = stdDev;
                this.strike = strike;
                this.value = value;
            }
        }

        private struct EuropeanOptionData
        {
            public QLNet.Option.Type type;
            public double strike;
            public double s;        // spot
            public double q;        // dividend
            public double r;        // risk-free rate
            public double t;        // time to maturity
            public double v;  // volatility
            public double result;   // expected result
            public double tol;      // tolerance

            public EuropeanOptionData(Option.Type type, double strike, double s, double q, double r, double t,
                                      double v, double result, double tol) : this()
            {
                this.type = type;
                this.strike = strike;
                this.s = s;
                this.q = q;
                this.r = r;
                this.t = t;
                this.v = v;
                this.result = result;
                this.tol = tol;
            }
        }

        [Fact]
        public void testDeltaValues()
        {
            // Testing delta calculator values

            DeltaData[] values =
            {
            // Values taken from parallel implementation in R
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Spot,     1.421, 0.997306, 0.992266,  0.1180654,  1.608080, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaSpot,   1.421, 0.997306, 0.992266,  0.1180654,  1.600545, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Fwd,      1.421, 0.997306, 0.992266,  0.1180654,  1.609029, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaFwd,    1.421, 0.997306, 0.992266,  0.1180654,  1.601550, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Spot,     122.121,  0.9695434, 0.9872347,  0.0887676,  119.8031, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaSpot,   122.121,  0.9695434, 0.9872347,  0.0887676,  117.7096, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Fwd,      122.121,  0.9695434, 0.9872347,  0.0887676,  120.0592, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaFwd,    122.121,  0.9695434, 0.9872347,  0.0887676,  118.0532, 0.67),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Spot,     3.4582,   0.99979, 0.9250616,   0.3199034,  4.964924, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaSpot,   3.4582,   0.99979, 0.9250616,   0.3199034,  3.778327, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Fwd,      3.4582,   0.99979, 0.9250616,   0.3199034,  4.51896, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaFwd,    3.4582,   0.99979, 0.9250616,   0.3199034,  3.65728, -0.8219),
            // JPYUSD Data taken from Castagnas "FX Options and Smile Risk" (Wiley 2009)
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Spot,     103.00,   0.99482, 0.98508,     0.07247845, 97.47,  -0.25),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaSpot,   103.00,   0.99482, 0.98508,     0.07247845, 97.22,  -0.25)
         };

            QLNet.Option.Type currOt;
            DeltaVolQuote.DeltaType currDt;
            double currSpot;
            double currdDf;
            double currfDf;
            double currStdDev;
            double currStrike;
            double expected;
            double currDelta;
            double calculated;
            double error;
            double tolerance;

            for (var i = 0; i < values.Length; i++)
            {
                currOt = values[i].ot;
                currDt = values[i].dt;
                currSpot = values[i].spot;
                currdDf = values[i].dDf;
                currfDf = values[i].fDf;
                currStdDev = values[i].stdDev;
                currStrike = values[i].strike;
                currDelta = values[i].value;

                var myCalc = new BlackDeltaCalculator(currOt, currDt, currSpot, currdDf, currfDf, currStdDev);

                tolerance = 1.0e-3;

                expected = currDelta;
                calculated = myCalc.deltaFromStrike(currStrike);
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Delta-from-strike calculation failed for delta. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Strike:" + calculated + "\n"
                                 + "Expected   Strike:" + expected + "\n"
                                 + "Error: " + error);
                }

                tolerance = 1.0e-2;
                // tolerance not that small, but sufficient for strikes in
                // particular since they might be results of a numerical
                // procedure

                expected = currStrike;
                calculated = myCalc.strikeFromDelta(currDelta);
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Strike-from-delta calculation failed for delta. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Strike:" + calculated + "\n"
                                 + "Expected   Strike:" + expected + "\n"
                                 + "Error: " + error);
                }
            }
        }

        [Fact]
        public void testDeltaPriceConsistency()
        {
            // Testing premium-adjusted delta price consistency

            // This function tests for price consistencies with the standard
            // Black Scholes calculator, since premium adjusted deltas can be calculated
            // from spot deltas by adding/subtracting the premium.

            var backup = new SavedSettings();

            // actually, value and tol won't be needed for testing
            EuropeanOptionData[] values =
            {
            //        ExerciseType, strike,   spot,    rd,    rf,    t,  vol,   value,    tol
            new EuropeanOptionData(QLNet.Option.Type.Call,  0.9123,  1.2212, 0.0231, 0.0000, 0.25, 0.301,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  0.9234,  1.2212, 0.0231, 0.0000, 0.35, 0.111,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  0.9783,  1.2212, 0.0231, 0.0000, 0.45, 0.071,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.0000,  1.2212, 0.0231, 0.0000, 0.55, 0.082,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.1230,  1.2212, 0.0231, 0.0000, 0.65, 0.012,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.2212,  1.2212, 0.0231, 0.0000, 0.75, 0.129,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.3212,  1.2212, 0.0231, 0.0000, 0.85, 0.034,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.3923,  1.2212, 0.0131, 0.2344, 0.95, 0.001,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Call,  1.3455,  1.2212, 0.0000, 0.0000, 1.00, 0.127,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   0.9123,  1.2212, 0.0231, 0.0000, 0.25, 0.301,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   0.9234,  1.2212, 0.0231, 0.0000, 0.35, 0.111,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   0.9783,  1.2212, 0.0231, 0.0000, 0.45, 0.071,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.0000,  1.2212, 0.0231, 0.0000, 0.55, 0.082,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.1230,  1.2212, 0.0231, 0.0000, 0.65, 0.012,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.2212,  1.2212, 0.0231, 0.0000, 0.75, 0.129,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.3212,  1.2212, 0.0231, 0.0000, 0.85, 0.034,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.3923,  1.2212, 0.0131, 0.2344, 0.95, 0.001,  0.0, 0.0),
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.3455,  1.2212, 0.0000, 0.0000, 1.00, 0.127,  0.0, 0.0),
            // extreme case: zero vol
            new EuropeanOptionData(QLNet.Option.Type.Put,   1.3455,  1.2212, 0.0000, 0.0000, 0.50, 0.000,  0.0, 0.0),
            // extreme case: zero strike
            new EuropeanOptionData(QLNet.Option.Type.Put,   0.0000,  1.2212, 0.0000, 0.0000, 1.50, 0.133,  0.0, 0.0),
            // extreme case: zero strike+zero vol
            new EuropeanOptionData(QLNet.Option.Type.Put,   0.0000,  1.2212, 0.0000, 0.0000, 1.00, 0.133,  0.0, 0.0),
         };

            DayCounter dc = new Actual360();
            Calendar calendar = new TARGET();
            var today = Date.Today;

            // Start setup of market data

            var discFor = 0.0;
            var discDom = 0.0;
            var implVol = 0.0;
            var expectedVal = 0.0;
            var calculatedVal = 0.0;
            var error = 0.0;

            var spotQuote = new SimpleQuote(0.0);
            var spotHandle = new Handle<Quote>(spotQuote);

            var qQuote = new SimpleQuote(0.0);
            var qHandle = new Handle<Quote>(qQuote);
            YieldTermStructure qTS = new FlatForward(today, qHandle, dc);

            var rQuote = new SimpleQuote(0.0);
            var rHandle = new Handle<Quote>(qQuote);
            YieldTermStructure rTS = new FlatForward(today, rHandle, dc);

            var volQuote = new SimpleQuote(0.0);
            var volHandle = new Handle<Quote>(volQuote);
            BlackVolTermStructure volTS = new BlackConstantVol(today, calendar, volHandle, dc);

            BlackScholesMertonProcess stochProcess;
            IPricingEngine engine;
            StrikedTypePayoff payoff;
            Date exDate;
            Exercise exercise;
            // Setup of market data finished

            var tolerance = 1.0e-10;

            for (var i = 0; i < values.Length; ++i)
            {

                payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);
                exDate = today + timeToDays(values[i].t);
                exercise = new EuropeanExercise(exDate);

                spotQuote.setValue(values[i].s);
                volQuote.setValue(values[i].v);
                rQuote.setValue(values[i].r);
                qQuote.setValue(values[i].q);

                discDom = rTS.discount(exDate);
                discFor = qTS.discount(exDate);
                implVol = System.Math.Sqrt(volTS.blackVariance(exDate, 0.0));

                var myCalc = new BlackDeltaCalculator(values[i].type, DeltaVolQuote.DeltaType.PaSpot,
                                                                       spotQuote.value(), discDom, discFor, implVol);

                stochProcess = new BlackScholesMertonProcess(spotHandle,
                                                             new Handle<YieldTermStructure>(qTS),
                                                             new Handle<YieldTermStructure>(rTS),
                                                             new Handle<BlackVolTermStructure>(volTS));

                engine = new AnalyticEuropeanEngine(stochProcess);

                var option = new EuropeanOption(payoff, exercise);
                option.setPricingEngine(engine);

                calculatedVal = myCalc.deltaFromStrike(values[i].strike);
                expectedVal = option.delta() - option.NPV() / spotQuote.value();
                error = System.Math.Abs(expectedVal - calculatedVal);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Premium-adjusted spot delta test failed. \n"
                                 + "Calculated Delta: " + calculatedVal + "\n"
                                 + "Expected Value:   " + expectedVal + "\n"
                                 + "Error: " + error);
                }

                myCalc.setDeltaType(DeltaVolQuote.DeltaType.PaFwd);

                calculatedVal = myCalc.deltaFromStrike(values[i].strike);
                expectedVal = expectedVal / discFor; // Premium adjusted Fwd Delta is PA spot without discount
                error = System.Math.Abs(expectedVal - calculatedVal);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Premium-adjusted forward delta test failed. \n"
                                 + "Calculated Delta: " + calculatedVal + "\n"
                                 + "Expected Value:   " + expectedVal + "\n"
                                 + "Error: " + error);
                }


                // Test consistency with BlackScholes Calculator for Spot Delta
                myCalc.setDeltaType(DeltaVolQuote.DeltaType.Spot);

                calculatedVal = myCalc.deltaFromStrike(values[i].strike);
                expectedVal = option.delta();
                error = System.Math.Abs(calculatedVal - expectedVal);

                if (error > tolerance)
                {
                    QAssert.Fail("\n spot delta in BlackDeltaCalculator differs from delta in BlackScholesCalculator. \n"
                                 + "Calculated Value: " + calculatedVal + "\n"
                                 + "Expected Value:   " + expectedVal + "\n"
                                 + "Error: " + error);
                }
            }
        }

        [Fact]
        public void testPutCallParity()
        {
            // Testing put-call parity for deltas

            // Test for put call parity between put and call deltas.

            var backup = new SavedSettings();

            /* The data below are from
               "Option pricing formulas", E.G. Haug, McGraw-Hill 1998
               pag 11-16
            */

            EuropeanOptionData[] values =
            {
            // pag 2-8
            //        ExerciseType, strike,   spot,    q,    r,    t,  vol,   value,    tol
            new EuropeanOptionData(QLNet.Option.Type.Call,  65.00,  60.00, 0.00, 0.08, 0.25, 0.30,  2.1334, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,   95.00, 100.00, 0.05, 0.10, 0.50, 0.20,  2.4648, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,   19.00,  19.00, 0.10, 0.10, 0.75, 0.28,  1.7011, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call,  19.00,  19.00, 0.10, 0.10, 0.75, 0.28,  1.7011, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call,   1.60,   1.56, 0.08, 0.06, 0.50, 0.12,  0.0291, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,   70.00,  75.00, 0.05, 0.10, 0.50, 0.35,  4.0870, 1.0e-4),
            // pag 24
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.15,  0.0205, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.15,  1.8734, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.15,  9.9413, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.25,  0.3150, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.25,  3.1217, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.25, 10.3556, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.10, 0.35,  0.9474, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.10, 0.35,  4.3693, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.10, 0.35, 11.1381, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.15,  0.8069, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.15,  4.0232, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.15, 10.5769, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.25,  2.7026, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.25,  6.6997, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.25, 12.7857, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00,  90.00, 0.10, 0.10, 0.50, 0.35,  4.9329, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 100.00, 0.10, 0.10, 0.50, 0.35,  9.3679, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Call, 100.00, 110.00, 0.10, 0.10, 0.50, 0.35, 15.3086, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.15,  9.9210, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.15,  1.8734, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.15,  0.0408, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.25, 10.2155, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.25,  3.1217, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.25,  0.4551, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.10, 0.35, 10.8479, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.10, 0.35,  4.3693, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.10, 0.35,  1.2376, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.15, 10.3192, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.15,  4.0232, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.15,  1.0646, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.25, 12.2149, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.25,  6.6997, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.25,  3.2734, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00,  90.00, 0.10, 0.10, 0.50, 0.35, 14.4452, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 100.00, 0.10, 0.10, 0.50, 0.35,  9.3679, 1.0e-4),
            new EuropeanOptionData(QLNet.Option.Type.Put,  100.00, 110.00, 0.10, 0.10, 0.50, 0.35,  5.7963, 1.0e-4),
            // pag 27
            new EuropeanOptionData(QLNet.Option.Type.Call,  40.00,  42.00, 0.08, 0.04, 0.75, 0.35,  5.0975, 1.0e-4)
         };

            DayCounter dc = new Actual360();
            Calendar calendar = new TARGET();
            var today = Date.Today;

            var discFor = 0.0;
            var discDom = 0.0;
            var implVol = 0.0;
            var deltaCall = 0.0;
            var deltaPut = 0.0;
            var expectedDiff = 0.0;
            var calculatedDiff = 0.0;
            var error = 0.0;
            var forward = 0.0;

            var spotQuote = new SimpleQuote(0.0);

            var qQuote = new SimpleQuote(0.0);
            var qHandle = new Handle<Quote>(qQuote);
            YieldTermStructure qTS = new FlatForward(today, qHandle, dc);

            var rQuote = new SimpleQuote(0.0);
            var rHandle = new Handle<Quote>(qQuote);
            YieldTermStructure rTS = new FlatForward(today, rHandle, dc);

            var volQuote = new SimpleQuote(0.0);
            var volHandle = new Handle<Quote>(volQuote);
            BlackVolTermStructure volTS = new BlackConstantVol(today, calendar, volHandle, dc);

            StrikedTypePayoff payoff;
            Date exDate;
            Exercise exercise;

            var tolerance = 1.0e-10;

            for (var i = 0; i < values.Length; ++i)
            {
                payoff = new PlainVanillaPayoff(QLNet.Option.Type.Call, values[i].strike);
                exDate = today + timeToDays(values[i].t);
                exercise = new EuropeanExercise(exDate);

                spotQuote.setValue(values[i].s);
                volQuote.setValue(values[i].v);
                rQuote.setValue(values[i].r);
                qQuote.setValue(values[i].q);
                discDom = rTS.discount(exDate);
                discFor = qTS.discount(exDate);
                implVol = System.Math.Sqrt(volTS.blackVariance(exDate, 0.0));
                forward = spotQuote.value() * discFor / discDom;

                var myCalc = new BlackDeltaCalculator(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Spot,
                                                                       spotQuote.value(), discDom, discFor, implVol);

                deltaCall = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                deltaPut = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expectedDiff = discFor;
                calculatedDiff = deltaCall - deltaPut;
                error = System.Math.Abs(expectedDiff - calculatedDiff);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Put-call parity failed for spot delta. \n"
                                 + "Calculated Call Delta: " + deltaCall + "\n"
                                 + "Calculated Put Delta:  " + deltaPut + "\n"
                                 + "Expected Difference:   " + expectedDiff + "\n"
                                 + "Calculated Difference: " + calculatedDiff);
                }
                myCalc.setDeltaType(DeltaVolQuote.DeltaType.Fwd);

                deltaCall = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                deltaPut = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expectedDiff = 1.0;
                calculatedDiff = deltaCall - deltaPut;
                error = System.Math.Abs(expectedDiff - calculatedDiff);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Put-call parity failed for forward delta. \n"
                                 + "Calculated Call Delta: " + deltaCall + "\n"
                                 + "Calculated Put Delta:  " + deltaPut + "\n"
                                 + "Expected Difference:   " + expectedDiff + "\n"
                                 + "Calculated Difference: " + calculatedDiff);
                }

                myCalc.setDeltaType(DeltaVolQuote.DeltaType.PaSpot);

                deltaCall = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                deltaPut = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expectedDiff = discFor * values[i].strike / forward;
                calculatedDiff = deltaCall - deltaPut;
                error = System.Math.Abs(expectedDiff - calculatedDiff);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Put-call parity failed for premium-adjusted spot delta. \n"
                                 + "Calculated Call Delta: " + deltaCall + "\n"
                                 + "Calculated Put Delta:  " + deltaPut + "\n"
                                 + "Expected Difference:   " + expectedDiff + "\n"
                                 + "Calculated Difference: " + calculatedDiff);
                }

                myCalc.setDeltaType(DeltaVolQuote.DeltaType.PaFwd);

                deltaCall = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                deltaPut = myCalc.deltaFromStrike(values[i].strike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expectedDiff = values[i].strike / forward;
                calculatedDiff = deltaCall - deltaPut;
                error = System.Math.Abs(expectedDiff - calculatedDiff);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Put-call parity failed for premium-adjusted forward delta. \n"
                                 + "Calculated Call Delta: " + deltaCall + "\n"
                                 + "Calculated Put Delta:  " + deltaPut + "\n"
                                 + "Expected Difference:   " + expectedDiff + "\n"
                                 + "Calculated Difference: " + calculatedDiff);
                }
            }
        }

        [Fact]
        public void testAtmCalcs()
        {
            // Testing delta-neutral ATM quotations
            var backup = new SavedSettings();

            DeltaData[] values =
            {
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Spot,     1.421, 0.997306, 0.992266,          0.1180654,  1.608080, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaSpot,   1.421, 0.997306, 0.992266,      0.1180654,  1.600545, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Fwd,      1.421, 0.997306, 0.992266,      0.1180654,  1.609029, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaFwd,    1.421, 0.997306, 0.992266,      0.1180654,  1.601550, 0.15),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Spot,     122.121,  0.9695434, 0.9872347,  0.0887676,  119.8031, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaSpot,   122.121,  0.9695434, 0.9872347,  0.0887676,  117.7096, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.Fwd,      122.121,  0.9695434, 0.9872347,  0.0887676,  120.0592, 0.67),
            new DeltaData(QLNet.Option.Type.Call, DeltaVolQuote.DeltaType.PaFwd,    122.121,  0.9695434, 0.9872347,  0.0887676,  118.0532, 0.67),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Spot,     3.4582,   0.99979, 0.9250616,   0.3199034,  4.964924, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaSpot,   3.4582,   0.99979, 0.9250616,   0.3199034,  3.778327, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Fwd,      3.4582,   0.99979, 0.9250616,   0.3199034,  4.51896, -0.821),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaFwd,    3.4582,   0.99979, 0.9250616,   0.3199034,  3.65728, -0.821),
            // Data taken from Castagnas "FX Options and Smile Risk" (Wiley 2009)
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.Spot,     103.00,   0.99482, 0.98508,     0.07247845, 97.47,  -0.25),
            new DeltaData(QLNet.Option.Type.Put,  DeltaVolQuote.DeltaType.PaSpot,   103.00,   0.99482, 0.98508,     0.07247845, 97.22,  -0.25),
            // Extreme case: zero vol, ATM Fwd strike
            new DeltaData(QLNet.Option.Type.Call,  DeltaVolQuote.DeltaType.Fwd, 103.00,     0.99482, 0.98508,       0.0,    101.0013, 0.5),
            new DeltaData(QLNet.Option.Type.Call,  DeltaVolQuote.DeltaType.Spot,    103.00,   0.99482, 0.98508,     0.0,    101.0013, 0.99482 * 0.5)
         };

            DeltaVolQuote.DeltaType currDt;
            double currSpot;
            double currdDf;
            double currfDf;
            double currStdDev;
            double expected;
            double calculated;
            double error;
            var tolerance = 1.0e-2; // not that small, but sufficient for strikes
            double currAtmStrike;
            double currCallDelta;
            double currPutDelta;
            double currFwd;

            for (var i = 0; i < values.Length; i++)
            {

                currDt = values[i].dt;
                currSpot = values[i].spot;
                currdDf = values[i].dDf;
                currfDf = values[i].fDf;
                currStdDev = values[i].stdDev;
                currFwd = currSpot * currfDf / currdDf;

                var myCalc = new BlackDeltaCalculator(QLNet.Option.Type.Call, currDt, currSpot, currdDf,
                                                                       currfDf, currStdDev);

                currAtmStrike = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmDeltaNeutral);
                currCallDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                currPutDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expected = 0.0;
                calculated = currCallDelta + currPutDelta;
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Delta neutrality failed for spot delta in Delta Calculator. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Delta Sum: " + calculated + "\n"
                                 + "Expected Delta Sum:   " + expected + "\n"
                                 + "Error: " + error);
                }

                myCalc.setDeltaType(DeltaVolQuote.DeltaType.Fwd);
                currAtmStrike = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmDeltaNeutral);
                currCallDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                currPutDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expected = 0.0;
                calculated = currCallDelta + currPutDelta;
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Delta neutrality failed for forward delta in Delta Calculator. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Delta Sum: " + calculated + "\n"
                                 + "Expected Delta Sum:   " + expected + "\n"
                                 + "Error: " + error);
                }

                myCalc.setDeltaType(DeltaVolQuote.DeltaType.PaSpot);
                currAtmStrike = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmDeltaNeutral);
                currCallDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                currPutDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expected = 0.0;
                calculated = currCallDelta + currPutDelta;
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Delta neutrality failed for premium-adjusted spot delta in Delta Calculator. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Delta Sum: " + calculated + "\n"
                                 + "Expected Delta Sum:   " + expected + "\n"
                                 + "Error: " + error);
                }


                myCalc.setDeltaType(DeltaVolQuote.DeltaType.PaFwd);
                currAtmStrike = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmDeltaNeutral);
                currCallDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Put);
                currPutDelta = myCalc.deltaFromStrike(currAtmStrike);
                myCalc.setOptionType(QLNet.Option.Type.Call);

                expected = 0.0;
                calculated = currCallDelta + currPutDelta;
                error = System.Math.Abs(calculated - expected);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Delta neutrality failed for premium-adjusted forward delta in Delta Calculator. \n"
                                 + "Iteration: " + i + "\n"
                                 + "Calculated Delta Sum: " + calculated + "\n"
                                 + "Expected Delta Sum:   " + expected + "\n"
                                 + "Error: " + error);
                }

                // Test ATM forward Calculations
                calculated = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmFwd);
                expected = currFwd;
                error = System.Math.Abs(expected - calculated);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Atm forward test failed. \n"
                                 + "Calculated Value: " + calculated + "\n"
                                 + "Expected   Value: " + expected + "\n"
                                 + "Error: " + error);
                }

                // Test ATM 0.50 delta calculations
                myCalc.setDeltaType(DeltaVolQuote.DeltaType.Fwd);
                var atmFiftyStrike = myCalc.atmStrike(DeltaVolQuote.AtmType.AtmPutCall50);
                calculated = System.Math.Abs(myCalc.deltaFromStrike(atmFiftyStrike));
                expected = 0.50;
                error = System.Math.Abs(expected - calculated);

                if (error > tolerance)
                {
                    QAssert.Fail("\n Atm 0.50 delta strike test failed. \n"
                                 + "Iteration:" + i + "\n"
                                 + "Calculated Value: " + calculated + "\n"
                                 + "Expected   Value: " + expected + "\n"
                                 + "Error: " + error);
                }
            }
        }
    }
}
