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
using Xunit;
using QLNet.Math.Distributions;
using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Cashflows;
using QLNet.Pricingengines.vanilla;
using QLNet.Time;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures;
using QLNet.processes;
using QLNet.Termstructures.Volatility.equityfx;
using QLNet.Quotes;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_DigitalCoupon
    {
        private class CommonVars
        {
            // global data
            public Date today, settlement;
            public double nominal;
            public Calendar calendar;
            public IborIndex index;
            public int fixingDays;
            public RelinkableHandle<YieldTermStructure> termStructure;
            public double optionTolerance;
            public double blackTolerance;

            // cleanup
            SavedSettings backup;

            // setup
            public CommonVars()
            {
                backup = new SavedSettings();
                termStructure = new RelinkableHandle<YieldTermStructure>();
                fixingDays = 2;
                nominal = 1000000.0;
                index = new Euribor6M(termStructure);
                calendar = index.fixingCalendar();
                today = calendar.adjust(Settings.evaluationDate());
                Settings.setEvaluationDate(today);
                settlement = calendar.advance(today, fixingDays, TimeUnit.Days);
                termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new Actual365Fixed()));
                optionTolerance = 1.0e-04;
                blackTolerance = 1e-10;
            }
        }

        [Fact]
        public void testAssetOrNothing()
        {

            // Testing European asset-or-nothing digital coupon

            /*  Call Payoff = (aL+b)Heaviside(aL+b-X) =  a Max[L-X'] + (b+aX')Heaviside(L-X')
               Value Call = aF N(d1') + bN(d2')
               Put Payoff =  (aL+b)Heaviside(X-aL-b) = -a Max[X-L'] + (b+aX')Heaviside(X'-L)
               Value Put = aF N(-d1') + bN(-d2')
               where:
               d1' = ln(F/X')/stdDev + 0.5*stdDev;
            */

            var vars = new CommonVars();

            double[] vols = { 0.05, 0.15, 0.30 };
            double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] gearings = { 1.0, 2.8 };
            double[] spreads = { 0.0, 0.005 };

            var gap = 1e-7; /* low, in order to compare digital option value
                           with black formula result */
            var replication = new DigitalReplication(Replication.Type.Central, gap);
            for (var i = 0; i < vols.Length; i++)
            {
                var capletVol = vols[i];
                var vol = new RelinkableHandle<OptionletVolatilityStructure>();
                vol.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                           capletVol, new Actual360()));
                for (var j = 0; j < strikes.Length; j++)
                {
                    var strike = strikes[j];
                    for (var k = 9; k < 10; k++)
                    {
                        var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                        var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                        double? nullstrike = null;
                        var paymentDate = endDate;
                        for (var h = 0; h < gearings.Length; h++)
                        {
                            var gearing = gearings[h];
                            var spread = spreads[h];

                            FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                                           vars.fixingDays, vars.index, gearing, spread);
                            // Floating Rate Coupon - Call Digital option
                            var digitalCappedCoupon = new DigitalCoupon(underlying,
                                                                                  strike, Position.Type.Short, false, nullstrike,
                                                                                  nullstrike, Position.Type.Short, false, nullstrike,
                                                                                  replication);
                            IborCouponPricer pricer = new BlackIborCouponPricer(vol);
                            digitalCappedCoupon.setPricer(pricer);

                            // Check digital option price vs N(d1) price
                            var accrualPeriod = underlying.accrualPeriod();
                            var discount = vars.termStructure.link.discount(endDate);
                            var exerciseDate = underlying.fixingDate();
                            var forward = underlying.rate();
                            var effFwd = (forward - spread) / gearing;
                            var effStrike = (strike - spread) / gearing;
                            var stdDev = System.Math.Sqrt(vol.link.blackVariance(exerciseDate, effStrike));
                            var phi = new CumulativeNormalDistribution();
                            var d1 = System.Math.Log(effFwd / effStrike) / stdDev + 0.5 * stdDev;
                            var d2 = d1 - stdDev;
                            var N_d1 = phi.value(d1);
                            var N_d2 = phi.value(d2);
                            var nd1Price = (gearing * effFwd * N_d1 + spread * N_d2)
                                           * vars.nominal * accrualPeriod * discount;
                            var optionPrice = digitalCappedCoupon.callOptionRate() *
                                              vars.nominal * accrualPeriod * discount;
                            var error = System.Math.Abs(nd1Price - optionPrice);
                            if (error > vars.optionTolerance)
                                QAssert.Fail("\nDigital Call Option:" +
                                             "\nVolatility = " + capletVol +
                                             "\nStrike = " + strike +
                                             "\nExercise = " + k + 1 + " years" +
                                             "\nOption price by replication = " + optionPrice +
                                             "\nOption price by Cox-Rubinstein formula = " + nd1Price +
                                             "\nError " + error);

                            // Check digital option price vs N(d1) price using Vanilla Option class
                            if (spread == 0.0)
                            {
                                Exercise exercise = new EuropeanExercise(exerciseDate);
                                var discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                                var fwd = new SimpleQuote(effFwd * discountAtFixing);
                                var qRate = new SimpleQuote(0.0);
                                var qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                                var vol1 = new SimpleQuote(0.0);
                                var volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                                StrikedTypePayoff callPayoff = new AssetOrNothingPayoff(QLNet.Option.Type.Call, effStrike);
                                var stochProcess = new BlackScholesMertonProcess(
                                   new Handle<Quote>(fwd),
                                   new Handle<YieldTermStructure>(qTS),
                                   new Handle<YieldTermStructure>(vars.termStructure),
                                   new Handle<BlackVolTermStructure>(volTS));
                                IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                                var callOpt = new VanillaOption(callPayoff, exercise);
                                callOpt.setPricingEngine(engine);
                                var callVO = vars.nominal * gearing
                                                          * accrualPeriod * callOpt.NPV()
                                                          * discount / discountAtFixing
                                                * forward / effFwd;
                                error = System.Math.Abs(nd1Price - callVO);
                                if (error > vars.blackTolerance)
                                    QAssert.Fail("\nDigital Call Option:" +
                                                 "\nVolatility = " + capletVol +
                                                 "\nStrike = " + strike +
                                                 "\nExercise = " + k + 1 + " years" +
                                                 "\nOption price by Black asset-ot-nothing payoff = " + callVO +
                                                 "\nOption price by Cox-Rubinstein = " + nd1Price +
                                                 "\nError " + error);
                            }

                            // Floating Rate Coupon + Put Digital option
                            var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long,
                                                                                   false, nullstrike, strike, Position.Type.Long, false, nullstrike, replication);
                            digitalFlooredCoupon.setPricer(pricer);

                            // Check digital option price vs N(d1) price
                            N_d1 = phi.value(-d1);
                            N_d2 = phi.value(-d2);
                            nd1Price = (gearing * effFwd * N_d1 + spread * N_d2)
                                       * vars.nominal * accrualPeriod * discount;
                            optionPrice = digitalFlooredCoupon.putOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
                            error = System.Math.Abs(nd1Price - optionPrice);
                            if (error > vars.optionTolerance)
                                QAssert.Fail("\nDigital Put Option:" +
                                             "\nVolatility = " + capletVol +
                                             "\nStrike = " + strike +
                                             "\nExercise = " + k + 1 + " years" +
                                             "\nOption price by replication = " + optionPrice +
                                             "\nOption price by Cox-Rubinstein = " + nd1Price +
                                             "\nError " + error);

                            // Check digital option price vs N(d1) price using Vanilla Option class
                            if (spread == 0.0)
                            {
                                Exercise exercise = new EuropeanExercise(exerciseDate);
                                var discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                                var fwd = new SimpleQuote(effFwd * discountAtFixing);
                                var qRate = new SimpleQuote(0.0);
                                var qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                                //SimpleQuote vol = new SimpleQuote(0.0);
                                var volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                                var stochProcess = new BlackScholesMertonProcess(
                                   new Handle<Quote>(fwd),
                                   new Handle<YieldTermStructure>(qTS),
                                   new Handle<YieldTermStructure>(vars.termStructure),
                                   new Handle<BlackVolTermStructure>(volTS));
                                StrikedTypePayoff putPayoff = new AssetOrNothingPayoff(QLNet.Option.Type.Put, effStrike);
                                IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                                var putOpt = new VanillaOption(putPayoff, exercise);
                                putOpt.setPricingEngine(engine);
                                var putVO = vars.nominal * gearing
                                                         * accrualPeriod * putOpt.NPV()
                                                         * discount / discountAtFixing
                                                * forward / effFwd;
                                error = System.Math.Abs(nd1Price - putVO);
                                if (error > vars.blackTolerance)
                                    QAssert.Fail("\nDigital Put Option:" +
                                                 "\nVolatility = " + capletVol +
                                                 "\nStrike = " + strike +
                                                 "\nExercise = " + k + 1 + " years" +
                                                 "\nOption price by Black asset-ot-nothing payoff = " + putVO +
                                                 "\nOption price by Cox-Rubinstein = " + nd1Price +
                                                 "\nError " + error);
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public void testAssetOrNothingDeepInTheMoney()
        {
            // Testing European deep in-the-money asset-or-nothing digital coupon
            var vars = new CommonVars();

            var gearing = 1.0;
            var spread = 0.0;

            var capletVolatility = 0.0001;
            var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                              capletVolatility, new Actual360()));
            var gap = 1e-4;
            var replication = new DigitalReplication(Replication.Type.Central, gap);

            for (var k = 0; k < 10; k++)
            {
                // Loop on start and end dates
                var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                double? nullstrike = null;
                var paymentDate = endDate;

                FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                               vars.fixingDays, vars.index, gearing, spread);

                // Floating Rate Coupon - Deep-in-the-money Call Digital option
                var strike = 0.001;
                var digitalCappedCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short, false,
                                                                      nullstrike, nullstrike, Position.Type.Short, false, nullstrike, replication);
                IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                digitalCappedCoupon.setPricer(pricer);

                // Check price vs its target price
                var accrualPeriod = underlying.accrualPeriod();
                var discount = vars.termStructure.link.discount(endDate);

                var targetOptionPrice = underlying.price(vars.termStructure);
                var targetPrice = 0.0;
                var digitalPrice = digitalCappedCoupon.price(vars.termStructure);
                var error = System.Math.Abs(targetPrice - digitalPrice);
                var tolerance = 1e-08;
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon - Digital Call Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon Price = " + digitalPrice +
                                 "\nTarget price = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option price
                var replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                             vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                var optionTolerance = 1e-08;
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Call Option:" +
                                 "\nVolatility = " + +capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);

                // Floating Rate Coupon + Deep-in-the-money Put Digital option
                strike = 0.99;
                var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long, false,
                                                                       nullstrike, strike, Position.Type.Long, false, nullstrike, replication);
                digitalFlooredCoupon.setPricer(pricer);

                // Check price vs its target price
                targetOptionPrice = underlying.price(vars.termStructure);
                targetPrice = underlying.price(vars.termStructure) + targetOptionPrice;
                digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
                error = System.Math.Abs(targetPrice - digitalPrice);
                tolerance = 2.5e-06;
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon + Digital Put Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nDigital coupon price = " + digitalPrice +
                                 "\nTarget price = " + targetPrice +
                                 "\nError " + error);

                // Check digital option
                replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                         vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                optionTolerance = 2.5e-06;
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Put Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError " + error);
            }
        }

        [Fact]
        public void testAssetOrNothingDeepOutTheMoney()
        {
            // Testing European deep out-the-money asset-or-nothing digital coupon
            var vars = new CommonVars();

            var gearing = 1.0;
            var spread = 0.0;

            var capletVolatility = 0.0001;
            var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                              capletVolatility, new Actual360()));
            var gap = 1e-4;
            var replication = new DigitalReplication(Replication.Type.Central, gap);

            for (var k = 0; k < 10; k++)
            {
                // loop on start and end dates
                var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                double? nullstrike = null;
                var paymentDate = endDate;

                FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                                vars.fixingDays, vars.index, gearing, spread);

                // Floating Rate Coupon - Deep-out-of-the-money Call Digital option
                var strike = 0.99;
                var digitalCappedCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short, false,
                                                                      nullstrike, nullstrike, Position.Type.Long, false, nullstrike, replication/*Replication::Central, gap*/);
                IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                digitalCappedCoupon.setPricer(pricer);

                // Check price vs its target
                var accrualPeriod = underlying.accrualPeriod();
                var discount = vars.termStructure.link.discount(endDate);

                var targetPrice = underlying.price(vars.termStructure);
                var digitalPrice = digitalCappedCoupon.price(vars.termStructure);
                var error = System.Math.Abs(targetPrice - digitalPrice);
                var tolerance = 1e-10;
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon - Digital Call Option :" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option price
                var targetOptionPrice = 0.0;
                var replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                             vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                var optionTolerance = 1e-08;
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Call Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);

                // Floating Rate Coupon - Deep-out-of-the-money Put Digital option
                strike = 0.01;
                var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long, false,
                                                                       nullstrike, strike, Position.Type.Long, false, nullstrike, replication);
                digitalFlooredCoupon.setPricer(pricer);

                // Check price vs its target
                targetPrice = underlying.price(vars.termStructure);
                digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
                tolerance = 1e-08;
                error = System.Math.Abs(targetPrice - digitalPrice);
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon + Digital Put Coupon:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option
                targetOptionPrice = 0.0;
                replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                         vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Put Coupon:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);
            }
        }

        [Fact]
        public void testCashOrNothing()
        {
            // Testing European cash-or-nothing digital coupon

            /*  Call Payoff = R Heaviside(aL+b-X)
               Value Call = R N(d2')
               Put Payoff =  R Heaviside(X-aL-b)
               Value Put = R N(-d2')
               where:
               d2' = ln(F/X')/stdDev - 0.5*stdDev;
            */

            var vars = new CommonVars();

            double[] vols = { 0.05, 0.15, 0.30 };
            double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

            var gearing = 3.0;
            var spread = -0.0002;

            var gap = 1e-08; /* very low, in order to compare digital option value
                                          with black formula result */
            var replication = new DigitalReplication(Replication.Type.Central, gap);
            var vol = new RelinkableHandle<OptionletVolatilityStructure>();

            for (var i = 0; i < vols.Length; i++)
            {
                var capletVol = vols[i];
                vol.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                           capletVol, new Actual360()));
                for (var j = 0; j < strikes.Length; j++)
                {
                    var strike = strikes[j];
                    for (var k = 0; k < 10; k++)
                    {
                        var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                        var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                        double? nullstrike = null;
                        var cashRate = 0.01;

                        var paymentDate = endDate;
                        FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                                       vars.fixingDays, vars.index, gearing, spread);
                        // Floating Rate Coupon - Call Digital option
                        var digitalCappedCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short, false,
                                                                              cashRate, nullstrike, Position.Type.Short, false, nullstrike, replication);
                        IborCouponPricer pricer = new BlackIborCouponPricer(vol);
                        digitalCappedCoupon.setPricer(pricer);

                        // Check digital option price vs N(d2) price
                        var exerciseDate = underlying.fixingDate();
                        var forward = underlying.rate();
                        var effFwd = (forward - spread) / gearing;
                        var effStrike = (strike - spread) / gearing;
                        var accrualPeriod = underlying.accrualPeriod();
                        var discount = vars.termStructure.link.discount(endDate);
                        var stdDev = System.Math.Sqrt(vol.link.blackVariance(exerciseDate, effStrike));
                        var ITM = Utils.blackFormulaCashItmProbability(QLNet.Option.Type.Call, effStrike, effFwd, stdDev);
                        var nd2Price = ITM * vars.nominal * accrualPeriod * discount * cashRate;
                        var optionPrice = digitalCappedCoupon.callOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
                        var error = System.Math.Abs(nd2Price - optionPrice);
                        if (error > vars.optionTolerance)
                            QAssert.Fail("\nDigital Call Option:" +
                                         "\nVolatility = " + capletVol +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nPrice by replication = " + optionPrice +
                                         "\nPrice by Reiner-Rubinstein = " + nd2Price +
                                         "\nError = " + error);

                        // Check digital option price vs N(d2) price using Vanilla Option class
                        Exercise exercise = new EuropeanExercise(exerciseDate);
                        var discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                        var fwd = new SimpleQuote(effFwd * discountAtFixing);
                        var qRate = new SimpleQuote(0.0);
                        var qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                        //SimpleQuote vol = new SimpleQuote(0.0);
                        var volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                        StrikedTypePayoff callPayoff = new CashOrNothingPayoff(QLNet.Option.Type.Call, effStrike, cashRate);
                        var stochProcess = new BlackScholesMertonProcess(
                           new Handle<Quote>(fwd),
                           new Handle<YieldTermStructure>(qTS),
                           new Handle<YieldTermStructure>(vars.termStructure),
                           new Handle<BlackVolTermStructure>(volTS));
                        IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                        var callOpt = new VanillaOption(callPayoff, exercise);
                        callOpt.setPricingEngine(engine);
                        var callVO = vars.nominal * accrualPeriod * callOpt.NPV()
                                        * discount / discountAtFixing;
                        error = System.Math.Abs(nd2Price - callVO);
                        if (error > vars.blackTolerance)
                            QAssert.Fail("\nDigital Call Option:" +
                                         "\nVolatility = " + capletVol +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nOption price by Black asset-ot-nothing payoff = " + callVO +
                                         "\nOption price by Reiner-Rubinstein = " + nd2Price +
                                         "\nError " + error);

                        // Floating Rate Coupon + Put Digital option
                        var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long, false,
                                                                               nullstrike, strike, Position.Type.Long, false, cashRate, replication);
                        digitalFlooredCoupon.setPricer(pricer);


                        // Check digital option price vs N(d2) price
                        ITM = Utils.blackFormulaCashItmProbability(QLNet.Option.Type.Put, effStrike, effFwd, stdDev);
                        nd2Price = ITM * vars.nominal * accrualPeriod * discount * cashRate;
                        optionPrice = digitalFlooredCoupon.putOptionRate() *
                                      vars.nominal * accrualPeriod * discount;
                        error = System.Math.Abs(nd2Price - optionPrice);
                        if (error > vars.optionTolerance)
                            QAssert.Fail("\nPut Digital Option:" +
                                         "\nVolatility = " + capletVol +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nPrice by replication = " + optionPrice +
                                         "\nPrice by Reiner-Rubinstein = " + nd2Price +
                                         "\nError = " + error);

                        // Check digital option price vs N(d2) price using Vanilla Option class
                        StrikedTypePayoff putPayoff = new CashOrNothingPayoff(QLNet.Option.Type.Put, effStrike, cashRate);
                        var putOpt = new VanillaOption(putPayoff, exercise);
                        putOpt.setPricingEngine(engine);
                        var putVO = vars.nominal * accrualPeriod * putOpt.NPV()
                                        * discount / discountAtFixing;
                        error = System.Math.Abs(nd2Price - putVO);
                        if (error > vars.blackTolerance)
                            QAssert.Fail("\nDigital Put Option:" +
                                         "\nVolatility = " + capletVol +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nOption price by Black asset-ot-nothing payoff = " + putVO +
                                         "\nOption price by Reiner-Rubinstein = " + nd2Price +
                                         "\nError " + error);
                    }
                }
            }
        }

        [Fact]
        public void testCashOrNothingDeepInTheMoney()
        {
            // Testing European deep in-the-money cash-or-nothing digital coupon
            var vars = new CommonVars();

            var gearing = 1.0;
            var spread = 0.0;

            var capletVolatility = 0.0001;
            var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                              capletVolatility, new Actual360()));

            for (var k = 0; k < 10; k++)
            {
                // Loop on start and end dates
                var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                double? nullstrike = null;
                var cashRate = 0.01;
                var gap = 1e-4;
                var replication = new DigitalReplication(Replication.Type.Central, gap);
                var paymentDate = endDate;

                FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                               vars.fixingDays, vars.index, gearing, spread);
                // Floating Rate Coupon - Deep-in-the-money Call Digital option
                var strike = 0.001;
                var digitalCappedCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short, false,
                                                                      cashRate, nullstrike, Position.Type.Short, false, nullstrike, replication);
                IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                digitalCappedCoupon.setPricer(pricer);

                // Check price vs its target
                var accrualPeriod = underlying.accrualPeriod();
                var discount = vars.termStructure.link.discount(endDate);

                var targetOptionPrice = cashRate * vars.nominal * accrualPeriod * discount;
                var targetPrice = underlying.price(vars.termStructure) - targetOptionPrice;
                var digitalPrice = digitalCappedCoupon.price(vars.termStructure);

                var error = System.Math.Abs(targetPrice - digitalPrice);
                var tolerance = 1e-07;
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon - Digital Call Coupon:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price = " + targetPrice +
                                 "\nError " + error);

                // Check digital option price
                var replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                             vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                var optionTolerance = 1e-07;
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Call Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);

                // Floating Rate Coupon + Deep-in-the-money Put Digital option
                strike = 0.99;
                var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long, false,
                                                                       nullstrike, strike, Position.Type.Long, false, cashRate, replication);
                digitalFlooredCoupon.setPricer(pricer);

                // Check price vs its target
                targetPrice = underlying.price(vars.termStructure) + targetOptionPrice;
                digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
                error = System.Math.Abs(targetPrice - digitalPrice);
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon + Digital Put Option:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price  = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option
                replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                         vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Put Coupon:" +
                                 "\nVolatility = " + capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);
            }
        }

        [Fact]
        public void testCashOrNothingDeepOutTheMoney()
        {
            // Testing European deep out-the-money cash-or-nothing digital coupon
            var vars = new CommonVars();

            var gearing = 1.0;
            var spread = 0.0;

            var capletVolatility = 0.0001;
            var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                              capletVolatility, new Actual360()));

            for (var k = 0; k < 10; k++)
            {
                // loop on start and end dates
                var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                double? nullstrike = null;
                var cashRate = 0.01;
                var gap = 1e-4;
                var replication = new DigitalReplication(Replication.Type.Central, gap);
                var paymentDate = endDate;

                FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                               vars.fixingDays, vars.index, gearing, spread);
                // Deep out-of-the-money Capped Digital Coupon
                var strike = 0.99;
                var digitalCappedCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short, false,
                                                                      cashRate, nullstrike, Position.Type.Short, false, nullstrike, replication);

                IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                digitalCappedCoupon.setPricer(pricer);

                // Check price vs its target
                var accrualPeriod = underlying.accrualPeriod();
                var discount = vars.termStructure.link.discount(endDate);

                var targetPrice = underlying.price(vars.termStructure);
                var digitalPrice = digitalCappedCoupon.price(vars.termStructure);
                var error = System.Math.Abs(targetPrice - digitalPrice);
                var tolerance = 1e-10;
                if (error > tolerance)
                    QAssert.Fail("\nFloating Coupon + Digital Call Option:" +
                                 "\nVolatility = " + +capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price  = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option price
                var targetOptionPrice = 0.0;
                var replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                             vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                var optionTolerance = 1e-10;
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Call Option:" +
                                 "\nVolatility = " + +capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication = " + replicationOptionPrice +
                                 "\nTarget price = " + targetOptionPrice +
                                 "\nError = " + error);

                // Deep out-of-the-money Floored Digital Coupon
                strike = 0.01;
                var digitalFlooredCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long, false,
                                                                       nullstrike, strike, Position.Type.Long, false, cashRate, replication);
                digitalFlooredCoupon.setPricer(pricer);

                // Check price vs its target
                targetPrice = underlying.price(vars.termStructure);
                digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
                tolerance = 1e-09;
                error = System.Math.Abs(targetPrice - digitalPrice);
                if (error > tolerance)
                    QAssert.Fail("\nDigital Floored Coupon:" +
                                 "\nVolatility = " + +capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nCoupon price = " + digitalPrice +
                                 "\nTarget price  = " + targetPrice +
                                 "\nError = " + error);

                // Check digital option
                targetOptionPrice = 0.0;
                replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                         vars.nominal * accrualPeriod * discount;
                error = System.Math.Abs(targetOptionPrice - replicationOptionPrice);
                if (error > optionTolerance)
                    QAssert.Fail("\nDigital Put Option:" +
                                 "\nVolatility = " + +capletVolatility +
                                 "\nStrike = " + +strike +
                                 "\nExercise = " + k + 1 + " years" +
                                 "\nPrice by replication " + replicationOptionPrice +
                                 "\nTarget price " + targetOptionPrice +
                                 "\nError " + error);
            }
        }

        [Fact]
        public void testCallPutParity()
        {
            // Testing call/put parity for European digital coupon
            var vars = new CommonVars();

            double[] vols = { 0.05, 0.15, 0.30 };
            double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

            var gearing = 1.0;
            var spread = 0.0;

            var gap = 1e-04;
            var replication = new DigitalReplication(Replication.Type.Central, gap);

            for (var i = 0; i < vols.Length; i++)
            {
                var capletVolatility = vols[i];
                var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
                volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                                  capletVolatility, new Actual360()));
                for (var j = 0; j < strikes.Length; j++)
                {
                    var strike = strikes[j];
                    for (var k = 0; k < 10; k++)
                    {
                        var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                        var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                        double? nullstrike = null;

                        var paymentDate = endDate;

                        FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                                       vars.fixingDays, vars.index, gearing, spread);
                        // Cash-or-Nothing
                        var cashRate = 0.01;
                        // Floating Rate Coupon + Call Digital option
                        var cash_digitalCallCoupon = new DigitalCoupon(underlying, strike, Position.Type.Long, false,
                                                                                 cashRate, nullstrike, Position.Type.Long, false, nullstrike, replication);
                        IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                        cash_digitalCallCoupon.setPricer(pricer);
                        // Floating Rate Coupon - Put Digital option
                        var cash_digitalPutCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long,
                                                                                false, nullstrike, strike, Position.Type.Short, false, cashRate, replication);

                        cash_digitalPutCoupon.setPricer(pricer);
                        var digitalPrice = cash_digitalCallCoupon.price(vars.termStructure) -
                                           cash_digitalPutCoupon.price(vars.termStructure);
                        // Target price
                        var accrualPeriod = underlying.accrualPeriod();
                        var discount = vars.termStructure.link.discount(endDate);
                        var targetPrice = vars.nominal * accrualPeriod * discount * cashRate;

                        var error = System.Math.Abs(targetPrice - digitalPrice);
                        var tolerance = 1.0e-08;
                        if (error > tolerance)
                            QAssert.Fail("\nCash-or-nothing:" +
                                         "\nVolatility = " + +capletVolatility +
                                         "\nStrike = " + +strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nPrice = " + digitalPrice +
                                         "\nTarget Price  = " + targetPrice +
                                         "\nError = " + error);

                        // Asset-or-Nothing
                        // Floating Rate Coupon + Call Digital option
                        var asset_digitalCallCoupon = new DigitalCoupon(underlying, strike, Position.Type.Long, false,
                                                                                  nullstrike, nullstrike, Position.Type.Long, false, nullstrike, replication);
                        asset_digitalCallCoupon.setPricer(pricer);
                        // Floating Rate Coupon - Put Digital option
                        var asset_digitalPutCoupon = new DigitalCoupon(underlying, nullstrike, Position.Type.Long,
                                                                                 false, nullstrike, strike, Position.Type.Short, false, nullstrike, replication);
                        asset_digitalPutCoupon.setPricer(pricer);
                        digitalPrice = asset_digitalCallCoupon.price(vars.termStructure) -
                                       asset_digitalPutCoupon.price(vars.termStructure);
                        // Target price
                        targetPrice = vars.nominal * accrualPeriod * discount * underlying.rate();
                        error = System.Math.Abs(targetPrice - digitalPrice);
                        tolerance = 1.0e-07;
                        if (error > tolerance)
                            QAssert.Fail("\nAsset-or-nothing:" +
                                         "\nVolatility = " + capletVolatility +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nPrice = " + digitalPrice +
                                         "\nTarget Price  = " + targetPrice +
                                         "\nError = " + error);
                    }
                }
            }
        }

        [Fact]
        public void testReplicationType()
        {
            // Testing replication ExerciseType for European digital coupon
            var vars = new CommonVars();

            double[] vols = { 0.05, 0.15, 0.30 };
            double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

            var gearing = 1.0;
            var spread = 0.0;

            var gap = 1e-04;
            var subReplication = new DigitalReplication(Replication.Type.Sub, gap);
            var centralReplication = new DigitalReplication(Replication.Type.Central, gap);
            var superReplication = new DigitalReplication(Replication.Type.Super, gap);

            for (var i = 0; i < vols.Length; i++)
            {
                var capletVolatility = vols[i];
                var volatility = new RelinkableHandle<OptionletVolatilityStructure>();
                volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following,
                                                                  capletVolatility, new Actual360()));
                for (var j = 0; j < strikes.Length; j++)
                {
                    var strike = strikes[j];
                    for (var k = 0; k < 10; k++)
                    {
                        var startDate = vars.calendar.advance(vars.settlement, new Period(k + 1, TimeUnit.Years));
                        var endDate = vars.calendar.advance(vars.settlement, new Period(k + 2, TimeUnit.Years));
                        double? nullstrike = null;

                        var paymentDate = endDate;

                        FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal, startDate, endDate,
                                                                       vars.fixingDays, vars.index, gearing, spread);
                        // Cash-or-Nothing
                        var cashRate = 0.005;
                        // Floating Rate Coupon + Call Digital option
                        var sub_cash_longDigitalCallCoupon = new DigitalCoupon(underlying, strike, Position.Type.Long,
                                                                                         false, cashRate, nullstrike, Position.Type.Long, false, nullstrike, subReplication);
                        var central_cash_longDigitalCallCoupon = new DigitalCoupon(underlying, strike,
                                                                                             Position.Type.Long, false, cashRate, nullstrike, Position.Type.Long, false, nullstrike,
                                                                                             centralReplication);
                        var over_cash_longDigitalCallCoupon = new DigitalCoupon(underlying, strike, Position.Type.Long,
                                                                                          false, cashRate, nullstrike, Position.Type.Long, false, nullstrike, superReplication);
                        IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                        sub_cash_longDigitalCallCoupon.setPricer(pricer);
                        central_cash_longDigitalCallCoupon.setPricer(pricer);
                        over_cash_longDigitalCallCoupon.setPricer(pricer);
                        var sub_digitalPrice = sub_cash_longDigitalCallCoupon.price(vars.termStructure);
                        var central_digitalPrice = central_cash_longDigitalCallCoupon.price(vars.termStructure);
                        var over_digitalPrice = over_cash_longDigitalCallCoupon.price(vars.termStructure);
                        var tolerance = 1.0e-09;
                        if (sub_digitalPrice > central_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - sub_digitalPrice) > tolerance ||
                            central_digitalPrice > over_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - over_digitalPrice) > tolerance)
                        {
                            QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Call Digital option" +
                                         "\nVolatility = " + +capletVolatility +
                                         "\nStrike = " + +strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nSub-Replication Price = " + sub_digitalPrice +
                                         "\nCentral-Replication Price = " + central_digitalPrice +
                                         "\nOver-Replication Price = " + over_digitalPrice);
                        }

                        // Floating Rate Coupon - Call Digital option
                        var sub_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying, strike, Position.Type.Short,
                                                                                          false, cashRate, nullstrike, Position.Type.Long, false, nullstrike, subReplication);
                        var central_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying, strike,
                                                                                              Position.Type.Short, false, cashRate, nullstrike, Position.Type.Long, false, nullstrike,
                                                                                              centralReplication);
                        var over_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying, strike,
                                                                                           Position.Type.Short, false, cashRate, nullstrike, Position.Type.Long, false, nullstrike,
                                                                                           superReplication);
                        sub_cash_shortDigitalCallCoupon.setPricer(pricer);
                        central_cash_shortDigitalCallCoupon.setPricer(pricer);
                        over_cash_shortDigitalCallCoupon.setPricer(pricer);
                        sub_digitalPrice = sub_cash_shortDigitalCallCoupon.price(vars.termStructure);
                        central_digitalPrice = central_cash_shortDigitalCallCoupon.price(vars.termStructure);
                        over_digitalPrice = over_cash_shortDigitalCallCoupon.price(vars.termStructure);
                        if (sub_digitalPrice > central_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - sub_digitalPrice) > tolerance ||
                            central_digitalPrice > over_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - over_digitalPrice) > tolerance)
                        {
                            QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon - Call Digital option" +
                                         "\nVolatility = " + +capletVolatility +
                                         "\nStrike = " + +strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nSub-Replication Price = " + sub_digitalPrice +
                                         "\nCentral-Replication Price = " + central_digitalPrice +
                                         "\nOver-Replication Price = " + over_digitalPrice);
                        }
                        // Floating Rate Coupon + Put Digital option
                        var sub_cash_longDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                        Position.Type.Long, false, nullstrike, strike, Position.Type.Long, false, cashRate, subReplication);
                        var central_cash_longDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                            Position.Type.Long, false, nullstrike, strike, Position.Type.Long, false, cashRate, centralReplication);
                        var over_cash_longDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                         Position.Type.Long, false, nullstrike, strike, Position.Type.Long, false, cashRate, superReplication);
                        sub_cash_longDigitalPutCoupon.setPricer(pricer);
                        central_cash_longDigitalPutCoupon.setPricer(pricer);
                        over_cash_longDigitalPutCoupon.setPricer(pricer);
                        sub_digitalPrice = sub_cash_longDigitalPutCoupon.price(vars.termStructure);
                        central_digitalPrice = central_cash_longDigitalPutCoupon.price(vars.termStructure);
                        over_digitalPrice = over_cash_longDigitalPutCoupon.price(vars.termStructure);
                        if (sub_digitalPrice > central_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - sub_digitalPrice) > tolerance ||
                            central_digitalPrice > over_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - over_digitalPrice) > tolerance)
                        {
                            QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Put Digital option" +
                                         "\nVolatility = " + capletVolatility +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nSub-Replication Price = " + sub_digitalPrice +
                                         "\nCentral-Replication Price = " + central_digitalPrice +
                                         "\nOver-Replication Price = " + over_digitalPrice);
                        }

                        // Floating Rate Coupon - Put Digital option
                        var sub_cash_shortDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                         Position.Type.Long, false, nullstrike, strike, Position.Type.Short, false, cashRate, subReplication);
                        var central_cash_shortDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                             Position.Type.Long, false, nullstrike, strike, Position.Type.Short, false, cashRate, centralReplication);
                        var over_cash_shortDigitalPutCoupon = new DigitalCoupon(underlying, nullstrike,
                                                                                          Position.Type.Long, false, nullstrike, strike, Position.Type.Short, false, cashRate, superReplication);
                        sub_cash_shortDigitalPutCoupon.setPricer(pricer);
                        central_cash_shortDigitalPutCoupon.setPricer(pricer);
                        over_cash_shortDigitalPutCoupon.setPricer(pricer);
                        sub_digitalPrice = sub_cash_shortDigitalPutCoupon.price(vars.termStructure);
                        central_digitalPrice = central_cash_shortDigitalPutCoupon.price(vars.termStructure);
                        over_digitalPrice = over_cash_shortDigitalPutCoupon.price(vars.termStructure);
                        if (sub_digitalPrice > central_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - sub_digitalPrice) > tolerance ||
                            central_digitalPrice > over_digitalPrice &&
                             System.Math.Abs(central_digitalPrice - over_digitalPrice) > tolerance)
                        {
                            QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Call Digital option" +
                                         "\nVolatility = " + capletVolatility +
                                         "\nStrike = " + strike +
                                         "\nExercise = " + k + 1 + " years" +
                                         "\nSub-Replication Price = " + sub_digitalPrice +
                                         "\nCentral-Replication Price = " + central_digitalPrice +
                                         "\nOver-Replication Price = " + over_digitalPrice);
                        }
                    }
                }
            }
        }
    }
}
