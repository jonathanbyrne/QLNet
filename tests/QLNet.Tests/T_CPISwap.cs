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
using QLNet.Time;
using QLNet.Indexes;
using QLNet.Termstructures.Inflation;
using QLNet.Instruments;
using QLNet.Instruments.Bonds;
using QLNet.Math.Interpolations;
using QLNet.Termstructures;
using QLNet.PricingEngines.Swap;
using QLNet.PricingEngines.Bond;
using QLNet.Quotes;
using QLNet.Cashflows;
using QLNet.Indexes.Ibor;
using QLNet.Indexes.Inflation;
using QLNet.Termstructures.Yield;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Tests
{
    [Collection("QLNet CI Tests")]
    [JetBrains.Annotations.PublicAPI] public class T_CPISwap
    {
        internal struct Datum
        {
            public Date date;
            public double rate;

            public Datum(Date d, double r)
            {
                date = d;
                rate = r;
            }
        }

        private class CommonVars
        {
            private List<BootstrapHelper<ZeroInflationTermStructure>> makeHelpers(Datum[] iiData, int N,
                                                                                  ZeroInflationIndex ii, Period observationLag,
                                                                                  Calendar calendar,
                                                                                  BusinessDayConvention bdc,
                                                                                  DayCounter dc)
            {
                var instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
                for (var i = 0; i < N; i++)
                {
                    var maturity = iiData[i].date;
                    var quote = new Handle<Quote>(new SimpleQuote(iiData[i].rate / 100.0));
                    BootstrapHelper<ZeroInflationTermStructure> anInstrument = new ZeroCouponInflationSwapHelper(quote, observationLag, maturity,
                          calendar, bdc, dc, ii);
                    instruments.Add(anInstrument);
                }
                return instruments;
            }

            // common data
            public int length;
            public Date startDate;
            public double volatility;

            public Frequency frequency;
            public List<double> nominals;
            public Calendar calendar;
            public BusinessDayConvention convention;
            public int fixingDays;
            public Date evaluationDate;
            public int settlementDays;
            public Date settlement;
            public Period observationLag, contractObservationLag;
            public InterpolationType contractObservationInterpolation;
            public DayCounter dcZCIIS, dcNominal;
            public List<Date> zciisD;
            public List<double> zciisR;
            public UKRPI ii;
            public RelinkableHandle<ZeroInflationIndex> hii;
            public int zciisDataLength;

            public RelinkableHandle<YieldTermStructure> nominalUK;
            public RelinkableHandle<ZeroInflationTermStructure> cpiUK;
            public RelinkableHandle<ZeroInflationTermStructure> hcpi;


            // cleanup

            public SavedSettings backup;
            public IndexHistoryCleaner cleaner;

            // setup
            public CommonVars()
            {
                backup = new SavedSettings();
                cleaner = new IndexHistoryCleaner();
                nominalUK = new RelinkableHandle<YieldTermStructure>();
                cpiUK = new RelinkableHandle<ZeroInflationTermStructure>();
                hcpi = new RelinkableHandle<ZeroInflationTermStructure>();
                zciisD = new List<Date>();
                zciisR = new List<double>();
                hii = new RelinkableHandle<ZeroInflationIndex>();

                nominals = new InitializedList<double>(1, 1000000);
                // option variables
                frequency = Frequency.Annual;
                // usual setup
                volatility = 0.01;
                length = 7;
                calendar = new UnitedKingdom();
                convention = BusinessDayConvention.ModifiedFollowing;
                var today = new Date(25, Month.November, 2009);
                evaluationDate = calendar.adjust(today);
                Settings.setEvaluationDate(evaluationDate);
                settlementDays = 0;
                fixingDays = 0;
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
                startDate = settlement;
                dcZCIIS = new ActualActual();
                dcNominal = new ActualActual();

                // uk rpi index
                //      fixing data
                var from = new Date(20, Month.July, 2007);
                var to = new Date(20, Month.November, 2009);
                var rpiSchedule = new MakeSchedule().from(from).to(to)
                .withTenor(new Period(1, TimeUnit.Months))
                .withCalendar(new UnitedKingdom())
                .withConvention(BusinessDayConvention.ModifiedFollowing).value();
                double[] fixData =
                {
               206.1, 207.3, 208.0, 208.9, 209.7, 210.9,
               209.8, 211.4, 212.1, 214.0, 215.1, 216.8,
               216.5, 217.2, 218.4, 217.7, 216,
               212.9, 210.1, 211.4, 211.3, 211.5,
               212.8, 213.4, 213.4, 213.4, 214.4,
               -999.0, -999.0
            };

                // link from cpi index to cpi TS
                var interp = false;// this MUST be false because the observation lag is only 2 months
                                    // for ZCIIS; but not for contract if the contract uses a bigger lag.
                ii = new UKRPI(interp, hcpi);
                for (var i = 0; i < rpiSchedule.Count; i++)
                {
                    ii.addFixing(rpiSchedule[i], fixData[i], true);  // force overwrite in case multiple use
                }

                Datum[] nominalData =
                {
               new Datum(new Date(26, Month.November, 2009), 0.475),
               new Datum(new Date(2, Month.December, 2009), 0.47498),
               new Datum(new Date(29, Month.December, 2009), 0.49988),
               new Datum(new Date(25, Month.February, 2010), 0.59955),
               new Datum(new Date(18, Month.March, 2010), 0.65361),
               new Datum(new Date(25, Month.May, 2010), 0.82830),
               new Datum(new Date(17, Month.June, 2010), 0.7),
               new Datum(new Date(16, Month.September, 2010), 0.78960),
               new Datum(new Date(16, Month.December, 2010), 0.93762),
               new Datum(new Date(17, Month.March, 2011), 1.12037),
               new Datum(new Date(22, Month.September, 2011), 1.52011),
               new Datum(new Date(25, Month.November, 2011), 1.78399),
               new Datum(new Date(26, Month.November, 2012), 2.41170),
               new Datum(new Date(25, Month.November, 2013), 2.83935),
               new Datum(new Date(25, Month.November, 2014), 3.12888),
               new Datum(new Date(25, Month.November, 2015), 3.34298),
               new Datum(new Date(25, Month.November, 2016), 3.50632),
               new Datum(new Date(27, Month.November, 2017), 3.63666),
               new Datum(new Date(26, Month.November, 2018), 3.74723),
               new Datum(new Date(25, Month.November, 2019), 3.83988),
               new Datum(new Date(25, Month.November, 2021), 4.00508),
               new Datum(new Date(25, Month.November, 2024), 4.16042),
               new Datum(new Date(26, Month.November, 2029), 4.15577),
               new Datum(new Date(27, Month.November, 2034), 4.04933),
               new Datum(new Date(25, Month.November, 2039), 3.95217),
               new Datum(new Date(25, Month.November, 2049), 3.80932),
               new Datum(new Date(25, Month.November, 2059), 3.80849),
               new Datum(new Date(25, Month.November, 2069), 3.72677),
               new Datum(new Date(27, Month.November, 2079), 3.63082)

            };
                var nominalDataLength = 30 - 1;

                var nomD = new List<Date>();
                var nomR = new List<double>();
                for (var i = 0; i < nominalDataLength; i++)
                {
                    nomD.Add(nominalData[i].date);
                    nomR.Add(nominalData[i].rate / 100.0);
                }
                YieldTermStructure nominal = new InterpolatedZeroCurve<Linear>(nomD, nomR, dcNominal);
                nominalUK.linkTo(nominal);

                // now build the zero inflation curve
                observationLag = new Period(2, TimeUnit.Months);
                contractObservationLag = new Period(3, TimeUnit.Months);
                contractObservationInterpolation = InterpolationType.Flat;

                Datum[] zciisData =
                {
               new Datum(new Date(25, Month.November, 2010), 3.0495),
               new Datum(new Date(25, Month.November, 2011), 2.93),
               new Datum(new Date(26, Month.November, 2012), 2.9795),
               new Datum(new Date(25, Month.November, 2013), 3.029),
               new Datum(new Date(25, Month.November, 2014), 3.1425),
               new Datum(new Date(25, Month.November, 2015), 3.211),
               new Datum(new Date(25, Month.November, 2016), 3.2675),
               new Datum(new Date(25, Month.November, 2017), 3.3625),
               new Datum(new Date(25, Month.November, 2018), 3.405),
               new Datum(new Date(25, Month.November, 2019), 3.48),
               new Datum(new Date(25, Month.November, 2021), 3.576),
               new Datum(new Date(25, Month.November, 2024), 3.649),
               new Datum(new Date(26, Month.November, 2029), 3.751),
               new Datum(new Date(27, Month.November, 2034), 3.77225),
               new Datum(new Date(25, Month.November, 2039), 3.77),
               new Datum(new Date(25, Month.November, 2049), 3.734),
               new Datum(new Date(25, Month.November, 2059), 3.714)
            };
                zciisDataLength = 17;
                for (var i = 0; i < zciisDataLength; i++)
                {
                    zciisD.Add(zciisData[i].date);
                    zciisR.Add(zciisData[i].rate);
                }

                // now build the helpers ...
                var helpers = makeHelpers(zciisData, zciisDataLength, ii,
                                                                                        observationLag, calendar, convention, dcZCIIS);

                // we can use historical or first ZCIIS for this
                // we know historical is WAY off market-implied, so use market implied flat.
                var baseZeroRate = zciisData[0].rate / 100.0;
                var pCPIts = new PiecewiseZeroInflationCurve<Linear>(
                   evaluationDate, calendar, dcZCIIS, observationLag, ii.frequency(), ii.interpolated(), baseZeroRate,
                   new Handle<YieldTermStructure>(nominalUK), helpers);
                pCPIts.recalculate();
                cpiUK.linkTo(pCPIts);

                // make sure that the index has the latest zero inflation term structure
                hcpi.linkTo(pCPIts);

            }
        }

        [Fact]
        public void consistency()
        {
            // check inflation leg vs calculation directly from inflation TS
            var common = new CommonVars();

            // ZeroInflationSwap aka CPISwap
            var type = CPISwap.Type.Payer;
            var nominal = 1000000.0;
            var subtractInflationNominal = true;
            // float+spread leg
            var spread = 0.0;
            DayCounter floatDayCount = new Actual365Fixed();
            var floatPaymentConvention = BusinessDayConvention.ModifiedFollowing;
            var fixingDays = 0;
            IborIndex floatIndex = new GBPLibor(new Period(6, TimeUnit.Months), common.nominalUK);

            // fixed x inflation leg
            var fixedRate = 0.1;//1% would be 0.01
            var baseCPI = 206.1; // would be 206.13871 if we were interpolating
            DayCounter fixedDayCount = new Actual365Fixed();
            var fixedPaymentConvention = BusinessDayConvention.ModifiedFollowing;
            Calendar fixedPaymentCalendar = new UnitedKingdom();
            ZeroInflationIndex fixedIndex = common.ii;
            var contractObservationLag = common.contractObservationLag;
            var observationInterpolation = common.contractObservationInterpolation;

            // set the schedules
            var startDate = new Date(2, Month.October, 2007);
            var endDate = new Date(2, Month.October, 2052);
            var floatSchedule = new MakeSchedule().from(startDate).to(endDate)
            .withTenor(new Period(6, TimeUnit.Months))
            .withCalendar(new UnitedKingdom())
            .withConvention(floatPaymentConvention)
            .backwards().value();
            var fixedSchedule = new MakeSchedule().from(startDate).to(endDate)
            .withTenor(new Period(6, TimeUnit.Months))
            .withCalendar(new UnitedKingdom())
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().value();


            var zisV = new CPISwap(type, nominal, subtractInflationNominal,
                                       spread, floatDayCount, floatSchedule,
                                       floatPaymentConvention, fixingDays, floatIndex,
                                       fixedRate, baseCPI, fixedDayCount, fixedSchedule,
                                       fixedPaymentConvention, contractObservationLag,
                                       fixedIndex, observationInterpolation);
            var asofDate = Settings.evaluationDate();

            double[] floatFix = { 0.06255, 0.05975, 0.0637, 0.018425, 0.0073438, -1, -1 };
            double[] cpiFix = { 211.4, 217.2, 211.4, 213.4, -2, -2 };
            for (var i = 0; i < floatSchedule.Count; i++)
            {
                if (floatSchedule[i] < common.evaluationDate)
                {
                    floatIndex.addFixing(floatSchedule[i], floatFix[i], true); //true=overwrite
                }

                var zic = zisV.cpiLeg()[i] as CPICoupon;
                if (zic != null)
                {
                    if (zic.fixingDate() < common.evaluationDate - new Period(1, TimeUnit.Months))
                    {
                        fixedIndex.addFixing(zic.fixingDate(), cpiFix[i], true);
                    }
                }
            }

            // simple structure so simple pricing engine - most work done by index
            var dse = new DiscountingSwapEngine(common.nominalUK);

            zisV.setPricingEngine(dse);

            // get float+spread & fixed*inflation leg prices separately
            var testInfLegNPV = 0.0;
            double diff;
            for (var i = 0; i < zisV.leg(0).Count; i++)
            {
                var zicPayDate = zisV.leg(0)[i].date();
                if (zicPayDate > asofDate)
                {
                    testInfLegNPV += zisV.leg(0)[i].amount() * common.nominalUK.link.discount(zicPayDate);
                }

                var zicV = zisV.cpiLeg()[i] as CPICoupon;
                if (zicV != null)
                {
                    diff = System.Math.Abs(zicV.rate() - fixedRate * (zicV.indexFixing() / baseCPI));
                    QAssert.IsTrue(diff < 1e-8, "failed " + i + "th coupon reconstruction as "
                                   + fixedRate * (zicV.indexFixing() / baseCPI) + " vs rate = "
                                   + zicV.rate() + ", with difference: " + diff);
                }
            }

            var error = System.Math.Abs(testInfLegNPV - zisV.legNPV(0).Value);
            QAssert.IsTrue(error < 1e-5, "failed manual inf leg NPV calc vs pricing engine: " + testInfLegNPV + " vs " +
                           zisV.legNPV(0));

            diff = System.Math.Abs(1 - zisV.NPV() / 4191660.0);
#if QL_USE_INDEXED_COUPON
         double max_diff = 1e-5;
#else
            var max_diff = 3e-5;
#endif
            QAssert.IsTrue(diff < max_diff, "failed stored consistency value test, ratio = " + diff);

            // remove circular refernce
            common.hcpi.linkTo(null);
        }

        [Fact]
        public void zciisconsistency()
        {
            var common = new CommonVars();

            var ztype = ZeroCouponInflationSwap.Type.Payer;
            var nominal = 1000000.0;
            var startDate = new Date(common.evaluationDate);
            var endDate = new Date(25, Month.November, 2059);
            Calendar cal = new UnitedKingdom();
            var paymentConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter dummyDC = null, dc = new ActualActual();
            var observationLag = new Period(2, TimeUnit.Months);

            var quote = 0.03714;
            var zciis = new ZeroCouponInflationSwap(ztype, nominal, startDate, endDate, cal,
                                                                        paymentConvention, dc, quote, common.ii, observationLag);

            // simple structure so simple pricing engine - most work done by index
            var dse = new DiscountingSwapEngine(common.nominalUK);

            zciis.setPricingEngine(dse);
            QAssert.IsTrue(System.Math.Abs(zciis.NPV()) < 1e-3, "zciis does not reprice to zero");

            var oneDate = new List<Date>();
            oneDate.Add(endDate);
            var schOneDate = new Schedule(oneDate, cal, paymentConvention);

            var stype = CPISwap.Type.Payer;
            var inflationNominal = nominal;
            var floatNominal = inflationNominal * System.Math.Pow(1.0 + quote, 50);
            var subtractInflationNominal = true;
            double dummySpread = 0.0, dummyFixedRate = 0.0;
            var fixingDays = 0;
            var baseDate = startDate - observationLag;
            var baseCPI = common.ii.fixing(baseDate);

            var dummyFloatIndex = new IborIndex();

            var cS = new CPISwap(stype, floatNominal, subtractInflationNominal, dummySpread, dummyDC, schOneDate,
                                     paymentConvention, fixingDays, dummyFloatIndex,
                                     dummyFixedRate, baseCPI, dummyDC, schOneDate, paymentConvention, observationLag,
                                     common.ii, InterpolationType.AsIndex, inflationNominal);

            cS.setPricingEngine(dse);
            QAssert.IsTrue(System.Math.Abs(cS.NPV()) < 1e-3, "CPISwap as ZCIIS does not reprice to zero");

            for (var i = 0; i < 2; i++)
            {
                var cs = cS.legNPV(i).GetValueOrDefault();
                var z = zciis.legNPV(i).GetValueOrDefault();
                QAssert.IsTrue(System.Math.Abs(cs - z) < 1e-3, "zciis leg does not equal CPISwap leg");
            }
            // remove circular refernce
            common.hcpi.linkTo(null);
        }

        [Fact]
        public void cpibondconsistency()
        {
            var common = new CommonVars();

            // ZeroInflationSwap aka CPISwap

            var type = CPISwap.Type.Payer;
            var nominal = 1000000.0;
            var subtractInflationNominal = true;
            // float+spread leg
            var spread = 0.0;
            DayCounter floatDayCount = new Actual365Fixed();
            var floatPaymentConvention = BusinessDayConvention.ModifiedFollowing;
            var fixingDays = 0;
            IborIndex floatIndex = new GBPLibor(new Period(6, TimeUnit.Months), common.nominalUK);

            // fixed x inflation leg
            var fixedRate = 0.1;//1% would be 0.01
            var baseCPI = 206.1; // would be 206.13871 if we were interpolating
            DayCounter fixedDayCount = new Actual365Fixed();
            var fixedPaymentConvention = BusinessDayConvention.ModifiedFollowing;
            Calendar fixedPaymentCalendar = new UnitedKingdom();
            ZeroInflationIndex fixedIndex = common.ii;
            var contractObservationLag = common.contractObservationLag;
            var observationInterpolation = common.contractObservationInterpolation;

            // set the schedules
            var startDate = new Date(2, Month.October, 2007);
            var endDate = new Date(2, Month.October, 2052);
            var floatSchedule = new MakeSchedule().from(startDate).to(endDate)
            .withTenor(new Period(6, TimeUnit.Months))
            .withCalendar(new UnitedKingdom())
            .withConvention(floatPaymentConvention)
            .backwards().value();
            var fixedSchedule = new MakeSchedule().from(startDate).to(endDate)
            .withTenor(new Period(6, TimeUnit.Months))
            .withCalendar(new UnitedKingdom())
            .withConvention(BusinessDayConvention.Unadjusted)
            .backwards().value();

            var zisV = new CPISwap(type, nominal, subtractInflationNominal,
                                       spread, floatDayCount, floatSchedule,
                                       floatPaymentConvention, fixingDays, floatIndex,
                                       fixedRate, baseCPI, fixedDayCount, fixedSchedule,
                                       fixedPaymentConvention, contractObservationLag,
                                       fixedIndex, observationInterpolation);

            double[] floatFix = { 0.06255, 0.05975, 0.0637, 0.018425, 0.0073438, -1, -1 };
            double[] cpiFix = { 211.4, 217.2, 211.4, 213.4, -2, -2 };
            for (var i = 0; i < floatSchedule.Count; i++)
            {
                if (floatSchedule[i] < common.evaluationDate)
                {
                    floatIndex.addFixing(floatSchedule[i], floatFix[i], true); //true=overwrite
                }

                var zic = zisV.cpiLeg()[i] as CPICoupon;
                if (zic != null)
                {
                    if (zic.fixingDate() < common.evaluationDate - new Period(1, TimeUnit.Months))
                    {
                        fixedIndex.addFixing(zic.fixingDate(), cpiFix[i], true);
                    }
                }
            }

            // simple structure so simple pricing engine - most work done by index
            var dse = new DiscountingSwapEngine(common.nominalUK);

            zisV.setPricingEngine(dse);

            // now do the bond equivalent
            List<double> fixedRates = new InitializedList<double>(1, fixedRate);
            var settlementDays = 1;// cannot be zero!
            var growthOnly = true;
            var cpiB = new CPIBond(settlementDays, nominal, growthOnly,
                                       baseCPI, contractObservationLag, fixedIndex,
                                       observationInterpolation, fixedSchedule,
                                       fixedRates, fixedDayCount, fixedPaymentConvention);

            var dbe = new DiscountingBondEngine(common.nominalUK);
            cpiB.setPricingEngine(dbe);

            QAssert.IsTrue(System.Math.Abs(cpiB.NPV() - zisV.legNPV(0).GetValueOrDefault()) < 1e-5,
                           "cpi bond does not equal equivalent cpi swap leg");
            // remove circular refernce
            common.hcpi.linkTo(null);
        }
    }
}
