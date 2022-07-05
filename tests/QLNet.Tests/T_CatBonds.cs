//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Cashflows;
using Xunit;
using Calendar = QLNet.Time.Calendar;
using QLNet.Instruments.Bonds;
using QLNet.Indexes;
using QLNet.Indexes.Ibor;
using QLNet.PricingEngines.Bond;
using QLNet.Time.Calendars;
using QLNet.Time;
using QLNet.Termstructures.Volatility.Optionlet;
using QLNet.Termstructures;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_CatBonds
    {
        static KeyValuePair<Date, double>[] data =
        {
         new KeyValuePair<Date, double>(new Date(1, Month.February, 2012), 100),
         new KeyValuePair<Date, double>(new Date(1, Month.July, 2013), 150),
         new KeyValuePair<Date, double>(new Date(5, Month.January, 2014), 50)
      };

        List<KeyValuePair<Date, double>> sampleEvents = new List<KeyValuePair<Date, double>>(data);

        Date eventsStart = new Date(1, Month.January, 2011);
        Date eventsEnd = new Date(31, Month.December, 2014);

        private class CommonVars
        {
            // common data
            public Calendar calendar;
            public Date today;
            public double faceAmount;

            // setup
            public CommonVars()
            {
                calendar = new TARGET();
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                faceAmount = 1000000.0;
            }
        }

        [Fact]
        public void testEventSetForWholeYears()
        {
            // Testing that catastrophe events are split correctly for periods of whole years

            var catRisk = new EventSet(sampleEvents, eventsStart, eventsEnd);
            var simulation = catRisk.newSimulation(new Date(1, Month.January, 2015), new Date(31, Month.December, 2015));

            QAssert.Require(simulation);

            var path = new List<KeyValuePair<Date, double>>();

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(0, path.Count);

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(1, path.Count);
            QAssert.AreEqual(new Date(1, Month.February, 2015), path[0].Key);
            QAssert.AreEqual(100, path[0].Value);

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(1, path.Count);
            QAssert.AreEqual(new Date(1, Month.July, 2015), path[0].Key);
            QAssert.AreEqual(150, path[0].Value);

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(1, path.Count);
            QAssert.AreEqual(new Date(5, Month.January, 2015), path[0].Key);
            QAssert.AreEqual(50, path[0].Value);

            QAssert.Require(!simulation.nextPath(path));
        }

        [Fact]
        public void testEventSetForIrregularPeriods()
        {
            // Testing that catastrophe events are split correctly for irregular periods

            var catRisk = new EventSet(sampleEvents, eventsStart, eventsEnd);
            var simulation = catRisk.newSimulation(new Date(2, Month.January, 2015), new Date(5, Month.January, 2016));

            QAssert.Require(simulation);

            var path = new List<KeyValuePair<Date, double>>();

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(0, path.Count);

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(2, path.Count);
            QAssert.AreEqual(new Date(1, Month.July, 2015), path[0].Key);
            QAssert.AreEqual(150, path[0].Value);
            QAssert.AreEqual(new Date(5, Month.January, 2016), path[1].Key);
            QAssert.AreEqual(50, path[1].Value);

            QAssert.Require(!simulation.nextPath(path));
        }

        [Fact]
        public void testEventSetForNoEvents()
        {
            // Testing that catastrophe events are split correctly when there are no simulated events

            var emptyEvents = new List<KeyValuePair<Date, double>>();
            var catRisk = new EventSet(emptyEvents, eventsStart, eventsEnd);
            var simulation = catRisk.newSimulation(new Date(2, Month.January, 2015), new Date(5, Month.January, 2016));

            QAssert.Require(simulation);

            var path = new List<KeyValuePair<Date, double>>();

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(0, path.Count);

            QAssert.Require(simulation.nextPath(path));
            QAssert.AreEqual(0, path.Count);

            QAssert.Require(!simulation.nextPath(path));
        }

        [Fact]
        public void testRiskFreeAgainstFloatingRateBond()
        {
            // Testing floating-rate cat bond against risk-free floating-rate bond

            var vars = new CommonVars();

            var today = new Date(22, Month.November, 2004);
            Settings.setEvaluationDate(today);

            var settlementDays = 1;

            var riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
            var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

            IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
            var fixingDays = 1;

            var tolerance = 1.0e-6;

            IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

            // plain

            var sch = new Schedule(new Date(30, Month.November, 2004),
                                        new Date(30, Month.November, 2008),
                                        new Period(Frequency.Semiannual),
                                        new UnitedStates(UnitedStates.Market.GovernmentBond),
                                        BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                        DateGeneration.Rule.Backward, false);

            CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double>>(), new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

            EventPaymentOffset paymentOffset = new NoOffset();
            NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

            var bond1 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                          index, new ActualActual(ActualActual.Convention.ISMA),
                                                          BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                          new List<double>(), new List<double>(),
                                                          new List<double?>(), new List<double?>(),
                                                          false,
                                                          100.0, new Date(30, Month.November, 2004));

            var catBond1 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                           index, new ActualActual(ActualActual.Convention.ISMA),
                                                           notionalRisk,
                                                           BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                           new List<double>(), new List<double>(),
                                                           new List<double?>(), new List<double?>(),
                                                           false,
                                                           100.0, new Date(30, Month.November, 2004));

            IPricingEngine bondEngine = new DiscountingBondEngine(riskFreeRate);
            bond1.setPricingEngine(bondEngine);
            Cashflows.Utils.setCouponPricer(bond1.cashflows(), pricer);

            IPricingEngine catBondEngine = new MonteCarloCatBondEngine(noCatRisk, riskFreeRate);
            catBond1.setPricingEngine(catBondEngine);
            Cashflows.Utils.setCouponPricer(catBond1.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice1 = 99.874645;
#else
            var cachedPrice1 = 99.874646;
#endif


            var price = bond1.cleanPrice();
            var catPrice = catBond1.cleanPrice();
            if (System.Math.Abs(price - cachedPrice1) > tolerance || System.Math.Abs(catPrice - price) > tolerance)
            {
                QAssert.Fail("failed to reproduce floating rate bond price:\n"
                             + "    floating bond: " + price + "\n"
                             + "    catBond bond: " + catPrice + "\n"
                             + "    expected:   " + cachedPrice1 + "\n"
                             + "    error:      " + (catPrice - price));
            }



            // different risk-free and discount curve

            var bond2 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                          index, new ActualActual(ActualActual.Convention.ISMA),
                                                          BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                          new List<double>(), new List<double>(),
                                                          new List<double?>(), new List<double?>(),
                                                          false,
                                                          100.0, new Date(30, Month.November, 2004));

            var catBond2 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                           index, new ActualActual(ActualActual.Convention.ISMA),
                                                           notionalRisk,
                                                           BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                           new List<double>(), new List<double>(),
                                                           new List<double?>(), new List<double?>(),
                                                           false,
                                                           100.0, new Date(30, Month.November, 2004));

            IPricingEngine bondEngine2 = new DiscountingBondEngine(discountCurve);
            bond2.setPricingEngine(bondEngine2);
            Cashflows.Utils.setCouponPricer(bond2.cashflows(), pricer);

            IPricingEngine catBondEngine2 = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
            catBond2.setPricingEngine(catBondEngine2);
            Cashflows.Utils.setCouponPricer(catBond2.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice2 = 97.955904;
#else
            var cachedPrice2 = 97.955904;
#endif

            price = bond2.cleanPrice();
            catPrice = catBond2.cleanPrice();
            if (System.Math.Abs(price - cachedPrice2) > tolerance || System.Math.Abs(catPrice - price) > tolerance)
            {
                QAssert.Fail("failed to reproduce floating rate bond price:\n"
                             + "    floating bond: " + price + "\n"
                             + "    catBond bond: " + catPrice + "\n"
                             + "    expected:   " + cachedPrice2 + "\n"
                             + "    error:      " + (catPrice - price));
            }

            // varying spread

            List<double> spreads = new InitializedList<double>(4);
            spreads[0] = 0.001;
            spreads[1] = 0.0012;
            spreads[2] = 0.0014;
            spreads[3] = 0.0016;

            var bond3 = new FloatingRateBond(settlementDays, vars.faceAmount, sch,
                                                          index, new ActualActual(ActualActual.Convention.ISMA),
                                                          BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                          new List<double>(), spreads,
                                                          new List<double?>(), new List<double?>(),
                                                          false,
                                                          100.0, new Date(30, Month.November, 2004));

            var catBond3 = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                           index, new ActualActual(ActualActual.Convention.ISMA),
                                                           notionalRisk,
                                                           BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                           new List<double>(), spreads,
                                                           new List<double?>(), new List<double?>(),
                                                           false,
                                                           100.0, new Date(30, Month.November, 2004));

            bond3.setPricingEngine(bondEngine2);
            Cashflows.Utils.setCouponPricer(bond3.cashflows(), pricer);

            catBond3.setPricingEngine(catBondEngine2);
            Cashflows.Utils.setCouponPricer(catBond3.cashflows(), pricer);

#if QL_USE_INDEXED_COUPON
         double cachedPrice3 = 98.495458;
#else
            var cachedPrice3 = 98.495459;
#endif

            price = bond3.cleanPrice();
            catPrice = catBond3.cleanPrice();
            if (System.Math.Abs(price - cachedPrice3) > tolerance || System.Math.Abs(catPrice - price) > tolerance)
            {
                QAssert.Fail("failed to reproduce floating rate bond price:\n"
                             + "    floating bond: " + price + "\n"
                             + "    catBond bond: " + catPrice + "\n"
                             + "    expected:   " + cachedPrice2 + "\n"
                             + "    error:      " + (catPrice - price));
            }
        }

        [Fact]
        public void testCatBondInDoomScenario()
        {
            // Testing floating-rate cat bond in a doom scenario (certain default)

            var vars = new CommonVars();

            var today = new Date(22, Month.November, 2004);
            Settings.setEvaluationDate(today);

            var settlementDays = 1;

            var riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
            var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

            IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
            var fixingDays = 1;

            var tolerance = 1.0e-6;

            IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

            var sch = new Schedule(new Date(30, Month.November, 2004),
                                        new Date(30, Month.November, 2008),
                                        new Period(Frequency.Semiannual),
                                        new UnitedStates(UnitedStates.Market.GovernmentBond),
                                        BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                        DateGeneration.Rule.Backward, false);

            var events = new List<KeyValuePair<Date, double>>();
            events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2004), 1000));
            CatRisk doomCatRisk = new EventSet(events,
                                               new Date(30, Month.November, 2004), new Date(30, Month.November, 2008));

            EventPaymentOffset paymentOffset = new NoOffset();
            NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

            var catBond = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                          index, new ActualActual(ActualActual.Convention.ISMA),
                                                          notionalRisk,
                                                          BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                          new List<double>(), new List<double>(),
                                                          new List<double?>(), new List<double?>(),
                                                          false,
                                                          100.0, new Date(30, Month.November, 2004));

            IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
            catBond.setPricingEngine(catBondEngine);
            Cashflows.Utils.setCouponPricer(catBond.cashflows(), pricer);

            var price = catBond.cleanPrice();
            QAssert.AreEqual(0, price);

            var lossProbability = catBond.lossProbability();
            var exhaustionProbability = catBond.exhaustionProbability();
            var expectedLoss = catBond.expectedLoss();

            QAssert.AreEqual(1.0, lossProbability, tolerance);
            QAssert.AreEqual(1.0, exhaustionProbability, tolerance);
            QAssert.AreEqual(1.0, expectedLoss, tolerance);
        }

        [Fact]
        public void testCatBondWithDoomOnceInTenYears()
        {
            // Testing floating-rate cat bond in a doom once in 10 years scenario
            var vars = new CommonVars();

            var today = new Date(22, Month.November, 2004);
            Settings.setEvaluationDate(today);

            var settlementDays = 1;

            var riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
            var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

            IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
            var fixingDays = 1;

            var tolerance = 1.0e-6;

            IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

            var sch = new Schedule(new Date(30, Month.November, 2004),
                                        new Date(30, Month.November, 2008),
                                        new Period(Frequency.Semiannual),
                                        new UnitedStates(UnitedStates.Market.GovernmentBond),
                                        BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                        DateGeneration.Rule.Backward, false);

            var events = new List<KeyValuePair<Date, double>>();
            events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2008), 1000));
            CatRisk doomCatRisk = new EventSet(events, new Date(30, Month.November, 2004), new Date(30, Month.November, 2044));

            CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double>>(),
                                             new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

            EventPaymentOffset paymentOffset = new NoOffset();
            NotionalRisk notionalRisk = new DigitalNotionalRisk(paymentOffset, 100);

            var catBond = new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                                          index, new ActualActual(ActualActual.Convention.ISMA),
                                                          notionalRisk,
                                                          BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                          new List<double>(), new List<double>(),
                                                          new List<double?>(), new List<double?>(),
                                                          false,
                                                          100.0, new Date(30, Month.November, 2004));

            IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
            catBond.setPricingEngine(catBondEngine);
            Cashflows.Utils.setCouponPricer(catBond.cashflows(), pricer);

            var price = catBond.cleanPrice();
            var yield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
            var lossProbability = catBond.lossProbability();
            var exhaustionProbability = catBond.exhaustionProbability();
            var expectedLoss = catBond.expectedLoss();

            QAssert.AreEqual(0.1, lossProbability, tolerance);
            QAssert.AreEqual(0.1, exhaustionProbability, tolerance);
            QAssert.AreEqual(0.1, expectedLoss, tolerance);

            IPricingEngine catBondEngineRF = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
            catBond.setPricingEngine(catBondEngineRF);

            var riskFreePrice = catBond.cleanPrice();
            var riskFreeYield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
            var riskFreeLossProbability = catBond.lossProbability();
            var riskFreeExhaustionProbability = catBond.exhaustionProbability();
            var riskFreeExpectedLoss = catBond.expectedLoss();

            QAssert.AreEqual(0.0, riskFreeLossProbability, tolerance);
            QAssert.AreEqual(0.0, riskFreeExhaustionProbability, tolerance);
            QAssert.IsTrue(System.Math.Abs(riskFreeExpectedLoss) < tolerance);

            QAssert.AreEqual(riskFreePrice * 0.9, price, tolerance);
            QAssert.IsTrue(riskFreeYield < yield);
        }

        [Fact]
        public void testCatBondWithDoomOnceInTenYearsProportional()
        {
            // Testing floating-rate cat bond in a doom once in 10 years scenario with proportional notional reduction

            var vars = new CommonVars();

            var today = new Date(22, Month.November, 2004);
            Settings.setEvaluationDate(today);

            var settlementDays = 1;

            var riskFreeRate = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.025, new Actual360()));
            var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(today, 0.03, new Actual360()));

            IborIndex index = new USDLibor(new Period(6, TimeUnit.Months), riskFreeRate);
            var fixingDays = 1;

            var tolerance = 1.0e-6;

            IborCouponPricer pricer = new BlackIborCouponPricer(new Handle<OptionletVolatilityStructure>());

            var sch =
               new Schedule(new Date(30, Month.November, 2004),
                            new Date(30, Month.November, 2008),
                            new Period(Frequency.Semiannual),
                            new UnitedStates(UnitedStates.Market.GovernmentBond),
                            BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                            DateGeneration.Rule.Backward, false);

            var events = new List<KeyValuePair<Date, double>>();
            events.Add(new KeyValuePair<Date, double>(new Date(30, Month.November, 2008), 1000));
            CatRisk doomCatRisk = new EventSet(events, new Date(30, Month.November, 2004), new Date(30, Month.November, 2044));

            CatRisk noCatRisk = new EventSet(new List<KeyValuePair<Date, double>>(),
                                             new Date(1, Month.Jan, 2000), new Date(31, Month.Dec, 2010));

            EventPaymentOffset paymentOffset = new NoOffset();
            NotionalRisk notionalRisk = new ProportionalNotionalRisk(paymentOffset, 500, 1500);

            var catBond =
               new FloatingCatBond(settlementDays, vars.faceAmount, sch,
                                   index, new ActualActual(ActualActual.Convention.ISMA),
                                   notionalRisk,
                                   BusinessDayConvention.ModifiedFollowing, fixingDays,
                                   new List<double>(), new List<double>(),
                                   new List<double?>(), new List<double?>(),
                                   false,
                                   100.0, new Date(30, Month.November, 2004));

            IPricingEngine catBondEngine = new MonteCarloCatBondEngine(doomCatRisk, discountCurve);
            catBond.setPricingEngine(catBondEngine);
            Cashflows.Utils.setCouponPricer(catBond.cashflows(), pricer);

            var price = catBond.cleanPrice();
            var yield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
            var lossProbability = catBond.lossProbability();
            var exhaustionProbability = catBond.exhaustionProbability();
            var expectedLoss = catBond.expectedLoss();

            QAssert.AreEqual(0.1, lossProbability, tolerance);
            QAssert.AreEqual(0.0, exhaustionProbability, tolerance);
            QAssert.AreEqual(0.05, expectedLoss, tolerance);

            IPricingEngine catBondEngineRF = new MonteCarloCatBondEngine(noCatRisk, discountCurve);
            catBond.setPricingEngine(catBondEngineRF);

            var riskFreePrice = catBond.cleanPrice();
            var riskFreeYield = catBond.yield(new ActualActual(ActualActual.Convention.ISMA), Compounding.Simple, Frequency.Annual);
            var riskFreeLossProbability = catBond.lossProbability();
            var riskFreeExpectedLoss = catBond.expectedLoss();

            QAssert.AreEqual(0.0, riskFreeLossProbability, tolerance);
            QAssert.IsTrue(System.Math.Abs(riskFreeExpectedLoss) < tolerance);

            QAssert.AreEqual(riskFreePrice * 0.95, price, tolerance);
            QAssert.IsTrue(riskFreeYield < yield);
        }
    }
}
