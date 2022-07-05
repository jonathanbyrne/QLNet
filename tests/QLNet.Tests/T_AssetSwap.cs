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
using System;
using System.Collections.Generic;
using Xunit;
using QLNet.Instruments;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Instruments.Bonds;
using QLNet.Time;
using QLNet.Pricingengines.Bond;
using QLNet.Pricingengines.Swap;
using QLNet.Time.DayCounters;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Termstructures;
using QLNet.Quotes;
using QLNet.Time.Calendars;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_AssetSwap : IDisposable
    {
        #region Initialize&Cleanup
        private SavedSettings backup;
        private IndexHistoryCleaner cleaner;

        public T_AssetSwap()
        {

            backup = new SavedSettings();
            cleaner = new IndexHistoryCleaner();
        }

        public void Dispose()
        {
            backup.Dispose();
            cleaner.Dispose();
        }
        #endregion

        class CommonVars
        {
            // common data
            public IborIndex iborIndex;
            public SwapIndex swapIndex;
            public IborCouponPricer pricer;
            public CmsCouponPricer cmspricer;
            public double spread;
            public double nonnullspread;
            public double faceAmount;
            public Compounding compounding;
            public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

            // initial setup
            public CommonVars()
            {
                termStructure = new RelinkableHandle<YieldTermStructure>();
                var swapSettlementDays = 2;
                faceAmount = 100.0;
                var fixedConvention = BusinessDayConvention.Unadjusted;
                compounding = Compounding.Continuous;
                var fixedFrequency = Frequency.Annual;
                var floatingFrequency = Frequency.Semiannual;
                iborIndex = new Euribor(new Period(floatingFrequency), termStructure);
                var calendar = iborIndex.fixingCalendar();
                swapIndex = new SwapIndex("EuriborSwapIsdaFixA", new Period(10, TimeUnit.Years), swapSettlementDays,
                                           iborIndex.currency(), calendar,
                                           new Period(fixedFrequency), fixedConvention,
                                           iborIndex.dayCounter(), iborIndex);
                spread = 0.0;
                nonnullspread = 0.003;
                var today = new Date(24, Month.April, 2007);
                Settings.setEvaluationDate(today);

                //Date today = Settings::instance().evaluationDate();
                termStructure.linkTo(Utilities.flatRate(today, 0.05, new Actual365Fixed()));

                pricer = new BlackIborCouponPricer();
                var swaptionVolatilityStructure =
                   new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(today,
                                                                                          new NullCalendar(), BusinessDayConvention.Following, 0.2, new Actual365Fixed()));

                var meanReversionQuote = new Handle<Quote>(new SimpleQuote(0.01));
                cmspricer = new AnalyticHaganPricer(swaptionVolatilityStructure, GFunctionFactory.YieldCurveModel.Standard, meanReversionQuote);
            }
        }

        [Fact]
        public void testConsistency()
        {

            // Testing consistency between fair price and fair spread...");
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var bondSchedule = new Schedule(new Date(4, Month.January, 2005),
                                                 new Date(4, Month.January, 2037),
                                                 new Period(Frequency.Annual), bondCalendar,
                                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                 DateGeneration.Rule.Backward, false);
            Bond bond = new FixedRateBond(settlementDays, vars.faceAmount,
            bondSchedule, new List<double>() { 0.04 },
            new ActualActual(ActualActual.Convention.ISDA),
            BusinessDayConvention.Following,
            100.0, new Date(4, Month.January, 2005));

            var payFixedRate = true;
            var bondPrice = 95.0;

            var isPar = true;

            var parAssetSwap = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, vars.spread,
                                                   null, vars.iborIndex.dayCounter(), isPar);

            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(),
                                                                  Settings.evaluationDate());

            parAssetSwap.setPricingEngine(swapEngine);
            var fairCleanPrice = parAssetSwap.fairCleanPrice();
            var fairSpread = parAssetSwap.fairSpread();

            var tolerance = 1.0e-13;

            var assetSwap2 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                                 null, vars.iborIndex.dayCounter(), isPar);

            assetSwap2.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap2.NPV()) > tolerance)
            {
                QAssert.Fail("npar asset swap fair clean price doesn't zero the NPV: " +
                             "\n  clean price:      " + bondPrice +
                             "\n  fair clean price: " + fairCleanPrice +
                             "\n  NPV:              " + assetSwap2.NPV() +
                             "\n  tolerance:        " + tolerance);
            }
            if (System.Math.Abs(assetSwap2.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + fairCleanPrice +
                             "\n  fair clean price:  " + assetSwap2.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap2.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap2.fairSpread() - vars.spread) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + vars.spread +
                             "\n  fair spread:  " + assetSwap2.fairSpread() +
                             "\n  NPV:          " + assetSwap2.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            var assetSwap3 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                                 null, vars.iborIndex.dayCounter(), isPar);

            assetSwap3.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap3.NPV()) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't zero the NPV: " +
                             "\n  spread:      " + vars.spread +
                             "\n  fair spread: " + fairSpread +
                             "\n  NPV:         " + assetSwap3.NPV() +
                             "\n  tolerance:   " + tolerance);
            }
            if (System.Math.Abs(assetSwap3.fairCleanPrice() - bondPrice) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + bondPrice +
                             "\n  fair clean price:  " + assetSwap3.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap3.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap3.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't equal input spread at  zero NPV: " +
                             "\n  input spread: " + fairSpread +
                             "\n  fair spread:  " + assetSwap3.fairSpread() +
                             "\n  NPV:          " + assetSwap3.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            // let's change the npv date
            swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(), bond.settlementDate());

            parAssetSwap.setPricingEngine(swapEngine);
            // fair clean price and fair spread should not change
            if (System.Math.Abs(parAssetSwap.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price changed with NpvDate:" +
                             "\n expected clean price: " + fairCleanPrice +
                             "\n fair clean price:     " + parAssetSwap.fairCleanPrice() +
                             "\n tolerance:            " + tolerance);
            }
            if (System.Math.Abs(parAssetSwap.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread changed with NpvDate:" +
                             "\n  expected spread: " + fairSpread +
                             "\n  fair spread:     " + parAssetSwap.fairSpread() +
                             "\n  tolerance:       " + tolerance);
            }

            assetSwap2 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                       null, vars.iborIndex.dayCounter(), isPar);
            assetSwap2.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap2.NPV()) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price doesn't zero the NPV: " +
                             "\n  clean price:      " + bondPrice +
                             "\n  fair clean price: " + fairCleanPrice +
                             "\n  NPV:              " + assetSwap2.NPV() +
                             "\n  tolerance:        " + tolerance);
            }
            if (System.Math.Abs(assetSwap2.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + fairCleanPrice +
                             "\n  fair clean price:  " + assetSwap2.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap2.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap2.fairSpread() - vars.spread) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + vars.spread +
                             "\n  fair spread:  " + assetSwap2.fairSpread() +
                             "\n  NPV:          " + assetSwap2.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            assetSwap3 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                       null, vars.iborIndex.dayCounter(), isPar);
            assetSwap3.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap3.NPV()) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't zero the NPV: " +
                             "\n  spread:      " + vars.spread +
                             "\n  fair spread: " + fairSpread +
                             "\n  NPV:         " + assetSwap3.NPV() +
                             "\n  tolerance:   " + tolerance);
            }
            if (System.Math.Abs(assetSwap3.fairCleanPrice() - bondPrice) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + bondPrice +
                             "\n  fair clean price:  " + assetSwap3.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap3.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap3.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + fairSpread +
                             "\n  fair spread:  " + assetSwap3.fairSpread() +
                             "\n  NPV:          " + assetSwap3.NPV() +
                             "\n  tolerance:    " + tolerance);

            }

            // now market asset swap
            isPar = false;
            var mktAssetSwap = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, vars.spread,
                                                   null, vars.iborIndex.dayCounter(), isPar);

            swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(),
                                                   Settings.evaluationDate());

            mktAssetSwap.setPricingEngine(swapEngine);
            fairCleanPrice = mktAssetSwap.fairCleanPrice();
            fairSpread = mktAssetSwap.fairSpread();

            var assetSwap4 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                                 null, vars.iborIndex.dayCounter(), isPar);
            assetSwap4.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap4.NPV()) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't zero the NPV: " +
                             "\n  clean price:      " + bondPrice +
                             "\n  fair clean price: " + fairCleanPrice +
                             "\n  NPV:              " + assetSwap4.NPV() +
                             "\n  tolerance:        " + tolerance);
            }
            if (System.Math.Abs(assetSwap4.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + fairCleanPrice +
                             "\n  fair clean price:  " + assetSwap4.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap4.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap4.fairSpread() - vars.spread) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + vars.spread +
                             "\n  fair spread:  " + assetSwap4.fairSpread() +
                             "\n  NPV:          " + assetSwap4.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            var assetSwap5 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                                 null, vars.iborIndex.dayCounter(), isPar);
            assetSwap5.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap5.NPV()) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't zero the NPV: " +
                             "\n  spread:      " + vars.spread +
                             "\n  fair spread: " + fairSpread +
                             "\n  NPV:         " + assetSwap5.NPV() +
                             "\n  tolerance:   " + tolerance);
            }
            if (System.Math.Abs(assetSwap5.fairCleanPrice() - bondPrice) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + bondPrice +
                             "\n  fair clean price:  " + assetSwap5.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap5.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap5.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + fairSpread +
                             "\n  fair spread:  " + assetSwap5.fairSpread() +
                             "\n  NPV:          " + assetSwap5.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            // let's change the npv date
            swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(), bond.settlementDate());
            mktAssetSwap.setPricingEngine(swapEngine);

            // fair clean price and fair spread should not change
            if (System.Math.Abs(mktAssetSwap.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price changed with NpvDate:" +
                             "\n  expected clean price: " + fairCleanPrice +
                             "\n  fair clean price:  " + mktAssetSwap.fairCleanPrice() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(mktAssetSwap.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread changed with NpvDate:" +
                             "\n  expected spread: " + fairSpread +
                             "\n  fair spread:  " + mktAssetSwap.fairSpread() +
                             "\n  tolerance:    " + tolerance);
            }

            assetSwap4 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                       null, vars.iborIndex.dayCounter(), isPar);
            assetSwap4.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap4.NPV()) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't zero the NPV: " +
                             "\n  clean price:      " + bondPrice +
                             "\n  fair clean price: " + fairCleanPrice +
                             "\n  NPV:              " + assetSwap4.NPV() +
                             "\n  tolerance:        " + tolerance);
            }
            if (System.Math.Abs(assetSwap4.fairCleanPrice() - fairCleanPrice) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + fairCleanPrice +
                             "\n  fair clean price:  " + assetSwap4.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap4.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap4.fairSpread() - vars.spread) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + vars.spread +
                             "\n  fair spread:  " + assetSwap4.fairSpread() +
                             "\n  NPV:          " + assetSwap4.NPV() +
                             "\n  tolerance:    " + tolerance);
            }

            assetSwap5 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                       null, vars.iborIndex.dayCounter(), isPar);
            assetSwap5.setPricingEngine(swapEngine);
            if (System.Math.Abs(assetSwap5.NPV()) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't zero the NPV: " +
                             "\n  spread:      " + vars.spread +
                             "\n  fair spread: " + fairSpread +
                             "\n  NPV:         " + assetSwap5.NPV() +
                             "\n  tolerance:   " + tolerance);
            }
            if (System.Math.Abs(assetSwap5.fairCleanPrice() - bondPrice) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                             "\n  input clean price: " + bondPrice +
                             "\n  fair clean price:  " + assetSwap5.fairCleanPrice() +
                             "\n  NPV:               " + assetSwap5.NPV() +
                             "\n  tolerance:         " + tolerance);
            }
            if (System.Math.Abs(assetSwap5.fairSpread() - fairSpread) > tolerance)
            {
                QAssert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                             "\n  input spread: " + fairSpread +
                             "\n  fair spread:  " + assetSwap5.fairSpread() +
                             "\n  NPV:          " + assetSwap5.NPV() +
                             "\n  tolerance:    " + tolerance);
            }
        }

        [Fact]
        public void testImpliedValue()
        {
            // Testing implied bond value against asset-swap fair price with null spread
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var payFixedRate = true;
            var parAssetSwap = true;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var fixedBondSchedule1 = new Schedule(new Date(4, Month.January, 2005),
                                                       new Date(4, Month.January, 2037),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount,
                                                fixedBondSchedule1,
            new List<double>() { 0.04 },
            new ActualActual(ActualActual.Convention.ISDA),
            BusinessDayConvention.Following,
            100.0, new Date(4, Month.January, 2005));

            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondPrice1 = fixedBond1.cleanPrice();
            var fixedBondAssetSwap1 = new AssetSwap(payFixedRate, fixedBond1, fixedBondPrice1, vars.iborIndex, vars.spread,
                                                          null, vars.iborIndex.dayCounter(), parAssetSwap);
            fixedBondAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondAssetSwapPrice1 - fixedBondPrice1);

            if (error1 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for fixed bond:" +
                             "\n  bond's clean price:    " + fixedBondPrice1 +
                             "\n  asset swap fair price: " + fixedBondAssetSwapPrice1 +
                             "\n  error:                 " + error1 +
                             "\n  tolerance:             " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day

            var fixedBondSchedule2 = new Schedule(new Date(5, Month.February, 2005),
                                                       new Date(5, Month.February, 2019),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount,
                                                fixedBondSchedule2,
            new List<double>() { 0.05 },
            new Thirty360(Thirty360.Thirty360Convention.BondBasis),
            BusinessDayConvention.Following,
            100.0, new Date(5, Month.February, 2005));

            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondPrice2 = fixedBond2.cleanPrice();
            var fixedBondAssetSwap2 = new AssetSwap(payFixedRate, fixedBond2, fixedBondPrice2, vars.iborIndex, vars.spread,
                                                          null, vars.iborIndex.dayCounter(), parAssetSwap);
            fixedBondAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
            var error2 = System.Math.Abs(fixedBondAssetSwapPrice2 - fixedBondPrice2);

            if (error2 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for fixed bond:" +
                             "\n  bond's clean price:    " + fixedBondPrice2 +
                             "\n  asset swap fair price: " + fixedBondAssetSwapPrice2 +
                             "\n  error:                 " + error2 +
                             "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day

            var floatingBondSchedule1 = new Schedule(new Date(29, Month.September, 2003),
                                                          new Date(29, Month.September, 2013),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);

            Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule1,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.Following, fixingDays,
            new List<double>() { 1 },
            new List<double>() { 0.0056 },
            new List<double?>(),
            new List<double?>(),
            inArrears,
            100.0, new Date(29, Month.September, 2003));

            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondPrice1 = floatingBond1.cleanPrice();
            var floatingBondAssetSwap1 = new AssetSwap(payFixedRate, floatingBond1, floatingBondPrice1, vars.iborIndex, vars.spread,
                                                             null, vars.iborIndex.dayCounter(), parAssetSwap);
            floatingBondAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
            var error3 = System.Math.Abs(floatingBondAssetSwapPrice1 - floatingBondPrice1);

            if (error3 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for floater:" +
                             "\n  bond's clean price:    " + floatingBondPrice1 +
                             "\n  asset swap fair price: " + floatingBondAssetSwapPrice1 +
                             "\n  error:                 " + error3 +
                             "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day

            var floatingBondSchedule2 = new Schedule(new Date(24, Month.September, 2004),
                                                          new Date(24, Month.September, 2018),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            Bond floatingBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule2,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.ModifiedFollowing, fixingDays,
            new List<double>() { 1 },
            new List<double>() { 0.0025 },
            new List<double?>(),
            new List<double?>(),
            inArrears,
            100.0, new Date(24, Month.September, 2004));

            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            var currentCoupon = 0.04013 + 0.0025;
            var floatingCurrentCoupon = floatingBond2.nextCouponRate();
            var error4 = System.Math.Abs(floatingCurrentCoupon - currentCoupon);
            if (error4 > tolerance)
            {
                QAssert.Fail("wrong current coupon is returned for floater bond:" +
                             "\n  bond's calculated current coupon:      " +
                             currentCoupon +
                             "\n  current coupon asked to the bond: " +
                             floatingCurrentCoupon +
                             "\n  error:                 " + error4 +
                             "\n  tolerance:             " + tolerance);
            }

            var floatingBondPrice2 = floatingBond2.cleanPrice();
            var floatingBondAssetSwap2 = new AssetSwap(payFixedRate, floatingBond2, floatingBondPrice2, vars.iborIndex, vars.spread,
                                                             null, vars.iborIndex.dayCounter(), parAssetSwap);
            floatingBondAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
            var error5 = System.Math.Abs(floatingBondAssetSwapPrice2 - floatingBondPrice2);

            if (error5 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for floater:" +
                             "\n  bond's clean price:    " + floatingBondPrice2 +
                             "\n  asset swap fair price: " + floatingBondAssetSwapPrice2 +
                             "\n  error:                 " + error5 +
                             "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day

            var cmsBondSchedule1 = new Schedule(new Date(22, Month.August, 2005),
                                                     new Date(22, Month.August, 2020),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount,
                                            cmsBondSchedule1,
                                            vars.swapIndex, new Thirty360(),
                                            BusinessDayConvention.Following, fixingDays,
            new List<double>() { 1.0 },
            new List<double>() { 0.0 },
            new List<double?>() { 0.055 },
            new List<double?>() { 0.025 },
            inArrears,
            100.0, new Date(22, Month.August, 2005));

            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondPrice1 = cmsBond1.cleanPrice();
            var cmsBondAssetSwap1 = new AssetSwap(payFixedRate, cmsBond1, cmsBondPrice1, vars.iborIndex, vars.spread,
                                                        null, vars.iborIndex.dayCounter(), parAssetSwap);
            cmsBondAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
            var error6 = System.Math.Abs(cmsBondAssetSwapPrice1 - cmsBondPrice1);

            if (error6 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for cms bond:" +
                             "\n  bond's clean price:    " + cmsBondPrice1 +
                             "\n  asset swap fair price: " + cmsBondAssetSwapPrice1 +
                             "\n  error:                 " + error6 +
                             "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day

            var cmsBondSchedule2 = new Schedule(new Date(06, Month.May, 2005),
                                                     new Date(06, Month.May, 2015),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                                            vars.swapIndex, new Thirty360(),
                                            BusinessDayConvention.Following, fixingDays,
            new List<double>() { 0.84 }, new List<double>() { 0.0 },
            new List<double?>(), new List<double?>(),
            inArrears,
            100.0, new Date(06, Month.May, 2005));

            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondPrice2 = cmsBond2.cleanPrice();
            var cmsBondAssetSwap2 = new AssetSwap(payFixedRate, cmsBond2, cmsBondPrice2, vars.iborIndex, vars.spread,
                                                        null, vars.iborIndex.dayCounter(), parAssetSwap);
            cmsBondAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
            var error7 = System.Math.Abs(cmsBondAssetSwapPrice2 - cmsBondPrice2);

            if (error7 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for cms bond:" +
                             "\n  bond's clean price:    " + cmsBondPrice2 +
                             "\n  asset swap fair price: " + cmsBondAssetSwapPrice2 +
                             "\n  error:                 " + error7 +
                             "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day

            Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(20, Month.December, 2015),
                                                   BusinessDayConvention.Following,
                                                   100.0, new Date(19, Month.December, 1985));

            zeroCpnBond1.setPricingEngine(bondEngine);

            var zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
            var zeroCpnAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1, zeroCpnBondPrice1, vars.iborIndex, vars.spread,
                                                        null, vars.iborIndex.dayCounter(), parAssetSwap);
            zeroCpnAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice1 = zeroCpnAssetSwap1.fairCleanPrice();
            var error8 = System.Math.Abs(cmsBondAssetSwapPrice1 - cmsBondPrice1);

            if (error8 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for zero cpn bond:" +
                             "\n  bond's clean price:    " + zeroCpnBondPrice1 +
                             "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice1 +
                             "\n  error:                 " + error8 +
                             "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day

            Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(17, Month.February, 2028),
                                                   BusinessDayConvention.Following,
                                                   100.0, new Date(17, Month.February, 1998));

            zeroCpnBond2.setPricingEngine(bondEngine);

            var zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
            var zeroCpnAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2, zeroCpnBondPrice2, vars.iborIndex, vars.spread,
                                                        null, vars.iborIndex.dayCounter(), parAssetSwap);
            zeroCpnAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice2 = zeroCpnAssetSwap2.fairCleanPrice();
            var error9 = System.Math.Abs(cmsBondAssetSwapPrice2 - cmsBondPrice2);

            if (error9 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for zero cpn bond:" +
                             "\n  bond's clean price:      " + zeroCpnBondPrice2 +
                             "\n  asset swap fair price:   " + zeroCpnBondAssetSwapPrice2 +
                             "\n  error:                   " + error9 +
                             "\n  tolerance:               " + tolerance);
            }

        }

        [Fact]
        public void testMarketASWSpread()
        {
            // Testing relationship between market asset swap and par asset swap...
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var payFixedRate = true;
            var parAssetSwap = true;
            var mktAssetSwap = false;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var fixedBondSchedule1 = new Schedule(new Date(4, Month.January, 2005),
                                                       new Date(4, Month.January, 2037),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                                                new List<double> { 0.04 },
                                                new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                                                100.0, new Date(4, Month.January, 2005));

            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondMktPrice1 = 89.22; // market price observed on 7th June 2007
            var fixedBondMktFullPrice1 = fixedBondMktPrice1 + fixedBond1.accruedAmount();
            var fixedBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                             fixedBond1, fixedBondMktPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            fixedBondParAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondParAssetSwapSpread1 = fixedBondParAssetSwap1.fairSpread();
            var fixedBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                             fixedBond1, fixedBondMktPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             mktAssetSwap);
            fixedBondMktAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondMktAssetSwapSpread1 = fixedBondMktAssetSwap1.fairSpread();

            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondMktAssetSwapSpread1 - 100 * fixedBondParAssetSwapSpread1 / fixedBondMktFullPrice1);

            if (error1 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for fixed bond:" +
                             "\n  market ASW spread: " + fixedBondMktAssetSwapSpread1 +
                             "\n  par ASW spread:    " + fixedBondParAssetSwapSpread1 +
                             "\n  error:             " + error1 +
                             "\n  tolerance:         " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day

            var fixedBondSchedule2 = new Schedule(new Date(5, Month.February, 2005),
                                                       new Date(5, Month.February, 2019),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                                                new List<double> { 0.05 },
                                                new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                                                100.0, new Date(5, Month.February, 2005));

            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondMktPrice2 = 99.98; // market price observed on 7th June 2007
            var fixedBondMktFullPrice2 = fixedBondMktPrice2 + fixedBond2.accruedAmount();
            var fixedBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                             fixedBond2, fixedBondMktPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            fixedBondParAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondParAssetSwapSpread2 = fixedBondParAssetSwap2.fairSpread();
            var fixedBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                             fixedBond2, fixedBondMktPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             mktAssetSwap);
            fixedBondMktAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondMktAssetSwapSpread2 = fixedBondMktAssetSwap2.fairSpread();
            var error2 = System.Math.Abs(fixedBondMktAssetSwapSpread2 -
                                         100 * fixedBondParAssetSwapSpread2 / fixedBondMktFullPrice2);

            if (error2 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for fixed bond:" +
                             "\n  market ASW spread: " + fixedBondMktAssetSwapSpread2 +
                             "\n  par ASW spread:    " + fixedBondParAssetSwapSpread2 +
                             "\n  error:             " + error2 +
                             "\n  tolerance:         " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day

            var floatingBondSchedule1 = new Schedule(new Date(29, Month.September, 2003),
                                                          new Date(29, Month.September, 2013),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);

            Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule1,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.Following, fixingDays,
                                                      new List<double> { 1 }, new List<double> { 0.0056 },
                                                      new List<double?>(), new List<double?>(),
                                                      inArrears,
                                                      100.0, new Date(29, Month.September, 2003));

            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            // market price observed on 7th June 2007
            var floatingBondMktPrice1 = 101.64;
            var floatingBondMktFullPrice1 = floatingBondMktPrice1 + floatingBond1.accruedAmount();
            var floatingBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                                floatingBond1, floatingBondMktPrice1,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                parAssetSwap);
            floatingBondParAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondParAssetSwapSpread1 = floatingBondParAssetSwap1.fairSpread();
            var floatingBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                                floatingBond1, floatingBondMktPrice1,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                mktAssetSwap);
            floatingBondMktAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondMktAssetSwapSpread1 = floatingBondMktAssetSwap1.fairSpread();
            var error3 = System.Math.Abs(floatingBondMktAssetSwapSpread1 -
                                         100 * floatingBondParAssetSwapSpread1 / floatingBondMktFullPrice1);

            if (error3 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for floating bond:" +
                             "\n  market ASW spread: " + floatingBondMktAssetSwapSpread1 +
                             "\n  par ASW spread:    " + floatingBondParAssetSwapSpread1 +
                             "\n  error:             " + error3 +
                             "\n  tolerance:         " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day

            var floatingBondSchedule2 = new Schedule(new Date(24, Month.September, 2004),
                                                          new Date(24, Month.September, 2018),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            Bond floatingBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule2,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                      new List<double> { 1 }, new List<double> { 0.0025 },
                                                      new List<double?>(), new List<double?>(),
                                                      inArrears,
                                                      100.0, new Date(24, Month.September, 2004));

            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            // market price observed on 7th June 2007
            var floatingBondMktPrice2 = 101.248;
            var floatingBondMktFullPrice2 = floatingBondMktPrice2 + floatingBond2.accruedAmount();
            var floatingBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                                floatingBond2, floatingBondMktPrice2,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                parAssetSwap);
            floatingBondParAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondParAssetSwapSpread2 = floatingBondParAssetSwap2.fairSpread();
            var floatingBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                                floatingBond2, floatingBondMktPrice2,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                mktAssetSwap);
            floatingBondMktAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondMktAssetSwapSpread2 = floatingBondMktAssetSwap2.fairSpread();
            var error4 = System.Math.Abs(floatingBondMktAssetSwapSpread2 -
                                         100 * floatingBondParAssetSwapSpread2 / floatingBondMktFullPrice2);

            if (error4 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for floating bond:" +
                             "\n  market ASW spread: " + floatingBondMktAssetSwapSpread2 +
                             "\n  par ASW spread:    " + floatingBondParAssetSwapSpread2 +
                             "\n  error:             " + error4 +
                             "\n  tolerance:         " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day

            var cmsBondSchedule1 = new Schedule(new Date(22, Month.August, 2005),
                                                     new Date(22, Month.August, 2020),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                                            vars.swapIndex, new Thirty360(),
                                            BusinessDayConvention.Following, fixingDays,
                                            new List<double> { 1.0 }, new List<double> { 0.0 },
                                            new List<double?> { 0.055 }, new List<double?> { 0.025 },
                                            inArrears,
                                            100.0, new Date(22, Month.August, 2005));

            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondMktPrice1 = 88.45; // market price observed on 7th June 2007
            var cmsBondMktFullPrice1 = cmsBondMktPrice1 + cmsBond1.accruedAmount();
            var cmsBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                           cmsBond1, cmsBondMktPrice1,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           parAssetSwap);
            cmsBondParAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondParAssetSwapSpread1 = cmsBondParAssetSwap1.fairSpread();
            var cmsBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                           cmsBond1, cmsBondMktPrice1,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           mktAssetSwap);
            cmsBondMktAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondMktAssetSwapSpread1 = cmsBondMktAssetSwap1.fairSpread();
            var error5 = System.Math.Abs(cmsBondMktAssetSwapSpread1 -
                                         100 * cmsBondParAssetSwapSpread1 / cmsBondMktFullPrice1);

            if (error5 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for cms bond:" +
                             "\n  market ASW spread: " + cmsBondMktAssetSwapSpread1 +
                             "\n  par ASW spread:    " + cmsBondParAssetSwapSpread1 +
                             "\n  error:             " + error5 +
                             "\n  tolerance:         " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day

            var cmsBondSchedule2 = new Schedule(new Date(06, Month.May, 2005),
                                                     new Date(06, Month.May, 2015),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                                            vars.swapIndex, new Thirty360(),
                                            BusinessDayConvention.Following, fixingDays,
                                            new List<double> { 0.84 }, new List<double> { 0.0 },
                                            new List<double?>(), new List<double?>(),
                                            inArrears,
                                            100.0, new Date(06, Month.May, 2005));

            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondMktPrice2 = 94.08; // market price observed on 7th June 2007
            var cmsBondMktFullPrice2 = cmsBondMktPrice2 + cmsBond2.accruedAmount();
            var cmsBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                           cmsBond2, cmsBondMktPrice2,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           parAssetSwap);
            cmsBondParAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondParAssetSwapSpread2 = cmsBondParAssetSwap2.fairSpread();
            var cmsBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                           cmsBond2, cmsBondMktPrice2,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           mktAssetSwap);
            cmsBondMktAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondMktAssetSwapSpread2 = cmsBondMktAssetSwap2.fairSpread();
            var error6 = System.Math.Abs(cmsBondMktAssetSwapSpread2 -
                                         100 * cmsBondParAssetSwapSpread2 / cmsBondMktFullPrice2);

            if (error6 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for cms bond:" +
                             "\n  market ASW spread: " + cmsBondMktAssetSwapSpread2 +
                             "\n  par ASW spread:    " + cmsBondParAssetSwapSpread2 +
                             "\n  error:             " + error6 +
                             "\n  tolerance:         " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day

            Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(20, Month.December, 2015), BusinessDayConvention.Following,
                                                   100.0, new Date(19, Month.December, 1985));

            zeroCpnBond1.setPricingEngine(bondEngine);

            // market price observed on 12th June 2007
            var zeroCpnBondMktPrice1 = 70.436;
            var zeroCpnBondMktFullPrice1 = zeroCpnBondMktPrice1 + zeroCpnBond1.accruedAmount();
            var zeroCpnBondParAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1,
                                                               zeroCpnBondMktPrice1,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            zeroCpnBondParAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondParAssetSwapSpread1 = zeroCpnBondParAssetSwap1.fairSpread();
            var zeroCpnBondMktAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1,
                                                               zeroCpnBondMktPrice1,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               mktAssetSwap);
            zeroCpnBondMktAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondMktAssetSwapSpread1 = zeroCpnBondMktAssetSwap1.fairSpread();
            var error7 = System.Math.Abs(zeroCpnBondMktAssetSwapSpread1 -
                                         100 * zeroCpnBondParAssetSwapSpread1 / zeroCpnBondMktFullPrice1);

            if (error7 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for zero cpn bond:" +
                             "\n  market ASW spread: " + zeroCpnBondMktAssetSwapSpread1 +
                             "\n  par ASW spread:    " + zeroCpnBondParAssetSwapSpread1 +
                             "\n  error:             " + error7 +
                             "\n  tolerance:         " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day

            Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(17, Month.February, 2028),
                                                   BusinessDayConvention.Following,
                                                   100.0, new Date(17, Month.February, 1998));

            zeroCpnBond2.setPricingEngine(bondEngine);

            // Real zeroCpnBondPrice2 = zeroCpnBond2->cleanPrice();

            // market price observed on 12th June 2007
            var zeroCpnBondMktPrice2 = 35.160;
            var zeroCpnBondMktFullPrice2 = zeroCpnBondMktPrice2 + zeroCpnBond2.accruedAmount();
            var zeroCpnBondParAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2,
                                                               zeroCpnBondMktPrice2,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            zeroCpnBondParAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondParAssetSwapSpread2 = zeroCpnBondParAssetSwap2.fairSpread();
            var zeroCpnBondMktAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2,
                                                               zeroCpnBondMktPrice2,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               mktAssetSwap);
            zeroCpnBondMktAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondMktAssetSwapSpread2 = zeroCpnBondMktAssetSwap2.fairSpread();
            var error8 = System.Math.Abs(zeroCpnBondMktAssetSwapSpread2 -
                                         100 * zeroCpnBondParAssetSwapSpread2 / zeroCpnBondMktFullPrice2);

            if (error8 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for zero cpn bond:" +
                             "\n  market ASW spread: " + zeroCpnBondMktAssetSwapSpread2 +
                             "\n  par ASW spread:    " + zeroCpnBondParAssetSwapSpread2 +
                             "\n  error:             " + error8 +
                             "\n  tolerance:         " + tolerance);
            }
        }

        [Fact]
        public void testZSpread()
        {
            // Testing clean and dirty price with null Z-spread against theoretical prices...
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var inArrears = false;

            // Fixed bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var fixedBondSchedule1 = new Schedule(new Date(4, Month.January, 2005),
                                                       new Date(4, Month.January, 2037),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                                                new List<double> { 0.04 },
                                                new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                                                100.0, new Date(4, Month.January, 2005));

            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondImpliedValue1 = fixedBond1.cleanPrice();
            var fixedBondSettlementDate1 = fixedBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YC...
            var fixedBondCleanPrice1 = BondFunctions.cleanPrice(fixedBond1, vars.termStructure, vars.spread,
                                                                   new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate1);
            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondImpliedValue1 - fixedBondCleanPrice1);
            if (error1 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:" +
                             "\n  market asset swap spread: " +
                             fixedBondImpliedValue1 +
                             "\n  par asset swap spread: " + fixedBondCleanPrice1 +
                             "\n  error:                 " + error1 +
                             "\n  tolerance:             " + tolerance);
            }

            // Fixed bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day

            var fixedBondSchedule2 = new Schedule(new Date(5, Month.February, 2005),
                                                       new Date(5, Month.February, 2019),
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                                                new List<double> { 0.05 },
                                                new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                                                100.0, new Date(5, Month.February, 2005));

            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondImpliedValue2 = fixedBond2.cleanPrice();
            var fixedBondSettlementDate2 = fixedBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var fixedBondCleanPrice2 = BondFunctions.cleanPrice(fixedBond2, vars.termStructure, vars.spread,
                                                                   new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate2);
            var error3 = System.Math.Abs(fixedBondImpliedValue2 - fixedBondCleanPrice2);
            if (error3 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:" +
                             "\n  market asset swap spread: " +
                             fixedBondImpliedValue2 +
                             "\n  par asset swap spread: " + fixedBondCleanPrice2 +
                             "\n  error:                 " + error3 +
                             "\n  tolerance:             " + tolerance);
            }

            // FRN bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day

            var floatingBondSchedule1 = new Schedule(new Date(29, Month.September, 2003),
                                                          new Date(29, Month.September, 2013),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);

            Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule1,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.Following, fixingDays,
                                                      new List<double> { 1 }, new List<double> { 0.0056 },
                                                      new List<double?>(), new List<double?>(),
                                                      inArrears,
                                                      100.0, new Date(29, Month.September, 2003));

            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondImpliedValue1 = floatingBond1.cleanPrice();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var floatingBondCleanPrice1 = BondFunctions.cleanPrice(floatingBond1, vars.termStructure, vars.spread,
                                                                      new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
            var error5 = System.Math.Abs(floatingBondImpliedValue1 - floatingBondCleanPrice1);
            if (error5 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:" +
                             "\n  market asset swap spread: " +
                             floatingBondImpliedValue1 +
                             "\n  par asset swap spread: " + floatingBondCleanPrice1 +
                             "\n  error:                 " + error5 +
                             "\n  tolerance:             " + tolerance);
            }

            // FRN bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day

            var floatingBondSchedule2 = new Schedule(new Date(24, Month.September, 2004),
                                                          new Date(24, Month.September, 2018),
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            Bond floatingBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                      floatingBondSchedule2,
                                                      vars.iborIndex, new Actual360(),
                                                      BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                      new List<double> { 1 }, new List<double> { 0.0025 },
                                                      new List<double?>(), new List<double?>(),
                                                      inArrears,
                                                      100.0, new Date(24, Month.September, 2004));

            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            var floatingBondImpliedValue2 = floatingBond2.cleanPrice();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var floatingBondCleanPrice2 = BondFunctions.cleanPrice(floatingBond2, vars.termStructure,
                                                                      vars.spread, new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
            var error7 = System.Math.Abs(floatingBondImpliedValue2 - floatingBondCleanPrice2);
            if (error7 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " +
                             floatingBondImpliedValue2
                             + "\n  par asset swap spread: " + floatingBondCleanPrice2
                             + "\n  error:                 " + error7
                             + "\n  tolerance:             " + tolerance);
            }

            //// CMS bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            //// maturity doesn't occur on a business day

            var cmsBondSchedule1 = new Schedule(new Date(22, Month.August, 2005),
                                                     new Date(22, Month.August, 2020),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                                            vars.swapIndex, new Thirty360(),
                                            BusinessDayConvention.Following, fixingDays,
                                            new List<double> { 1.0 }, new List<double> { 0.0 },
                                            new List<double?> { 0.055 }, new List<double?> { 0.025 },
                                            inArrears,
                                            100.0, new Date(22, Month.August, 2005));

            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondImpliedValue1 = cmsBond1.cleanPrice();
            var cmsBondSettlementDate1 = cmsBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var cmsBondCleanPrice1 = BondFunctions.cleanPrice(cmsBond1, vars.termStructure, vars.spread,
                                                                 new Actual365Fixed(), vars.compounding, Frequency.Annual, cmsBondSettlementDate1);
            var error9 = System.Math.Abs(cmsBondImpliedValue1 - cmsBondCleanPrice1);
            if (error9 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " + cmsBondImpliedValue1
                             + "\n  par asset swap spread: " + cmsBondCleanPrice1
                             + "\n  error:                 " + error9
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day

            var cmsBondSchedule2 = new Schedule(new Date(06, Month.May, 2005),
                                                     new Date(06, Month.May, 2015),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            Bond cmsBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                                             vars.swapIndex, new Thirty360(),
                                             BusinessDayConvention.Following, fixingDays,
                                             new List<double> { 0.84 }, new List<double> { 0.0 },
                                             new List<double?>(), new List<double?>(),
                                             inArrears,
                                             100.0, new Date(06, Month.May, 2005));

            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondImpliedValue2 = cmsBond2.cleanPrice();
            var cmsBondSettlementDate2 = cmsBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var cmsBondCleanPrice2 = BondFunctions.cleanPrice(cmsBond2, vars.termStructure, vars.spread,
                                                                 new Actual365Fixed(), vars.compounding, Frequency.Annual, cmsBondSettlementDate2);
            var error11 = System.Math.Abs(cmsBondImpliedValue2 - cmsBondCleanPrice2);
            if (error11 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " + cmsBondImpliedValue2
                             + "\n  par asset swap spread: " + cmsBondCleanPrice2
                             + "\n  error:                 " + error11
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero-Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day

            Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(20, Month.December, 2015),
                                                   BusinessDayConvention.Following,
                                                   100.0, new Date(19, Month.December, 1985));

            zeroCpnBond1.setPricingEngine(bondEngine);

            var zeroCpnBondImpliedValue1 = zeroCpnBond1.cleanPrice();
            var zeroCpnBondSettlementDate1 = zeroCpnBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var zeroCpnBondCleanPrice1 = BondFunctions.cleanPrice(zeroCpnBond1, vars.termStructure, vars.spread,
                                                                     new Actual365Fixed(), vars.compounding, Frequency.Annual, zeroCpnBondSettlementDate1);
            var error13 = System.Math.Abs(zeroCpnBondImpliedValue1 - zeroCpnBondCleanPrice1);
            if (error13 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  zero cpn implied value: " +
                             zeroCpnBondImpliedValue1
                             + "\n  zero cpn price: " + zeroCpnBondCleanPrice1
                             + "\n  error:                 " + error13
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity doesn't occur on a business day

            Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                   new Date(17, Month.February, 2028),
                                                   BusinessDayConvention.Following,
                                                   100.0, new Date(17, Month.February, 1998));

            zeroCpnBond2.setPricingEngine(bondEngine);

            var zeroCpnBondImpliedValue2 = zeroCpnBond2.cleanPrice();
            var zeroCpnBondSettlementDate2 = zeroCpnBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var zeroCpnBondCleanPrice2 = BondFunctions.cleanPrice(zeroCpnBond2, vars.termStructure, vars.spread,
                                                                     new Actual365Fixed(), vars.compounding, Frequency.Annual, zeroCpnBondSettlementDate2);
            var error15 = System.Math.Abs(zeroCpnBondImpliedValue2 - zeroCpnBondCleanPrice2);
            if (error15 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  zero cpn implied value: " +
                             zeroCpnBondImpliedValue2
                             + "\n  zero cpn price: " + zeroCpnBondCleanPrice2
                             + "\n  error:                 " + error15
                             + "\n  tolerance:             " + tolerance);
            }
        }

        [Fact]
        public void testGenericBondImplied()
        {

            // Testing implied generic-bond value against asset-swap fair price with null spread...

            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var payFixeddouble = true;
            var parAssetSwap = true;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day
            var fixedBondStartDate1 = new Date(4, Month.January, 2005);
            var fixedBondMaturityDate1 = new Date(4, Month.January, 2037);
            var fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                                       fixedBondMaturityDate1,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                            BusinessDayConvention.Following);
            fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
            var fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondPrice1 = fixedBond1.cleanPrice();
            var fixedBondAssetSwap1 = new AssetSwap(payFixeddouble,
                                                          fixedBond1, fixedBondPrice1,
                                                          vars.iborIndex, vars.spread,
                                                          null,
                                                          vars.iborIndex.dayCounter(),
                                                          parAssetSwap);
            fixedBondAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondAssetSwapPrice1 - fixedBondPrice1);

            if (error1 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for fixed bond:"
                             + "\n  bond's clean price:    " + fixedBondPrice1
                             + "\n  asset swap fair price: " + fixedBondAssetSwapPrice1
                             + "\n  error:                 " + error1
                             + "\n  tolerance:             " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day
            var fixedBondStartDate2 = new Date(5, Month.February, 2005);
            var fixedBondMaturityDate2 = new Date(5, Month.February, 2019);
            var fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                                       fixedBondMaturityDate2,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
            fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
            var fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondPrice2 = fixedBond2.cleanPrice();
            var fixedBondAssetSwap2 = new AssetSwap(payFixeddouble,
                                                          fixedBond2, fixedBondPrice2,
                                                          vars.iborIndex, vars.spread,
                                                          null,
                                                          vars.iborIndex.dayCounter(),
                                                          parAssetSwap);
            fixedBondAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
            var error2 = System.Math.Abs(fixedBondAssetSwapPrice2 - fixedBondPrice2);

            if (error2 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for fixed bond:"
                             + "\n  bond's clean price:    " + fixedBondPrice2
                             + "\n  asset swap fair price: " + fixedBondAssetSwapPrice2
                             + "\n  error:                 " + error2
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day
            var floatingBondStartDate1 = new Date(29, Month.September, 2003);
            var floatingBondMaturityDate1 = new Date(29, Month.September, 2013);
            var floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                                          floatingBondMaturityDate1,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption1 =
               bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
            floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
            var floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondPrice1 = floatingBond1.cleanPrice();
            var floatingBondAssetSwap1 = new AssetSwap(payFixeddouble,
                                                             floatingBond1, floatingBondPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            floatingBondAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
            var error3 = System.Math.Abs(floatingBondAssetSwapPrice1 - floatingBondPrice1);

            if (error3 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for floater:"
                             + "\n  bond's clean price:    " + floatingBondPrice1
                             + "\n  asset swap fair price: " +
                             floatingBondAssetSwapPrice1
                             + "\n  error:                 " + error3
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day
            var floatingBondStartDate2 = new Date(24, Month.September, 2004);
            var floatingBondMaturityDate2 = new Date(24, Month.September, 2018);
            var floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                                          floatingBondMaturityDate2,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing);
            var floatingbondRedemption2 =
               bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
            floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
            var floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate2, floatingBondStartDate2, floatingBondLeg2);
            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            var currentCoupon = 0.04013 + 0.0025;
            var floatingCurrentCoupon = floatingBond2.nextCouponRate();
            var error4 = System.Math.Abs(floatingCurrentCoupon - currentCoupon);
            if (error4 > tolerance)
            {
                QAssert.Fail("wrong current coupon is returned for floater bond:"
                             + "\n  bond's calculated current coupon:      " +
                             currentCoupon
                             + "\n  current coupon asked to the bond: " +
                             floatingCurrentCoupon
                             + "\n  error:                 " + error4
                             + "\n  tolerance:             " + tolerance);
            }

            var floatingBondPrice2 = floatingBond2.cleanPrice();
            var floatingBondAssetSwap2 = new AssetSwap(payFixeddouble,
                                                             floatingBond2, floatingBondPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            floatingBondAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
            var error5 = System.Math.Abs(floatingBondAssetSwapPrice2 - floatingBondPrice2);

            if (error5 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for floater:"
                             + "\n  bond's clean price:    " + floatingBondPrice2
                             + "\n  asset swap fair price: " +
                             floatingBondAssetSwapPrice2
                             + "\n  error:                 " + error5
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day
            var cmsBondStartDate1 = new Date(22, Month.August, 2005);
            var cmsBondMaturityDate1 = new Date(22, Month.August, 2020);
            var cmsBondSchedule1 = new Schedule(cmsBondStartDate1,
                                                     cmsBondMaturityDate1,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withFixingDays(fixingDays)
            .withPaymentDayCounter(new Thirty360())
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
            cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
            var cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                     cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondPrice1 = cmsBond1.cleanPrice();
            var cmsBondAssetSwap1 = new AssetSwap(payFixeddouble,
                                                        cmsBond1, cmsBondPrice1,
                                                        vars.iborIndex, vars.spread,
                                                        null,
                                                        vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            cmsBondAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
            var error6 = System.Math.Abs(cmsBondAssetSwapPrice1 - cmsBondPrice1);

            if (error6 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for cms bond:"
                             + "\n  bond's clean price:    " + cmsBondPrice1
                             + "\n  asset swap fair price: " + cmsBondAssetSwapPrice1
                             + "\n  error:                 " + error6
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day
            var cmsBondStartDate2 = new Date(06, Month.May, 2005);
            var cmsBondMaturityDate2 = new Date(06, Month.May, 2015);
            var cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                                     cmsBondMaturityDate2,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withPaymentDayCounter(new Thirty360())
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2, BusinessDayConvention.Following);
            cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
            var cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                     cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondPrice2 = cmsBond2.cleanPrice();
            var cmsBondAssetSwap2 = new AssetSwap(payFixeddouble,
                                                        cmsBond2, cmsBondPrice2,
                                                        vars.iborIndex, vars.spread,
                                                        null,
                                                        vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            cmsBondAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
            var error7 = System.Math.Abs(cmsBondAssetSwapPrice2 - cmsBondPrice2);

            if (error7 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for cms bond:"
                             + "\n  bond's clean price:    " + cmsBondPrice2
                             + "\n  asset swap fair price: " + cmsBondAssetSwapPrice2
                             + "\n  error:                 " + error7
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day
            var zeroCpnBondStartDate1 = new Date(19, Month.December, 1985);
            var zeroCpnBondMaturityDate1 = new Date(20, Month.December, 2015);
            var zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1, BusinessDayConvention.Following);
            var zeroCpnBondLeg1 = new List<CashFlow> { new SimpleCashFlow(100.0, zeroCpnBondRedemption1) };
            var zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
            zeroCpnBond1.setPricingEngine(bondEngine);

            var zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
            var zeroCpnAssetSwap1 = new AssetSwap(payFixeddouble,
                                                        zeroCpnBond1, zeroCpnBondPrice1,
                                                        vars.iborIndex, vars.spread,
                                                        null,
                                                        vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            zeroCpnAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice1 = zeroCpnAssetSwap1.fairCleanPrice();
            var error8 = System.Math.Abs(zeroCpnBondAssetSwapPrice1 - zeroCpnBondPrice1);

            if (error8 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for zero cpn bond:"
                             + "\n  bond's clean price:    " + zeroCpnBondPrice1
                             + "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice1
                             + "\n  error:                 " + error8
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day
            var zeroCpnBondStartDate2 = new Date(17, Month.February, 1998);
            var zeroCpnBondMaturityDate2 = new Date(17, Month.February, 2028);
            var zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2, BusinessDayConvention.Following);
            var zeroCpnBondLeg2 = new List<CashFlow> { new SimpleCashFlow(100.0, zerocpbondRedemption2) };
            var zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
            zeroCpnBond2.setPricingEngine(bondEngine);

            var zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
            var zeroCpnAssetSwap2 = new AssetSwap(payFixeddouble,
                                                        zeroCpnBond2, zeroCpnBondPrice2,
                                                        vars.iborIndex, vars.spread,
                                                        null,
                                                        vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            zeroCpnAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice2 = zeroCpnAssetSwap2.fairCleanPrice();
            var error9 = System.Math.Abs(cmsBondAssetSwapPrice2 - cmsBondPrice2);

            if (error9 > tolerance)
            {
                QAssert.Fail("wrong zero spread asset swap price for zero cpn bond:"
                             + "\n  bond's clean price:    " + zeroCpnBondPrice2
                             + "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice2
                             + "\n  error:                 " + error9
                             + "\n  tolerance:             " + tolerance);
            }
        }

        [Fact]
        public void testMASWWithGenericBond()
        {
            // Testing market asset swap against par asset swap with generic bond...

            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var payFixedRate = true;
            var parAssetSwap = true;
            var mktAssetSwap = false;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var fixedBondStartDate1 = new Date(4, Month.January, 2005);
            var fixedBondMaturityDate1 = new Date(4, Month.January, 2037);
            var fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                                       fixedBondMaturityDate1,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1, BusinessDayConvention.Following);
            fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
            var fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate1,
                                       fixedBondStartDate1, fixedBondLeg1);
            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondMktPrice1 = 89.22; // market price observed on 7th June 2007
            var fixedBondMktFullPrice1 = fixedBondMktPrice1 + fixedBond1.accruedAmount();
            var fixedBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                             fixedBond1, fixedBondMktPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            fixedBondParAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondParAssetSwapSpread1 = fixedBondParAssetSwap1.fairSpread();
            var fixedBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                             fixedBond1, fixedBondMktPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             mktAssetSwap);
            fixedBondMktAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondMktAssetSwapSpread1 = fixedBondMktAssetSwap1.fairSpread();

            var tolerance = 1.0e-13;
            var error1 =
               System.Math.Abs(fixedBondMktAssetSwapSpread1 -
                        100 * fixedBondParAssetSwapSpread1 / fixedBondMktFullPrice1);

            if (error1 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for fixed bond:" +
                             "\n  market asset swap spread: " + fixedBondMktAssetSwapSpread1 +
                             "\n  par asset swap spread:    " + fixedBondParAssetSwapSpread1 +
                             "\n  error:                    " + error1 +
                             "\n  tolerance:                " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day

            var fixedBondStartDate2 = new Date(5, Month.February, 2005);
            var fixedBondMaturityDate2 = new Date(5, Month.February, 2019);
            var fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                                       fixedBondMaturityDate2,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
            fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
            var fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate2, fixedBondStartDate2,
                                       fixedBondLeg2);
            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondMktPrice2 = 99.98; // market price observed on 7th June 2007
            var fixedBondMktFullPrice2 = fixedBondMktPrice2 + fixedBond2.accruedAmount();
            var fixedBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                             fixedBond2, fixedBondMktPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            fixedBondParAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondParAssetSwapSpread2 = fixedBondParAssetSwap2.fairSpread();
            var fixedBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                             fixedBond2, fixedBondMktPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             mktAssetSwap);
            fixedBondMktAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondMktAssetSwapSpread2 = fixedBondMktAssetSwap2.fairSpread();
            var error2 = System.Math.Abs(fixedBondMktAssetSwapSpread2 -
                                         100 * fixedBondParAssetSwapSpread2 / fixedBondMktFullPrice2);

            if (error2 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for fixed bond:" +
                             "\n  market asset swap spread: " + fixedBondMktAssetSwapSpread2 +
                             "\n  par asset swap spread:    " + fixedBondParAssetSwapSpread2 +
                             "\n  error:                    " + error2 +
                             "\n  tolerance:                " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day

            var floatingBondStartDate1 = new Date(29, Month.September, 2003);
            var floatingBondMaturityDate1 = new Date(29, Month.September, 2013);
            var floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                                          floatingBondMaturityDate1,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption1 =
               bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
            floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
            var floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, floatingBondMaturityDate1,
                                          floatingBondStartDate1, floatingBondLeg1);
            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            // market price observed on 7th June 2007
            var floatingBondMktPrice1 = 101.64;
            var floatingBondMktFullPrice1 =
               floatingBondMktPrice1 + floatingBond1.accruedAmount();
            var floatingBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                                floatingBond1, floatingBondMktPrice1,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                parAssetSwap);
            floatingBondParAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondParAssetSwapSpread1 =
               floatingBondParAssetSwap1.fairSpread();
            var floatingBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                                floatingBond1, floatingBondMktPrice1,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                mktAssetSwap);
            floatingBondMktAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondMktAssetSwapSpread1 =
               floatingBondMktAssetSwap1.fairSpread();
            var error3 = System.Math.Abs(floatingBondMktAssetSwapSpread1 -
                                         100 * floatingBondParAssetSwapSpread1 / floatingBondMktFullPrice1);

            if (error3 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for floating bond:" +
                             "\n  market asset swap spread: " + floatingBondMktAssetSwapSpread1 +
                             "\n  par asset swap spread:    " + floatingBondParAssetSwapSpread1 +
                             "\n  error:                    " + error3 +
                             "\n  tolerance:                " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day

            var floatingBondStartDate2 = new Date(24, Month.September, 2004);
            var floatingBondMaturityDate2 = new Date(24, Month.September, 2018);
            var floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                                          floatingBondMaturityDate2,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentDayCounter(new Actual360())
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption2 =
               bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
            floatingBondLeg2.Add(new
                                 SimpleCashFlow(100.0, floatingbondRedemption2));
            var floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, floatingBondMaturityDate2,
                                          floatingBondStartDate2, floatingBondLeg2);
            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            // market price observed on 7th June 2007
            var floatingBondMktPrice2 = 101.248;
            var floatingBondMktFullPrice2 =
               floatingBondMktPrice2 + floatingBond2.accruedAmount();
            var floatingBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                                floatingBond2, floatingBondMktPrice2,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                parAssetSwap);
            floatingBondParAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondParAssetSwapSpread2 = floatingBondParAssetSwap2.fairSpread();
            var floatingBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                                floatingBond2, floatingBondMktPrice2,
                                                                vars.iborIndex, vars.spread,
                                                                null,
                                                                vars.iborIndex.dayCounter(),
                                                                mktAssetSwap);
            floatingBondMktAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondMktAssetSwapSpread2 =
               floatingBondMktAssetSwap2.fairSpread();
            var error4 = System.Math.Abs(floatingBondMktAssetSwapSpread2 -
                                         100 * floatingBondParAssetSwapSpread2 / floatingBondMktFullPrice2);

            if (error4 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for floating bond:" +
                             "\n  market asset swap spread: " + floatingBondMktAssetSwapSpread2 +
                             "\n  par asset swap spread:    " + floatingBondParAssetSwapSpread2 +
                             "\n  error:                    " + error4 +
                             "\n  tolerance:                " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day

            var cmsBondStartDate1 = new Date(22, Month.August, 2005);
            var cmsBondMaturityDate1 = new Date(22, Month.August, 2020);
            var cmsBondSchedule1 = new Schedule(cmsBondStartDate1,
                                                     cmsBondMaturityDate1,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
            cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
            var cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, cmsBondMaturityDate1, cmsBondStartDate1,
                                     cmsBondLeg1);
            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondMktPrice1 = 88.45; // market price observed on 7th June 2007
            var cmsBondMktFullPrice1 = cmsBondMktPrice1 + cmsBond1.accruedAmount();
            var cmsBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                                           cmsBond1, cmsBondMktPrice1,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           parAssetSwap);
            cmsBondParAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondParAssetSwapSpread1 = cmsBondParAssetSwap1.fairSpread();
            var cmsBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                                           cmsBond1, cmsBondMktPrice1,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           mktAssetSwap);
            cmsBondMktAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondMktAssetSwapSpread1 = cmsBondMktAssetSwap1.fairSpread();
            var error5 =
               System.Math.Abs(cmsBondMktAssetSwapSpread1 -
                        100 * cmsBondParAssetSwapSpread1 / cmsBondMktFullPrice1);

            if (error5 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for cms bond:" +
                             "\n  market asset swap spread: " + cmsBondMktAssetSwapSpread1 +
                             "\n  par asset swap spread:    " + cmsBondParAssetSwapSpread1 +
                             "\n  error:                    " + error5 +
                             "\n  tolerance:                " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day

            var cmsBondStartDate2 = new Date(06, Month.May, 2005);
            var cmsBondMaturityDate2 = new Date(06, Month.May, 2015);
            var cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                                     cmsBondMaturityDate2,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2, BusinessDayConvention.Following);
            cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
            var cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, cmsBondMaturityDate2, cmsBondStartDate2,
                                     cmsBondLeg2);
            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondMktPrice2 = 94.08; // market price observed on 7th June 2007
            var cmsBondMktFullPrice2 = cmsBondMktPrice2 + cmsBond2.accruedAmount();
            var cmsBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                                           cmsBond2, cmsBondMktPrice2,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           parAssetSwap);
            cmsBondParAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondParAssetSwapSpread2 = cmsBondParAssetSwap2.fairSpread();
            var cmsBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                                           cmsBond2, cmsBondMktPrice2,
                                                           vars.iborIndex, vars.spread,
                                                           null,
                                                           vars.iborIndex.dayCounter(),
                                                           mktAssetSwap);
            cmsBondMktAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondMktAssetSwapSpread2 = cmsBondMktAssetSwap2.fairSpread();
            var error6 =
               System.Math.Abs(cmsBondMktAssetSwapSpread2 -
                        100 * cmsBondParAssetSwapSpread2 / cmsBondMktFullPrice2);

            if (error6 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for cms bond:" +
                             "\n  market asset swap spread: " + cmsBondMktAssetSwapSpread2 +
                             "\n  par asset swap spread:    " + cmsBondParAssetSwapSpread2 +
                             "\n  error:                    " + error6 +
                             "\n  tolerance:                " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day

            var zeroCpnBondStartDate1 = new Date(19, Month.December, 1985);
            var zeroCpnBondMaturityDate1 = new Date(20, Month.December, 2015);
            var zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                              BusinessDayConvention.Following);
            var zeroCpnBondLeg1 = new List<CashFlow> { new SimpleCashFlow(100.0, zeroCpnBondRedemption1) };
            var zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, zeroCpnBondMaturityDate1,
                                          zeroCpnBondStartDate1, zeroCpnBondLeg1);
            zeroCpnBond1.setPricingEngine(bondEngine);

            // market price observed on 12th June 2007
            var zeroCpnBondMktPrice1 = 70.436;
            var zeroCpnBondMktFullPrice1 =
               zeroCpnBondMktPrice1 + zeroCpnBond1.accruedAmount();
            var zeroCpnBondParAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1,
                                                               zeroCpnBondMktPrice1,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            zeroCpnBondParAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondParAssetSwapSpread1 = zeroCpnBondParAssetSwap1.fairSpread();
            var zeroCpnBondMktAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1,
                                                               zeroCpnBondMktPrice1,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               mktAssetSwap);
            zeroCpnBondMktAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondMktAssetSwapSpread1 = zeroCpnBondMktAssetSwap1.fairSpread();
            var error7 =
               System.Math.Abs(zeroCpnBondMktAssetSwapSpread1 -
                        100 * zeroCpnBondParAssetSwapSpread1 / zeroCpnBondMktFullPrice1);

            if (error7 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for zero cpn bond:" +
                             "\n  market asset swap spread: " + zeroCpnBondMktAssetSwapSpread1 +
                             "\n  par asset swap spread:    " + zeroCpnBondParAssetSwapSpread1 +
                             "\n  error:                    " + error7 +
                             "\n  tolerance:                " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day

            var zeroCpnBondStartDate2 = new Date(17, Month.February, 1998);
            var zeroCpnBondMaturityDate2 = new Date(17, Month.February, 2028);
            var zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                             BusinessDayConvention.Following);
            var zeroCpnBondLeg2 = new List<CashFlow> { new SimpleCashFlow(100.0, zerocpbondRedemption2) };
            var zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
            zeroCpnBond2.setPricingEngine(bondEngine);

            // double zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
            // market price observed on 12th June 2007
            var zeroCpnBondMktPrice2 = 35.160;
            var zeroCpnBondMktFullPrice2 =
               zeroCpnBondMktPrice2 + zeroCpnBond2.accruedAmount();
            var zeroCpnBondParAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2,
                                                               zeroCpnBondMktPrice2,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            zeroCpnBondParAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondParAssetSwapSpread2 = zeroCpnBondParAssetSwap2.fairSpread();
            var zeroCpnBondMktAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2,
                                                               zeroCpnBondMktPrice2,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               mktAssetSwap);
            zeroCpnBondMktAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondMktAssetSwapSpread2 = zeroCpnBondMktAssetSwap2.fairSpread();
            var error8 =
               System.Math.Abs(zeroCpnBondMktAssetSwapSpread2 -
                        100 * zeroCpnBondParAssetSwapSpread2 / zeroCpnBondMktFullPrice2);

            if (error8 > tolerance)
            {
                QAssert.Fail("wrong asset swap spreads for zero cpn bond:" +
                             "\n  market asset swap spread: " + zeroCpnBondMktAssetSwapSpread2 +
                             "\n  par asset swap spread:    " + zeroCpnBondParAssetSwapSpread2 +
                             "\n  error:                    " + error8 +
                             "\n  tolerance:                " + tolerance);
            }
        }

        [Fact]
        public void testZSpreadWithGenericBond()
        {
            // Testing clean and dirty price with null Z-spread against theoretical prices...

            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day

            var fixedBondStartDate1 = new Date(4, Month.January, 2005);
            var fixedBondMaturityDate1 = new Date(4, Month.January, 2037);
            var fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                                       fixedBondMaturityDate1,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                            BusinessDayConvention.Following);
            fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
            var fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate1, fixedBondStartDate1,
                                       fixedBondLeg1);
            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            var fixedBondImpliedValue1 = fixedBond1.cleanPrice();
            var fixedBondSettlementDate1 = fixedBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var fixedBondCleanPrice1 = BondFunctions.cleanPrice(fixedBond1, vars.termStructure, vars.spread,
                                                                   new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate1);
            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondImpliedValue1 - fixedBondCleanPrice1);
            if (error1 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: "
                             + fixedBondImpliedValue1
                             + "\n  par asset swap spread: " + fixedBondCleanPrice1
                             + "\n  error:                 " + error1
                             + "\n  tolerance:             " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day

            var fixedBondStartDate2 = new Date(5, Month.February, 2005);
            var fixedBondMaturityDate2 = new Date(5, Month.February, 2019);
            var fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                                       fixedBondMaturityDate2,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
            fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
            var fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
            fixedBond2.setPricingEngine(bondEngine);

            var fixedBondImpliedValue2 = fixedBond2.cleanPrice();
            var fixedBondSettlementDate2 = fixedBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve

            var fixedBondCleanPrice2 = BondFunctions.cleanPrice(fixedBond2, vars.termStructure, vars.spread,
                                                                   new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate2);
            var error3 = System.Math.Abs(fixedBondImpliedValue2 - fixedBondCleanPrice2);
            if (error3 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: "
                             + fixedBondImpliedValue2
                             + "\n  par asset swap spread: " + fixedBondCleanPrice2
                             + "\n  error:                 " + error3
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day

            var floatingBondStartDate1 = new Date(29, Month.September, 2003);
            var floatingBondMaturityDate1 = new Date(29, Month.September, 2013);
            var floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                                          floatingBondMaturityDate1,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption1 =
               bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
            floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
            var floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate1, floatingBondStartDate1,
                                          floatingBondLeg1);
            floatingBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondImpliedValue1 = floatingBond1.cleanPrice();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var floatingBondCleanPrice1 = BondFunctions.cleanPrice(floatingBond1, vars.termStructure, vars.spread,
                                                                      new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
            var error5 = System.Math.Abs(floatingBondImpliedValue1 - floatingBondCleanPrice1);
            if (error5 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " +
                             floatingBondImpliedValue1
                             + "\n  par asset swap spread: " + floatingBondCleanPrice1
                             + "\n  error:                 " + error5
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day

            var floatingBondStartDate2 = new Date(24, Month.September, 2004);
            var floatingBondMaturityDate2 = new Date(24, Month.September, 2018);
            var floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                                          floatingBondMaturityDate2,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .withPaymentDayCounter(new Actual360())
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption2 = bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
            floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
            var floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, floatingBondMaturityDate2,
                                          floatingBondStartDate2, floatingBondLeg2);
            floatingBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);
            var floatingBondImpliedValue2 = floatingBond2.cleanPrice();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var floatingBondCleanPrice2 = BondFunctions.cleanPrice(floatingBond2, vars.termStructure, vars.spread,
                                                                      new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
            var error7 = System.Math.Abs(floatingBondImpliedValue2 - floatingBondCleanPrice2);
            if (error7 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " +
                             floatingBondImpliedValue2
                             + "\n  par asset swap spread: " + floatingBondCleanPrice2
                             + "\n  error:                 " + error7
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day

            var cmsBondStartDate1 = new Date(22, Month.August, 2005);
            var cmsBondMaturityDate1 = new Date(22, Month.August, 2020);
            var cmsBondSchedule1 = new Schedule(cmsBondStartDate1,
                                                     cmsBondMaturityDate1,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
            cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
            var cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, cmsBondMaturityDate1, cmsBondStartDate1,
                                     cmsBondLeg1);
            cmsBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondImpliedValue1 = cmsBond1.cleanPrice();
            var cmsBondSettlementDate1 = cmsBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var cmsBondCleanPrice1 = BondFunctions.cleanPrice(cmsBond1, vars.termStructure, vars.spread,
                                                                 new Actual365Fixed(), vars.compounding, Frequency.Annual,
                                                                 cmsBondSettlementDate1);
            var error9 = System.Math.Abs(cmsBondImpliedValue1 - cmsBondCleanPrice1);
            if (error9 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " + cmsBondImpliedValue1
                             + "\n  par asset swap spread: " + cmsBondCleanPrice1
                             + "\n  error:                 " + error9
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day

            var cmsBondStartDate2 = new Date(06, Month.May, 2005);
            var cmsBondMaturityDate2 = new Date(06, Month.May, 2015);
            var cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                                     cmsBondMaturityDate2,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2, BusinessDayConvention.Following);
            cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
            var cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                     cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
            cmsBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondImpliedValue2 = cmsBond2.cleanPrice();
            var cmsBondSettlementDate2 = cmsBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var cmsBondCleanPrice2 = BondFunctions.cleanPrice(cmsBond2, vars.termStructure, vars.spread,
                                                                 new Actual365Fixed(), vars.compounding, Frequency.Annual,
                                                                 cmsBondSettlementDate2);
            var error11 = System.Math.Abs(cmsBondImpliedValue2 - cmsBondCleanPrice2);
            if (error11 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  market asset swap spread: " + cmsBondImpliedValue2
                             + "\n  par asset swap spread: " + cmsBondCleanPrice2
                             + "\n  error:                 " + error11
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day

            var zeroCpnBondStartDate1 = new Date(19, Month.December, 1985);
            var zeroCpnBondMaturityDate1 = new Date(20, Month.December, 2015);
            var zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                              BusinessDayConvention.Following);
            var zeroCpnBondLeg1 = new List<CashFlow> { new SimpleCashFlow(100.0, zeroCpnBondRedemption1) };
            var zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
            zeroCpnBond1.setPricingEngine(bondEngine);

            var zeroCpnBondImpliedValue1 = zeroCpnBond1.cleanPrice();
            var zeroCpnBondSettlementDate1 = zeroCpnBond1.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var zeroCpnBondCleanPrice1 =
               BondFunctions.cleanPrice(zeroCpnBond1,
                                        vars.termStructure,
                                        vars.spread,
                                        new Actual365Fixed(),
                                        vars.compounding, Frequency.Annual,
                                        zeroCpnBondSettlementDate1);
            var error13 = System.Math.Abs(zeroCpnBondImpliedValue1 - zeroCpnBondCleanPrice1);
            if (error13 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  zero cpn implied value: " +
                             zeroCpnBondImpliedValue1
                             + "\n  zero cpn price: " + zeroCpnBondCleanPrice1
                             + "\n  error:                 " + error13
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day

            var zeroCpnBondStartDate2 = new Date(17, Month.February, 1998);
            var zeroCpnBondMaturityDate2 = new Date(17, Month.February, 2028);
            var zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                             BusinessDayConvention.Following);
            var zeroCpnBondLeg2 = new List<CashFlow> { new SimpleCashFlow(100.0, zerocpbondRedemption2) };
            var zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
            zeroCpnBond2.setPricingEngine(bondEngine);

            var zeroCpnBondImpliedValue2 = zeroCpnBond2.cleanPrice();
            var zeroCpnBondSettlementDate2 = zeroCpnBond2.settlementDate();
            // standard market conventions:
            // bond's frequency + coumpounding and daycounter of the YieldCurve
            var zeroCpnBondCleanPrice2 =
               BondFunctions.cleanPrice(zeroCpnBond2,
                                        vars.termStructure,
                                        vars.spread,
                                        new Actual365Fixed(),
                                        vars.compounding, Frequency.Annual,
                                        zeroCpnBondSettlementDate2);
            var error15 = System.Math.Abs(zeroCpnBondImpliedValue2 - zeroCpnBondCleanPrice2);
            if (error15 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  zero cpn implied value: " +
                             zeroCpnBondImpliedValue2
                             + "\n  zero cpn price: " + zeroCpnBondCleanPrice2
                             + "\n  error:                 " + error15
                             + "\n  tolerance:             " + tolerance);
            }
        }

        [Fact]
        public void testSpecializedBondVsGenericBond()
        {
            // Testing clean and dirty prices for specialized bond against equivalent generic bond...
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var inArrears = false;

            // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day
            var fixedBondStartDate1 = new Date(4, Month.January, 2005);
            var fixedBondMaturityDate1 = new Date(4, Month.January, 2037);
            var fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                                       fixedBondMaturityDate1,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                            BusinessDayConvention.Following);
            fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
            // generic bond
            var fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                        fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            // equivalent specialized fixed rate bond
            Bond fixedSpecializedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                                                           new List<double> { 0.04 },
                                                           new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                                                           100.0, new Date(4, Month.January, 2005));
            fixedSpecializedBond1.setPricingEngine(bondEngine);

            var fixedBondTheoValue1 = fixedBond1.cleanPrice();
            var fixedSpecializedBondTheoValue1 = fixedSpecializedBond1.cleanPrice();
            var tolerance = 1.0e-13;
            var error1 = System.Math.Abs(fixedBondTheoValue1 - fixedSpecializedBondTheoValue1);
            if (error1 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  specialized fixed rate bond's theo clean price: "
                             + fixedBondTheoValue1
                             + "\n  generic equivalent bond's theo clean price: "
                             + fixedSpecializedBondTheoValue1
                             + "\n  error:                 " + error1
                             + "\n  tolerance:             " + tolerance);
            }
            var fixedBondTheoDirty1 = fixedBondTheoValue1 + fixedBond1.accruedAmount();
            var fixedSpecializedTheoDirty1 = fixedSpecializedBondTheoValue1 +
                                             fixedSpecializedBond1.accruedAmount();
            var error2 = System.Math.Abs(fixedBondTheoDirty1 - fixedSpecializedTheoDirty1);
            if (error2 > tolerance)
            {
                QAssert.Fail("wrong dirty price for fixed bond:"
                             + "\n  specialized fixed rate bond's theo dirty price: "
                             + fixedBondTheoDirty1
                             + "\n  generic equivalent bond's theo dirty price: "
                             + fixedSpecializedTheoDirty1
                             + "\n  error:                 " + error2
                             + "\n  tolerance:             " + tolerance);
            }

            // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
            // maturity occurs on a business day
            var fixedBondStartDate2 = new Date(5, Month.February, 2005);
            var fixedBondMaturityDate2 = new Date(5, Month.February, 2019);
            var fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                                       fixedBondMaturityDate2,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
            fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));

            // generic bond
            var fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
            fixedBond2.setPricingEngine(bondEngine);

            // equivalent specialized fixed rate bond
            Bond fixedSpecializedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                                                            new List<double> { 0.05 },
                                                            new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                                                            100.0, new Date(5, Month.February, 2005));
            fixedSpecializedBond2.setPricingEngine(bondEngine);

            var fixedBondTheoValue2 = fixedBond2.cleanPrice();
            var fixedSpecializedBondTheoValue2 = fixedSpecializedBond2.cleanPrice();

            var error3 = System.Math.Abs(fixedBondTheoValue2 - fixedSpecializedBondTheoValue2);
            if (error3 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  specialized fixed rate bond's theo clean price: "
                             + fixedBondTheoValue2
                             + "\n  generic equivalent bond's theo clean price: "
                             + fixedSpecializedBondTheoValue2
                             + "\n  error:                 " + error3
                             + "\n  tolerance:             " + tolerance);
            }
            var fixedBondTheoDirty2 = fixedBondTheoValue2 +
                                      fixedBond2.accruedAmount();
            var fixedSpecializedBondTheoDirty2 = fixedSpecializedBondTheoValue2 +
                                                 fixedSpecializedBond2.accruedAmount();

            var error4 = System.Math.Abs(fixedBondTheoDirty2 - fixedSpecializedBondTheoDirty2);
            if (error4 > tolerance)
            {
                QAssert.Fail("wrong dirty price for fixed bond:"
                             + "\n  specialized fixed rate bond's dirty clean price: "
                             + fixedBondTheoDirty2
                             + "\n  generic equivalent bond's theo dirty price: "
                             + fixedSpecializedBondTheoDirty2
                             + "\n  error:                 " + error4
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            // maturity doesn't occur on a business day
            var floatingBondStartDate1 = new Date(29, Month.September, 2003);
            var floatingBondMaturityDate1 = new Date(29, Month.September, 2013);
            var floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                                          floatingBondMaturityDate1,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption1 = bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
            floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
            // generic bond
            var floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
            floatingBond1.setPricingEngine(bondEngine);

            // equivalent specialized floater
            Bond floatingSpecializedBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                                 floatingBondSchedule1,
                                                                 vars.iborIndex, new Actual360(),
                                                                 BusinessDayConvention.Following, fixingDays,
                                                                 new List<double> { 1 },
                                                                 new List<double> { 0.0056 },
                                                                 new List<double?>(), new List<double?>(),
                                                                 inArrears,
                                                                 100.0, new Date(29, Month.September, 2003));
            floatingSpecializedBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            Utils.setCouponPricer(floatingSpecializedBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondTheoValue1 = floatingBond1.cleanPrice();
            var floatingSpecializedBondTheoValue1 =
               floatingSpecializedBond1.cleanPrice();

            var error5 = System.Math.Abs(floatingBondTheoValue1 -
                                         floatingSpecializedBondTheoValue1);
            if (error5 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  generic fixed rate bond's theo clean price: "
                             + floatingBondTheoValue1
                             + "\n  equivalent specialized bond's theo clean price: "
                             + floatingSpecializedBondTheoValue1
                             + "\n  error:                 " + error5
                             + "\n  tolerance:             " + tolerance);
            }
            var floatingBondTheoDirty1 = floatingBondTheoValue1 +
                                         floatingBond1.accruedAmount();
            var floatingSpecializedBondTheoDirty1 =
               floatingSpecializedBondTheoValue1 +
               floatingSpecializedBond1.accruedAmount();
            var error6 = System.Math.Abs(floatingBondTheoDirty1 -
                                         floatingSpecializedBondTheoDirty1);
            if (error6 > tolerance)
            {
                QAssert.Fail("wrong dirty price for frn bond:"
                             + "\n  generic frn bond's dirty clean price: "
                             + floatingBondTheoDirty1
                             + "\n  equivalent specialized bond's theo dirty price: "
                             + floatingSpecializedBondTheoDirty1
                             + "\n  error:                 " + error6
                             + "\n  tolerance:             " + tolerance);
            }

            // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
            // maturity occurs on a business day
            var floatingBondStartDate2 = new Date(24, Month.September, 2004);
            var floatingBondMaturityDate2 = new Date(24, Month.September, 2018);
            var floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                                          floatingBondMaturityDate2,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption2 =
               bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
            floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
            // generic bond
            var floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate2, floatingBondStartDate2, floatingBondLeg2);
            floatingBond2.setPricingEngine(bondEngine);

            // equivalent specialized floater
            Bond floatingSpecializedBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                                 floatingBondSchedule2,
                                                                 vars.iborIndex, new Actual360(),
                                                                 BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                                 new List<double> { 1 },
                                                                 new List<double> { 0.0025 },
                                                                 new List<double?>(), new List<double?>(),
                                                                 inArrears,
                                                                 100.0, new Date(24, Month.September, 2004));
            floatingSpecializedBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            Utils.setCouponPricer(floatingSpecializedBond2.cashflows(), vars.pricer);

            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);

            var floatingBondTheoValue2 = floatingBond2.cleanPrice();
            var floatingSpecializedBondTheoValue2 =
               floatingSpecializedBond2.cleanPrice();

            var error7 =
               System.Math.Abs(floatingBondTheoValue2 - floatingSpecializedBondTheoValue2);
            if (error7 > tolerance)
            {
                QAssert.Fail("wrong clean price for floater bond:"
                             + "\n  generic floater bond's theo clean price: "
                             + floatingBondTheoValue2
                             + "\n  equivalent specialized bond's theo clean price: "
                             + floatingSpecializedBondTheoValue2
                             + "\n  error:                 " + error7
                             + "\n  tolerance:             " + tolerance);
            }
            var floatingBondTheoDirty2 = floatingBondTheoValue2 +
                                         floatingBond2.accruedAmount();
            var floatingSpecializedTheoDirty2 = floatingSpecializedBondTheoValue2 +
                                                floatingSpecializedBond2.accruedAmount();

            var error8 =
               System.Math.Abs(floatingBondTheoDirty2 - floatingSpecializedTheoDirty2);
            if (error8 > tolerance)
            {
                QAssert.Fail("wrong dirty price for floater bond:"
                             + "\n  generic floater bond's theo dirty price: "
                             + floatingBondTheoDirty2
                             + "\n  equivalent specialized  bond's theo dirty price: "
                             + floatingSpecializedTheoDirty2
                             + "\n  error:                 " + error8
                             + "\n  tolerance:             " + tolerance);
            }


            // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day
            var cmsBondStartDate1 = new Date(22, Month.August, 2005);
            var cmsBondMaturityDate1 = new Date(22, Month.August, 2020);
            var cmsBondSchedule1 = new Schedule(cmsBondStartDate1,
                                                     cmsBondMaturityDate1,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
            cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
            // generic cms bond
            var cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                      cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
            cmsBond1.setPricingEngine(bondEngine);

            // equivalent specialized cms bond
            Bond cmsSpecializedBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                                                         vars.swapIndex, new Thirty360(),
                                                         BusinessDayConvention.Following, fixingDays,
                                                         new List<double> { 1.0 }, new List<double> { 0.0 },
                                                         new List<double?> { 0.055 }, new List<double?> { 0.025 },
                                                         inArrears,
                                                         100.0, new Date(22, Month.August, 2005));
            cmsSpecializedBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            Utils.setCouponPricer(cmsSpecializedBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondTheoValue1 = cmsBond1.cleanPrice();
            var cmsSpecializedBondTheoValue1 = cmsSpecializedBond1.cleanPrice();
            var error9 = System.Math.Abs(cmsBondTheoValue1 - cmsSpecializedBondTheoValue1);
            if (error9 > tolerance)
            {
                QAssert.Fail("wrong clean price for cms bond:"
                             + "\n  generic cms bond's theo clean price: "
                             + cmsBondTheoValue1
                             + "\n  equivalent specialized bond's theo clean price: "
                             + cmsSpecializedBondTheoValue1
                             + "\n  error:                 " + error9
                             + "\n  tolerance:             " + tolerance);
            }
            var cmsBondTheoDirty1 = cmsBondTheoValue1 + cmsBond1.accruedAmount();
            var cmsSpecializedBondTheoDirty1 = cmsSpecializedBondTheoValue1 +
                                               cmsSpecializedBond1.accruedAmount();
            var error10 = System.Math.Abs(cmsBondTheoDirty1 - cmsSpecializedBondTheoDirty1);
            if (error10 > tolerance)
            {
                QAssert.Fail("wrong dirty price for cms bond:"
                             + "\n generic cms bond's theo dirty price: "
                             + cmsBondTheoDirty1
                             + "\n  specialized cms bond's theo dirty price: "
                             + cmsSpecializedBondTheoDirty1
                             + "\n  error:                 " + error10
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            // maturity occurs on a business day
            var cmsBondStartDate2 = new Date(06, Month.May, 2005);
            var cmsBondMaturityDate2 = new Date(06, Month.May, 2015);
            var cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                                     cmsBondMaturityDate2,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2, BusinessDayConvention.Following);
            cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
            // generic bond
            var cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                      cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
            cmsBond2.setPricingEngine(bondEngine);

            // equivalent specialized cms bond
            Bond cmsSpecializedBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                                                       vars.swapIndex, new Thirty360(),
                                                       BusinessDayConvention.Following, fixingDays,
                                                       new List<double> { 0.84 }, new List<double> { 0.0 },
                                                       new List<double?>(), new List<double?>(),
                                                       inArrears, 100.0, new Date(06, Month.May, 2005));
            cmsSpecializedBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            Utils.setCouponPricer(cmsSpecializedBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondTheoValue2 = cmsBond2.cleanPrice();
            var cmsSpecializedBondTheoValue2 = cmsSpecializedBond2.cleanPrice();

            var error11 = System.Math.Abs(cmsBondTheoValue2 - cmsSpecializedBondTheoValue2);
            if (error11 > tolerance)
            {
                QAssert.Fail("wrong clean price for cms bond:"
                             + "\n  generic cms bond's theo clean price: "
                             + cmsBondTheoValue2
                             + "\n  cms bond's theo clean price: "
                             + cmsSpecializedBondTheoValue2
                             + "\n  error:                 " + error11
                             + "\n  tolerance:             " + tolerance);
            }
            var cmsBondTheoDirty2 = cmsBondTheoValue2 + cmsBond2.accruedAmount();
            var cmsSpecializedBondTheoDirty2 =
               cmsSpecializedBondTheoValue2 + cmsSpecializedBond2.accruedAmount();
            var error12 = System.Math.Abs(cmsBondTheoDirty2 - cmsSpecializedBondTheoDirty2);
            if (error12 > tolerance)
            {
                QAssert.Fail("wrong dirty price for cms bond:"
                             + "\n  generic cms bond's dirty price: "
                             + cmsBondTheoDirty2
                             + "\n  specialized cms bond's theo dirty price: "
                             + cmsSpecializedBondTheoDirty2
                             + "\n  error:                 " + error12
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            // maturity doesn't occur on a business day
            var zeroCpnBondStartDate1 = new Date(19, Month.December, 1985);
            var zeroCpnBondMaturityDate1 = new Date(20, Month.December, 2015);
            var zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                              BusinessDayConvention.Following);
            var zeroCpnBondLeg1 = new List<CashFlow> { new SimpleCashFlow(100.0, zeroCpnBondRedemption1) };
            // generic bond
            var zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, zeroCpnBondMaturityDate1,
                                         zeroCpnBondStartDate1, zeroCpnBondLeg1);
            zeroCpnBond1.setPricingEngine(bondEngine);

            // specialized zerocpn bond
            Bond zeroCpnSpecializedBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                              new Date(20, Month.December, 2015),
                                                              BusinessDayConvention.Following,
                                                              100.0, new Date(19, Month.December, 1985));
            zeroCpnSpecializedBond1.setPricingEngine(bondEngine);

            var zeroCpnBondTheoValue1 = zeroCpnBond1.cleanPrice();
            var zeroCpnSpecializedBondTheoValue1 =
               zeroCpnSpecializedBond1.cleanPrice();

            var error13 =
               System.Math.Abs(zeroCpnBondTheoValue1 - zeroCpnSpecializedBondTheoValue1);
            if (error13 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  generic zero bond's clean price: "
                             + zeroCpnBondTheoValue1
                             + "\n  specialized zero bond's clean price: "
                             + zeroCpnSpecializedBondTheoValue1
                             + "\n  error:                 " + error13
                             + "\n  tolerance:             " + tolerance);
            }
            var zeroCpnBondTheoDirty1 = zeroCpnBondTheoValue1 +
                                        zeroCpnBond1.accruedAmount();
            var zeroCpnSpecializedBondTheoDirty1 =
               zeroCpnSpecializedBondTheoValue1 +
               zeroCpnSpecializedBond1.accruedAmount();
            var error14 =
               System.Math.Abs(zeroCpnBondTheoDirty1 - zeroCpnSpecializedBondTheoDirty1);
            if (error14 > tolerance)
            {
                QAssert.Fail("wrong dirty price for zero bond:"
                             + "\n  generic zerocpn bond's dirty price: "
                             + zeroCpnBondTheoDirty1
                             + "\n  specialized zerocpn bond's clean price: "
                             + zeroCpnSpecializedBondTheoDirty1
                             + "\n  error:                 " + error14
                             + "\n  tolerance:             " + tolerance);
            }

            // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            // maturity occurs on a business day
            var zeroCpnBondStartDate2 = new Date(17, Month.February, 1998);
            var zeroCpnBondMaturityDate2 = new Date(17, Month.February, 2028);
            var zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                             BusinessDayConvention.Following);
            var zeroCpnBondLeg2 = new List<CashFlow> { new SimpleCashFlow(100.0, zerocpbondRedemption2) };
            // generic bond
            var zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
            zeroCpnBond2.setPricingEngine(bondEngine);

            // specialized zerocpn bond
            Bond zeroCpnSpecializedBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                              new Date(17, Month.February, 2028),
                                                              BusinessDayConvention.Following,
                                                              100.0, new Date(17, Month.February, 1998));
            zeroCpnSpecializedBond2.setPricingEngine(bondEngine);

            var zeroCpnBondTheoValue2 = zeroCpnBond2.cleanPrice();
            var zeroCpnSpecializedBondTheoValue2 =
               zeroCpnSpecializedBond2.cleanPrice();

            var error15 =
               System.Math.Abs(zeroCpnBondTheoValue2 - zeroCpnSpecializedBondTheoValue2);
            if (error15 > tolerance)
            {
                QAssert.Fail("wrong clean price for zero coupon bond:"
                             + "\n  generic zerocpn bond's clean price: "
                             + zeroCpnBondTheoValue2
                             + "\n  specialized zerocpn bond's clean price: "
                             + zeroCpnSpecializedBondTheoValue2
                             + "\n  error:                 " + error15
                             + "\n  tolerance:             " + tolerance);
            }
            var zeroCpnBondTheoDirty2 = zeroCpnBondTheoValue2 +
                                        zeroCpnBond2.accruedAmount();

            var zeroCpnSpecializedBondTheoDirty2 =
               zeroCpnSpecializedBondTheoValue2 +
               zeroCpnSpecializedBond2.accruedAmount();

            var error16 =
               System.Math.Abs(zeroCpnBondTheoDirty2 - zeroCpnSpecializedBondTheoDirty2);
            if (error16 > tolerance)
            {
                QAssert.Fail("wrong dirty price for zero coupon bond:"
                             + "\n  generic zerocpn bond's dirty price: "
                             + zeroCpnBondTheoDirty2
                             + "\n  specialized zerocpn bond's dirty price: "
                             + zeroCpnSpecializedBondTheoDirty2
                             + "\n  error:                 " + error16
                             + "\n  tolerance:             " + tolerance);
            }
        }

        [Fact]
        public void testSpecializedBondVsGenericBondUsingAsw()
        {
            // Testing asset-swap prices and spreads for specialized bond against equivalent generic bond...
            var vars = new CommonVars();

            Calendar bondCalendar = new TARGET();
            var settlementDays = 3;
            var fixingDays = 2;
            var payFixedRate = true;
            var parAssetSwap = true;
            var inArrears = false;

            // Fixed bond (Isin: DE0001135275 DBR 4 01/04/37)
            // maturity doesn't occur on a business day
            var fixedBondStartDate1 = new Date(4, Month.January, 2005);
            var fixedBondMaturityDate1 = new Date(4, Month.January, 2037);
            var fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                                       fixedBondMaturityDate1,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1, BusinessDayConvention.Following);
            fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
            // generic bond
            var fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
            IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
            IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
            fixedBond1.setPricingEngine(bondEngine);

            // equivalent specialized fixed rate bond
            Bond fixedSpecializedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                                                           new List<double> { 0.04 },
                                                           new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                                                           100.0, new Date(4, Month.January, 2005));
            fixedSpecializedBond1.setPricingEngine(bondEngine);

            var fixedBondPrice1 = fixedBond1.cleanPrice();
            var fixedSpecializedBondPrice1 = fixedSpecializedBond1.cleanPrice();
            var fixedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                          fixedBond1, fixedBondPrice1,
                                                          vars.iborIndex, vars.nonnullspread,
                                                          null,
                                                          vars.iborIndex.dayCounter(),
                                                          parAssetSwap);
            fixedBondAssetSwap1.setPricingEngine(swapEngine);
            var fixedSpecializedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                                     fixedSpecializedBond1,
                                                                     fixedSpecializedBondPrice1,
                                                                     vars.iborIndex,
                                                                     vars.nonnullspread,
                                                                     null,
                                                                     vars.iborIndex.dayCounter(),
                                                                     parAssetSwap);
            fixedSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
            var fixedSpecializedBondAssetSwapPrice1 =
               fixedSpecializedBondAssetSwap1.fairCleanPrice();
            var tolerance = 1.0e-13;
            var error1 =
               System.Math.Abs(fixedBondAssetSwapPrice1 - fixedSpecializedBondAssetSwapPrice1);
            if (error1 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  generic  fixed rate bond's  clean price: "
                             + fixedBondAssetSwapPrice1
                             + "\n  equivalent specialized bond's clean price: "
                             + fixedSpecializedBondAssetSwapPrice1
                             + "\n  error:                 " + error1
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var fixedBondMktPrice1 = 91.832;
            var fixedBondASW1 = new AssetSwap(payFixedRate,
                                                    fixedBond1, fixedBondMktPrice1,
                                                    vars.iborIndex, vars.spread,
                                                    null,
                                                    vars.iborIndex.dayCounter(),
                                                    parAssetSwap);
            fixedBondASW1.setPricingEngine(swapEngine);
            var fixedSpecializedBondASW1 = new AssetSwap(payFixedRate,
                                                               fixedSpecializedBond1,
                                                               fixedBondMktPrice1,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            fixedSpecializedBondASW1.setPricingEngine(swapEngine);
            var fixedBondASWSpread1 = fixedBondASW1.fairSpread();
            var fixedSpecializedBondASWSpread1 = fixedSpecializedBondASW1.fairSpread();
            var error2 = System.Math.Abs(fixedBondASWSpread1 - fixedSpecializedBondASWSpread1);
            if (error2 > tolerance)
            {
                QAssert.Fail("wrong asw spread  for fixed bond:"
                             + "\n  generic  fixed rate bond's  asw spread: "
                             + fixedBondASWSpread1
                             + "\n  equivalent specialized bond's asw spread: "
                             + fixedSpecializedBondASWSpread1
                             + "\n  error:                 " + error2
                             + "\n  tolerance:             " + tolerance);
            }

            //Fixed bond (Isin: IT0006527060 IBRD 5 02/05/19)
            //maturity occurs on a business day

            var fixedBondStartDate2 = new Date(5, Month.February, 2005);
            var fixedBondMaturityDate2 = new Date(5, Month.February, 2019);
            var fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                                       fixedBondMaturityDate2,
                                                       new Period(Frequency.Annual), bondCalendar,
                                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                       DateGeneration.Rule.Backward, false);
            List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
            var fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
            fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));

            // generic bond
            var fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                       fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
            fixedBond2.setPricingEngine(bondEngine);

            // equivalent specialized fixed rate bond
            Bond fixedSpecializedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                                                           new List<double> { 0.05 },
                                                           new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                                                           100.0, new Date(5, Month.February, 2005));
            fixedSpecializedBond2.setPricingEngine(bondEngine);

            var fixedBondPrice2 = fixedBond2.cleanPrice();
            var fixedSpecializedBondPrice2 = fixedSpecializedBond2.cleanPrice();
            var fixedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                          fixedBond2, fixedBondPrice2,
                                                          vars.iborIndex, vars.nonnullspread,
                                                          null,
                                                          vars.iborIndex.dayCounter(),
                                                          parAssetSwap);
            fixedBondAssetSwap2.setPricingEngine(swapEngine);
            var fixedSpecializedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                                     fixedSpecializedBond2,
                                                                     fixedSpecializedBondPrice2,
                                                                     vars.iborIndex,
                                                                     vars.nonnullspread,
                                                                     null,
                                                                     vars.iborIndex.dayCounter(),
                                                                     parAssetSwap);
            fixedSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
            var fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
            var fixedSpecializedBondAssetSwapPrice2 = fixedSpecializedBondAssetSwap2.fairCleanPrice();

            var error3 = System.Math.Abs(fixedBondAssetSwapPrice2 - fixedSpecializedBondAssetSwapPrice2);
            if (error3 > tolerance)
            {
                QAssert.Fail("wrong clean price for fixed bond:"
                             + "\n  generic  fixed rate bond's clean price: "
                             + fixedBondAssetSwapPrice2
                             + "\n  equivalent specialized  bond's clean price: "
                             + fixedSpecializedBondAssetSwapPrice2
                             + "\n  error:                 " + error3
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var fixedBondMktPrice2 = 102.178;
            var fixedBondASW2 = new AssetSwap(payFixedRate,
                                                    fixedBond2, fixedBondMktPrice2,
                                                    vars.iborIndex, vars.spread,
                                                    null,
                                                    vars.iborIndex.dayCounter(),
                                                    parAssetSwap);
            fixedBondASW2.setPricingEngine(swapEngine);
            var fixedSpecializedBondASW2 = new AssetSwap(payFixedRate,
                                                               fixedSpecializedBond2,
                                                               fixedBondMktPrice2,
                                                               vars.iborIndex, vars.spread,
                                                               null,
                                                               vars.iborIndex.dayCounter(),
                                                               parAssetSwap);
            fixedSpecializedBondASW2.setPricingEngine(swapEngine);
            var fixedBondASWSpread2 = fixedBondASW2.fairSpread();
            var fixedSpecializedBondASWSpread2 = fixedSpecializedBondASW2.fairSpread();
            var error4 = System.Math.Abs(fixedBondASWSpread2 - fixedSpecializedBondASWSpread2);
            if (error4 > tolerance)
            {
                QAssert.Fail("wrong asw spread for fixed bond:"
                             + "\n  generic  fixed rate bond's  asw spread: "
                             + fixedBondASWSpread2
                             + "\n  equivalent specialized bond's asw spread: "
                             + fixedSpecializedBondASWSpread2
                             + "\n  error:                 " + error4
                             + "\n  tolerance:             " + tolerance);
            }


            //FRN bond (Isin: IT0003543847 ISPIM 0 09/29/13)
            //maturity doesn't occur on a business day
            var floatingBondStartDate1 = new Date(29, Month.September, 2003);
            var floatingBondMaturityDate1 = new Date(29, Month.September, 2013);
            var floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                                          floatingBondMaturityDate1,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption1 = bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
            floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
            // generic bond
            var floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                          floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
            floatingBond1.setPricingEngine(bondEngine);

            // equivalent specialized floater
            Bond floatingSpecializedBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                                 floatingBondSchedule1,
                                                                 vars.iborIndex, new Actual360(),
                                                                 BusinessDayConvention.Following, fixingDays,
                                                                 new List<double> { 1 },
                                                                 new List<double> { 0.0056 },
                                                                 new List<double?>(), new List<double?>(),
                                                                 inArrears,
                                                                 100.0, new Date(29, Month.September, 2003));
            floatingSpecializedBond1.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
            Utils.setCouponPricer(floatingSpecializedBond1.cashflows(), vars.pricer);
            vars.iborIndex.addFixing(new Date(27, Month.March, 2007), 0.0402);
            var floatingBondPrice1 = floatingBond1.cleanPrice();
            var floatingSpecializedBondPrice1 = floatingSpecializedBond1.cleanPrice();
            var floatingBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                             floatingBond1, floatingBondPrice1,
                                                             vars.iborIndex, vars.nonnullspread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            floatingBondAssetSwap1.setPricingEngine(swapEngine);
            var floatingSpecializedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                                        floatingSpecializedBond1,
                                                                        floatingSpecializedBondPrice1,
                                                                        vars.iborIndex,
                                                                        vars.nonnullspread,
                                                                        null,
                                                                        vars.iborIndex.dayCounter(),
                                                                        parAssetSwap);
            floatingSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
            var floatingSpecializedBondAssetSwapPrice1 =
               floatingSpecializedBondAssetSwap1.fairCleanPrice();

            var error5 =
               System.Math.Abs(floatingBondAssetSwapPrice1 - floatingSpecializedBondAssetSwapPrice1);
            if (error5 > tolerance)
            {
                QAssert.Fail("wrong clean price for frnbond:"
                             + "\n  generic frn rate bond's clean price: "
                             + floatingBondAssetSwapPrice1
                             + "\n  equivalent specialized  bond's price: "
                             + floatingSpecializedBondAssetSwapPrice1
                             + "\n  error:                 " + error5
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var floatingBondMktPrice1 = 101.33;
            var floatingBondASW1 = new AssetSwap(payFixedRate,
                                                       floatingBond1, floatingBondMktPrice1,
                                                       vars.iborIndex, vars.spread,
                                                       null,
                                                       vars.iborIndex.dayCounter(),
                                                       parAssetSwap);
            floatingBondASW1.setPricingEngine(swapEngine);
            var floatingSpecializedBondASW1 = new AssetSwap(payFixedRate,
                                                                  floatingSpecializedBond1,
                                                                  floatingBondMktPrice1,
                                                                  vars.iborIndex, vars.spread,
                                                                  null,
                                                                  vars.iborIndex.dayCounter(),
                                                                  parAssetSwap);
            floatingSpecializedBondASW1.setPricingEngine(swapEngine);
            var floatingBondASWSpread1 = floatingBondASW1.fairSpread();
            var floatingSpecializedBondASWSpread1 =
               floatingSpecializedBondASW1.fairSpread();
            var error6 =
               System.Math.Abs(floatingBondASWSpread1 - floatingSpecializedBondASWSpread1);
            if (error6 > tolerance)
            {
                QAssert.Fail("wrong asw spread for fixed bond:"
                             + "\n  generic  frn rate bond's  asw spread: "
                             + floatingBondASWSpread1
                             + "\n  equivalent specialized bond's asw spread: "
                             + floatingSpecializedBondASWSpread1
                             + "\n  error:                 " + error6
                             + "\n  tolerance:             " + tolerance);
            }
            //FRN bond (Isin: XS0090566539 COE 0 09/24/18)
            //maturity occurs on a business day
            var floatingBondStartDate2 = new Date(24, Month.September, 2004);
            var floatingBondMaturityDate2 = new Date(24, Month.September, 2018);
            var floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                                          floatingBondMaturityDate2,
                                                          new Period(Frequency.Semiannual), bondCalendar,
                                                          BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                          DateGeneration.Rule.Backward, false);
            List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
            var floatingbondRedemption2 = bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
            floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
            // generic bond
            var floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                           floatingBondMaturityDate2, floatingBondStartDate2, floatingBondLeg2);
            floatingBond2.setPricingEngine(bondEngine);

            // equivalent specialized floater
            Bond floatingSpecializedBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                                 floatingBondSchedule2,
                                                                 vars.iborIndex, new Actual360(),
                                                                 BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                                 new List<double> { 1 },
                                                                 new List<double> { 0.0025 },
                                                                 new List<double?>(), new List<double?>(),
                                                                 inArrears,
                                                                 100.0, new Date(24, Month.September, 2004));
            floatingSpecializedBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
            Utils.setCouponPricer(floatingSpecializedBond2.cashflows(), vars.pricer);

            vars.iborIndex.addFixing(new Date(22, Month.March, 2007), 0.04013);

            var floatingBondPrice2 = floatingBond2.cleanPrice();
            var floatingSpecializedBondPrice2 = floatingSpecializedBond2.cleanPrice();
            var floatingBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                             floatingBond2, floatingBondPrice2,
                                                             vars.iborIndex, vars.nonnullspread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            floatingBondAssetSwap2.setPricingEngine(swapEngine);
            var floatingSpecializedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                                        floatingSpecializedBond2,
                                                                        floatingSpecializedBondPrice2,
                                                                        vars.iborIndex,
                                                                        vars.nonnullspread,
                                                                        null,
                                                                        vars.iborIndex.dayCounter(),
                                                                        parAssetSwap);
            floatingSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
            var floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
            var floatingSpecializedBondAssetSwapPrice2 =
               floatingSpecializedBondAssetSwap2.fairCleanPrice();
            var error7 =
               System.Math.Abs(floatingBondAssetSwapPrice2 - floatingSpecializedBondAssetSwapPrice2);
            if (error7 > tolerance)
            {
                QAssert.Fail("wrong clean price for frnbond:"
                             + "\n  generic frn rate bond's clean price: "
                             + floatingBondAssetSwapPrice2
                             + "\n  equivalent specialized frn  bond's price: "
                             + floatingSpecializedBondAssetSwapPrice2
                             + "\n  error:                 " + error7
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var floatingBondMktPrice2 = 101.26;
            var floatingBondASW2 = new AssetSwap(payFixedRate,
                                                       floatingBond2, floatingBondMktPrice2,
                                                       vars.iborIndex, vars.spread,
                                                       null,
                                                       vars.iborIndex.dayCounter(),
                                                       parAssetSwap);
            floatingBondASW2.setPricingEngine(swapEngine);
            var floatingSpecializedBondASW2 = new AssetSwap(payFixedRate,
                                                                  floatingSpecializedBond2,
                                                                  floatingBondMktPrice2,
                                                                  vars.iborIndex, vars.spread,
                                                                  null,
                                                                  vars.iborIndex.dayCounter(),
                                                                  parAssetSwap);
            floatingSpecializedBondASW2.setPricingEngine(swapEngine);
            var floatingBondASWSpread2 = floatingBondASW2.fairSpread();
            var floatingSpecializedBondASWSpread2 =
               floatingSpecializedBondASW2.fairSpread();
            var error8 =
               System.Math.Abs(floatingBondASWSpread2 - floatingSpecializedBondASWSpread2);
            if (error8 > tolerance)
            {
                QAssert.Fail("wrong asw spread for frn bond:"
                             + "\n  generic  frn rate bond's  asw spread: "
                             + floatingBondASWSpread2
                             + "\n  equivalent specialized bond's asw spread: "
                             + floatingSpecializedBondASWSpread2
                             + "\n  error:                 " + error8
                             + "\n  tolerance:             " + tolerance);
            }

            // CMS bond (Isin: XS0228052402 CRDIT 0 8/22/20)
            // maturity doesn't occur on a business day
            var cmsBondStartDate1 = new Date(22, Month.August, 2005);
            var cmsBondMaturityDate1 = new Date(22, Month.August, 2020);
            var cmsBondSchedule1 = new Schedule(cmsBondStartDate1,
                                                     cmsBondMaturityDate1,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
            cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
            // generic cms bond
            var cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                      cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
            cmsBond1.setPricingEngine(bondEngine);

            // equivalent specialized cms bond
            Bond cmsSpecializedBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                                                       vars.swapIndex, new Thirty360(),
                                                       BusinessDayConvention.Following, fixingDays,
                                                       new List<double> { 1.0 }, new List<double> { 0.0 },
                                                       new List<double?> { 0.055 }, new List<double?> { 0.025 },
                                                       inArrears,
                                                       100.0, new Date(22, Month.August, 2005));
            cmsSpecializedBond1.setPricingEngine(bondEngine);


            Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
            Utils.setCouponPricer(cmsSpecializedBond1.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(18, Month.August, 2006), 0.04158);
            var cmsBondPrice1 = cmsBond1.cleanPrice();
            var cmsSpecializedBondPrice1 = cmsSpecializedBond1.cleanPrice();
            var cmsBondAssetSwap1 = new AssetSwap(payFixedRate, cmsBond1, cmsBondPrice1,
                                                        vars.iborIndex, vars.nonnullspread,
                                                        null, vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            cmsBondAssetSwap1.setPricingEngine(swapEngine);
            var cmsSpecializedBondAssetSwap1 = new AssetSwap(payFixedRate, cmsSpecializedBond1,
                                                                   cmsSpecializedBondPrice1,
                                                                   vars.iborIndex,
                                                                   vars.nonnullspread,
                                                                   null,
                                                                   vars.iborIndex.dayCounter(),
                                                                   parAssetSwap);
            cmsSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
            var cmsSpecializedBondAssetSwapPrice1 =
               cmsSpecializedBondAssetSwap1.fairCleanPrice();
            var error9 =
               System.Math.Abs(cmsBondAssetSwapPrice1 - cmsSpecializedBondAssetSwapPrice1);
            if (error9 > tolerance)
            {
                QAssert.Fail("wrong clean price for cmsbond:"
                             + "\n  generic bond's clean price: "
                             + cmsBondAssetSwapPrice1
                             + "\n  equivalent specialized cms rate bond's price: "
                             + cmsSpecializedBondAssetSwapPrice1
                             + "\n  error:                 " + error9
                             + "\n  tolerance:             " + tolerance);
            }
            var cmsBondMktPrice1 = 87.02;// market executable price as of 4th sept 2007
            var cmsBondASW1 = new AssetSwap(payFixedRate,
                                                  cmsBond1, cmsBondMktPrice1,
                                                  vars.iborIndex, vars.spread,
                                                  null,
                                                  vars.iborIndex.dayCounter(),
                                                  parAssetSwap);
            cmsBondASW1.setPricingEngine(swapEngine);
            var cmsSpecializedBondASW1 = new AssetSwap(payFixedRate,
                                                             cmsSpecializedBond1,
                                                             cmsBondMktPrice1,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            cmsSpecializedBondASW1.setPricingEngine(swapEngine);
            var cmsBondASWSpread1 = cmsBondASW1.fairSpread();
            var cmsSpecializedBondASWSpread1 = cmsSpecializedBondASW1.fairSpread();
            var error10 = System.Math.Abs(cmsBondASWSpread1 - cmsSpecializedBondASWSpread1);
            if (error10 > tolerance)
            {
                QAssert.Fail("wrong asw spread for cm bond:"
                             + "\n  generic cms rate bond's  asw spread: "
                             + cmsBondASWSpread1
                             + "\n  equivalent specialized bond's asw spread: "
                             + cmsSpecializedBondASWSpread1
                             + "\n  error:                 " + error10
                             + "\n  tolerance:             " + tolerance);
            }

            //CMS bond (Isin: XS0218766664 ISPIM 0 5/6/15)
            //maturity occurs on a business day
            var cmsBondStartDate2 = new Date(06, Month.May, 2005);
            var cmsBondMaturityDate2 = new Date(06, Month.May, 2015);
            var cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                                     cmsBondMaturityDate2,
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
            List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
            var cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2,
                                                          BusinessDayConvention.Following);
            cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
            // generic bond
            var cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                      cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
            cmsBond2.setPricingEngine(bondEngine);

            // equivalent specialized cms bond
            Bond cmsSpecializedBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                                                        vars.swapIndex, new Thirty360(),
                                                        BusinessDayConvention.Following, fixingDays,
                                                        new List<double> { 0.84 }, new List<double> { 0.0 },
                                                        new List<double?>(), new List<double?>(),
                                                        inArrears,
                                                        100.0, new Date(06, Month.May, 2005));
            cmsSpecializedBond2.setPricingEngine(bondEngine);

            Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
            Utils.setCouponPricer(cmsSpecializedBond2.cashflows(), vars.cmspricer);
            vars.swapIndex.addFixing(new Date(04, Month.May, 2006), 0.04217);
            var cmsBondPrice2 = cmsBond2.cleanPrice();
            var cmsSpecializedBondPrice2 = cmsSpecializedBond2.cleanPrice();
            var cmsBondAssetSwap2 = new AssetSwap(payFixedRate, cmsBond2, cmsBondPrice2,
                                                        vars.iborIndex, vars.nonnullspread,
                                                        null,
                                                        vars.iborIndex.dayCounter(),
                                                        parAssetSwap);
            cmsBondAssetSwap2.setPricingEngine(swapEngine);
            var cmsSpecializedBondAssetSwap2 = new AssetSwap(payFixedRate, cmsSpecializedBond2,
                                                                   cmsSpecializedBondPrice2,
                                                                   vars.iborIndex,
                                                                   vars.nonnullspread,
                                                                   null,
                                                                   vars.iborIndex.dayCounter(),
                                                                   parAssetSwap);
            cmsSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
            var cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
            var cmsSpecializedBondAssetSwapPrice2 =
               cmsSpecializedBondAssetSwap2.fairCleanPrice();
            var error11 =
               System.Math.Abs(cmsBondAssetSwapPrice2 - cmsSpecializedBondAssetSwapPrice2);
            if (error11 > tolerance)
            {
                QAssert.Fail("wrong clean price for cmsbond:"
                             + "\n  generic  bond's clean price: "
                             + cmsBondAssetSwapPrice2
                             + "\n  equivalent specialized cms rate bond's price: "
                             + cmsSpecializedBondAssetSwapPrice2
                             + "\n  error:                 " + error11
                             + "\n  tolerance:             " + tolerance);
            }
            var cmsBondMktPrice2 = 94.35;// market executable price as of 4th sept 2007
            var cmsBondASW2 = new AssetSwap(payFixedRate,
                                                  cmsBond2, cmsBondMktPrice2,
                                                  vars.iborIndex, vars.spread,
                                                  null,
                                                  vars.iborIndex.dayCounter(),
                                                  parAssetSwap);
            cmsBondASW2.setPricingEngine(swapEngine);
            var cmsSpecializedBondASW2 = new AssetSwap(payFixedRate,
                                                             cmsSpecializedBond2,
                                                             cmsBondMktPrice2,
                                                             vars.iborIndex, vars.spread,
                                                             null,
                                                             vars.iborIndex.dayCounter(),
                                                             parAssetSwap);
            cmsSpecializedBondASW2.setPricingEngine(swapEngine);
            var cmsBondASWSpread2 = cmsBondASW2.fairSpread();
            var cmsSpecializedBondASWSpread2 = cmsSpecializedBondASW2.fairSpread();
            var error12 = System.Math.Abs(cmsBondASWSpread2 - cmsSpecializedBondASWSpread2);
            if (error12 > tolerance)
            {
                QAssert.Fail("wrong asw spread for cm bond:"
                             + "\n  generic cms rate bond's  asw spread: "
                             + cmsBondASWSpread2
                             + "\n  equivalent specialized bond's asw spread: "
                             + cmsSpecializedBondASWSpread2
                             + "\n  error:                 " + error12
                             + "\n  tolerance:             " + tolerance);
            }


            //  Zero-Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
            //  maturity doesn't occur on a business day
            var zeroCpnBondStartDate1 = new Date(19, Month.December, 1985);
            var zeroCpnBondMaturityDate1 = new Date(20, Month.December, 2015);
            var zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                              BusinessDayConvention.Following);
            var zeroCpnBondLeg1 = new List<CashFlow> { new SimpleCashFlow(100.0, zeroCpnBondRedemption1) };
            // generic bond
            var zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
            zeroCpnBond1.setPricingEngine(bondEngine);

            // specialized zerocpn bond
            Bond zeroCpnSpecializedBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                              new Date(20, Month.December, 2015),
                                                              BusinessDayConvention.Following,
                                                              100.0, new Date(19, Month.December, 1985));
            zeroCpnSpecializedBond1.setPricingEngine(bondEngine);

            var zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
            var zeroCpnSpecializedBondPrice1 = zeroCpnSpecializedBond1.cleanPrice();
            var zeroCpnBondAssetSwap1 = new AssetSwap(payFixedRate, zeroCpnBond1,
                                                            zeroCpnBondPrice1,
                                                            vars.iborIndex, vars.nonnullspread,
                                                            null,
                                                            vars.iborIndex.dayCounter(),
                                                            parAssetSwap);
            zeroCpnBondAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnSpecializedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                                       zeroCpnSpecializedBond1,
                                                                       zeroCpnSpecializedBondPrice1,
                                                                       vars.iborIndex,
                                                                       vars.nonnullspread,
                                                                       null,
                                                                       vars.iborIndex.dayCounter(),
                                                                       parAssetSwap);
            zeroCpnSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice1 = zeroCpnBondAssetSwap1.fairCleanPrice();
            var zeroCpnSpecializedBondAssetSwapPrice1 =
               zeroCpnSpecializedBondAssetSwap1.fairCleanPrice();
            var error13 =
               System.Math.Abs(zeroCpnBondAssetSwapPrice1 - zeroCpnSpecializedBondAssetSwapPrice1);
            if (error13 > tolerance)
            {
                QAssert.Fail("wrong clean price for zerocpn bond:"
                             + "\n  generic zero cpn bond's clean price: "
                             + zeroCpnBondAssetSwapPrice1
                             + "\n  specialized equivalent bond's price: "
                             + zeroCpnSpecializedBondAssetSwapPrice1
                             + "\n  error:                 " + error13
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var zeroCpnBondMktPrice1 = 72.277;
            var zeroCpnBondASW1 = new AssetSwap(payFixedRate,
                                                      zeroCpnBond1, zeroCpnBondMktPrice1,
                                                      vars.iborIndex, vars.spread,
                                                      null,
                                                      vars.iborIndex.dayCounter(),
                                                      parAssetSwap);
            zeroCpnBondASW1.setPricingEngine(swapEngine);
            var zeroCpnSpecializedBondASW1 = new AssetSwap(payFixedRate,
                                                                 zeroCpnSpecializedBond1,
                                                                 zeroCpnBondMktPrice1,
                                                                 vars.iborIndex, vars.spread,
                                                                 null,
                                                                 vars.iborIndex.dayCounter(),
                                                                 parAssetSwap);
            zeroCpnSpecializedBondASW1.setPricingEngine(swapEngine);
            var zeroCpnBondASWSpread1 = zeroCpnBondASW1.fairSpread();
            var zeroCpnSpecializedBondASWSpread1 =
               zeroCpnSpecializedBondASW1.fairSpread();
            var error14 =
               System.Math.Abs(zeroCpnBondASWSpread1 - zeroCpnSpecializedBondASWSpread1);
            if (error14 > tolerance)
            {
                QAssert.Fail("wrong asw spread for zeroCpn bond:"
                             + "\n  generic zeroCpn bond's  asw spread: "
                             + zeroCpnBondASWSpread1
                             + "\n  equivalent specialized bond's asw spread: "
                             + zeroCpnSpecializedBondASWSpread1
                             + "\n  error:                 " + error14
                             + "\n  tolerance:             " + tolerance);
            }


            //  Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
            //  maturity doesn't occur on a business day
            var zeroCpnBondStartDate2 = new Date(17, Month.February, 1998);
            var zeroCpnBondMaturityDate2 = new Date(17, Month.February, 2028);
            var zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                             BusinessDayConvention.Following);
            var zeroCpnBondLeg2 = new List<CashFlow> { new SimpleCashFlow(100.0, zerocpbondRedemption2) };
            // generic bond
            var zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                                         zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
            zeroCpnBond2.setPricingEngine(bondEngine);

            // specialized zerocpn bond
            Bond zeroCpnSpecializedBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                              new Date(17, Month.February, 2028),
                                                              BusinessDayConvention.Following,
                                                              100.0, new Date(17, Month.February, 1998));
            zeroCpnSpecializedBond2.setPricingEngine(bondEngine);

            var zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
            var zeroCpnSpecializedBondPrice2 = zeroCpnSpecializedBond2.cleanPrice();

            var zeroCpnBondAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2,
                                                            zeroCpnBondPrice2,
                                                            vars.iborIndex, vars.nonnullspread,
                                                            null,
                                                            vars.iborIndex.dayCounter(),
                                                            parAssetSwap);
            zeroCpnBondAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnSpecializedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                                       zeroCpnSpecializedBond2,
                                                                       zeroCpnSpecializedBondPrice2,
                                                                       vars.iborIndex,
                                                                       vars.nonnullspread,
                                                                       null,
                                                                       vars.iborIndex.dayCounter(),
                                                                       parAssetSwap);
            zeroCpnSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
            var zeroCpnBondAssetSwapPrice2 = zeroCpnBondAssetSwap2.fairCleanPrice();
            var zeroCpnSpecializedBondAssetSwapPrice2 =
               zeroCpnSpecializedBondAssetSwap2.fairCleanPrice();
            var error15 = System.Math.Abs(zeroCpnBondAssetSwapPrice2
                                          - zeroCpnSpecializedBondAssetSwapPrice2);
            if (error15 > tolerance)
            {
                QAssert.Fail("wrong clean price for zerocpn bond:"
                             + "\n  generic zero cpn bond's clean price: "
                             + zeroCpnBondAssetSwapPrice2
                             + "\n  equivalent specialized bond's price: "
                             + zeroCpnSpecializedBondAssetSwapPrice2
                             + "\n  error:                 " + error15
                             + "\n  tolerance:             " + tolerance);
            }
            // market executable price as of 4th sept 2007
            var zeroCpnBondMktPrice2 = 72.277;
            var zeroCpnBondASW2 = new AssetSwap(payFixedRate,
                                                      zeroCpnBond2, zeroCpnBondMktPrice2,
                                                      vars.iborIndex, vars.spread,
                                                      null,
                                                      vars.iborIndex.dayCounter(),
                                                      parAssetSwap);
            zeroCpnBondASW2.setPricingEngine(swapEngine);
            var zeroCpnSpecializedBondASW2 = new AssetSwap(payFixedRate,
                                                                 zeroCpnSpecializedBond2,
                                                                 zeroCpnBondMktPrice2,
                                                                 vars.iborIndex, vars.spread,
                                                                 null,
                                                                 vars.iborIndex.dayCounter(),
                                                                 parAssetSwap);
            zeroCpnSpecializedBondASW2.setPricingEngine(swapEngine);
            var zeroCpnBondASWSpread2 = zeroCpnBondASW2.fairSpread();
            var zeroCpnSpecializedBondASWSpread2 =
               zeroCpnSpecializedBondASW2.fairSpread();
            var error16 =
               System.Math.Abs(zeroCpnBondASWSpread2 - zeroCpnSpecializedBondASWSpread2);
            if (error16 > tolerance)
            {
                QAssert.Fail("wrong asw spread for zeroCpn bond:"
                             + "\n  generic zeroCpn bond's  asw spread: "
                             + zeroCpnBondASWSpread2
                             + "\n  equivalent specialized bond's asw spread: "
                             + zeroCpnSpecializedBondASWSpread2
                             + "\n  error:                 " + error16
                             + "\n  tolerance:             " + tolerance);
            }
        }




    }
}
