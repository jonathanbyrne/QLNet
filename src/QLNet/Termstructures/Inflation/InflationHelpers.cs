﻿/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)

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

using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Time;
using System;
using System.Collections.Generic;
using QLNet.Pricingengines.Swap;

namespace QLNet.Termstructures.Inflation
{
    //! Zero-coupon inflation-swap bootstrap helper
    [JetBrains.Annotations.PublicAPI] public class ZeroCouponInflationSwapHelper : BootstrapHelper<ZeroInflationTermStructure>
    {
        public ZeroCouponInflationSwapHelper(
           Handle<Quote> quote,
           Period swapObsLag,   // lag on swap observation of index
           Date maturity,
           Calendar calendar,   // index may have null calendar as valid on every day
           BusinessDayConvention paymentConvention,
           DayCounter dayCounter,
           ZeroInflationIndex zii)
           : base(quote)
        {
            swapObsLag_ = swapObsLag;
            maturity_ = maturity;
            calendar_ = calendar;
            paymentConvention_ = paymentConvention;
            dayCounter_ = dayCounter;
            zii_ = zii;

            if (zii_.interpolated())
            {
                // if interpolated then simple
                earliestDate_ = maturity_ - swapObsLag_;
                latestDate_ = maturity_ - swapObsLag_;
            }
            else
            {
                // but if NOT interpolated then the value is valid
                // for every day in an inflation period so you actually
                // get an extended validity, however for curve building
                // just put the first date because using that convention
                // for the base date throughout
                var limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                                                                          zii_.frequency());
                earliestDate_ = limStart.Key;
                latestDate_ = limStart.Key;
            }

            // check that the observation lag of the swap
            // is compatible with the availability lag of the index AND
            // it's interpolation (assuming the start day is spot)
            if (zii_.interpolated())
            {
                var pShift = new Period(zii_.frequency());
                Utils.QL_REQUIRE(swapObsLag_ - pShift > zii_.availabilityLag(), () =>
                                 "inconsistency between swap observation of index "
                                 + swapObsLag_ +
                                 " index availability " + zii_.availabilityLag() +
                                 " index period " + pShift +
                                 " and index availability " + zii_.availabilityLag() +
                                 " need (obsLag-index period) > availLag");

            }
            Settings.registerWith(update);
        }


        public override void setTermStructure(ZeroInflationTermStructure z)
        {

            base.setTermStructure(z);

            // set up a new ZCIIS
            // but this one does NOT own its inflation term structure
            var own = false;
            var K = quote().link.value();

            // The effect of the new inflation term structure is
            // felt via the effect on the inflation index
            var zits = new Handle<ZeroInflationTermStructure>(z, own);

            var new_zii = zii_.clone(zits);

            var nominal = 1000000.0;   // has to be something but doesn't matter what
            var start = z.nominalTermStructure().link.referenceDate();
            zciis_ = new ZeroCouponInflationSwap(
               ZeroCouponInflationSwap.Type.Payer,
               nominal, start, maturity_,
               calendar_, paymentConvention_, dayCounter_, K, // fixed side & fixed rate
               new_zii, swapObsLag_);
            // Because very simple instrument only takes
            // standard discounting swap engine.
            zciis_.setPricingEngine(new DiscountingSwapEngine(z.nominalTermStructure()));
        }

        public override double impliedQuote()
        {
            // what does the term structure imply?
            // in this case just the same value ... trivial case
            // (would not be so for an inflation-linked bond)
            zciis_.recalculate();
            return zciis_.fairRate();
        }


        protected Period swapObsLag_;
        protected Date maturity_;
        protected Calendar calendar_;
        protected BusinessDayConvention paymentConvention_;
        protected DayCounter dayCounter_;
        protected ZeroInflationIndex zii_;
        protected ZeroCouponInflationSwap zciis_;
    }

    //! Year-on-year inflation-swap bootstrap helper
    [JetBrains.Annotations.PublicAPI] public class YearOnYearInflationSwapHelper : BootstrapHelper<YoYInflationTermStructure>
    {
        public YearOnYearInflationSwapHelper(Handle<Quote> quote,
                                             Period swapObsLag,
                                             Date maturity,
                                             Calendar calendar,
                                             BusinessDayConvention paymentConvention,
                                             DayCounter dayCounter,
                                             YoYInflationIndex yii)
           : base(quote)
        {
            swapObsLag_ = swapObsLag;
            maturity_ = maturity;
            calendar_ = calendar;
            paymentConvention_ = paymentConvention;
            dayCounter_ = dayCounter;
            yii_ = yii;

            if (yii_.interpolated())
            {
                // if interpolated then simple
                earliestDate_ = maturity_ - swapObsLag_;
                latestDate_ = maturity_ - swapObsLag_;
            }
            else
            {
                // but if NOT interpolated then the value is valid
                // for every day in an inflation period so you actually
                // get an extended validity, however for curve building
                // just put the first date because using that convention
                // for the base date throughout
                var limStart = Utils.inflationPeriod(maturity_ - swapObsLag_,
                                                                          yii_.frequency());
                earliestDate_ = limStart.Key;
                latestDate_ = limStart.Key;
            }

            // check that the observation lag of the swap
            // is compatible with the availability lag of the index AND
            // it's interpolation (assuming the start day is spot)
            if (yii_.interpolated())
            {
                var pShift = new Period(yii_.frequency());
                Utils.QL_REQUIRE(swapObsLag_ - pShift > yii_.availabilityLag(), () =>
                                 "inconsistency between swap observation of index "
                                 + swapObsLag_ +
                                 " index availability " + yii_.availabilityLag() +
                                 " index period " + pShift +
                                 " and index availability " + yii_.availabilityLag() +
                                 " need (obsLag-index period) > availLag");
            }

            Settings.registerWith(update);
        }

        public override void setTermStructure(YoYInflationTermStructure y)
        {
            base.setTermStructure(y);

            // set up a new YYIIS
            // but this one does NOT own its inflation term structure
            const bool own = false;

            // The effect of the new inflation term structure is
            // felt via the effect on the inflation index
            var yyts = new Handle<YoYInflationTermStructure>(y, own);

            var new_yii = yii_.clone(yyts);

            // always works because tenor is always 1 year so
            // no problem with different days-in-month
            var from = Settings.evaluationDate();
            var to = maturity_;
            var fixedSchedule = new MakeSchedule().from(from).to(to)
            .withTenor(new Period(1, TimeUnit.Years))
            .withConvention(BusinessDayConvention.Unadjusted)
            .withCalendar(calendar_)// fixed leg gets cal from sched
            .value();
            var yoySchedule = fixedSchedule;
            var spread = 0.0;
            var fixedRate = quote().link.value();

            var nominal = 1000000.0;   // has to be something but doesn't matter what
            yyiis_ = new YearOnYearInflationSwap(YearOnYearInflationSwap.Type.Payer,
                                                 nominal,
                                                 fixedSchedule,
                                                 fixedRate,
                                                 dayCounter_,
                                                 yoySchedule,
                                                 new_yii,
                                                 swapObsLag_,
                                                 spread,
                                                 dayCounter_,
                                                 calendar_,  // inflation index does not have a calendar
                                                 paymentConvention_);


            // Because very simple instrument only takes
            // standard discounting swap engine.
            yyiis_.setPricingEngine(new DiscountingSwapEngine(y.nominalTermStructure()));
        }

        public override double impliedQuote()
        {
            // what does the term structure imply?
            // in this case just the same value ... trivial case
            // (would not be so for an inflation-linked bond)
            yyiis_.recalculate();
            return yyiis_.fairRate();
        }

        protected Period swapObsLag_;
        protected Date maturity_;
        protected Calendar calendar_;
        protected BusinessDayConvention paymentConvention_;
        protected DayCounter dayCounter_;
        protected YoYInflationIndex yii_;
        protected YearOnYearInflationSwap yyiis_;
    }

}
