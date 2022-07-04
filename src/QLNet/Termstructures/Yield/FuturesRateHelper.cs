/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

using QLNet.Indexes;
using QLNet.Instruments;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    //! Rate helper for bootstrapping over interest-rate futures prices
    [JetBrains.Annotations.PublicAPI] public class FuturesRateHelper : RateHelper
    {

        public FuturesRateHelper(Handle<Quote> price,
                                 Date iborStartDate,
                                 int lengthInMonths,
                                 Calendar calendar,
                                 BusinessDayConvention convention,
                                 bool endOfMonth,
                                 DayCounter dayCounter,
                                 Handle<Quote> convAdj = null,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = convAdj ?? new Handle<Quote>();

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            maturityDate_ = calendar.advance(iborStartDate, new Period(lengthInMonths, TimeUnit.Months), convention, endOfMonth);
            yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;

            convAdj_.registerWith(update);
        }


        public FuturesRateHelper(double price,
                                 Date iborStartDate,
                                 int lengthInMonths,
                                 Calendar calendar,
                                 BusinessDayConvention convention,
                                 bool endOfMonth,
                                 DayCounter dayCounter,
                                 double convexityAdjustment = 0.0,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = new Handle<Quote>(new SimpleQuote(convexityAdjustment));

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            maturityDate_ = calendar.advance(iborStartDate, new Period(lengthInMonths, TimeUnit.Months), convention, endOfMonth);
            yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
        }

        public FuturesRateHelper(Handle<Quote> price,
                                 Date iborStartDate,
                                 Date iborEndDate,
                                 DayCounter dayCounter,
                                 Handle<Quote> convAdj = null,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = convAdj ?? new Handle<Quote>();

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    if (iborEndDate == null)
                    {
                        // advance 3 months
                        maturityDate_ = IMM.nextDate(iborStartDate, false);
                        maturityDate_ = IMM.nextDate(maturityDate_, false);
                        maturityDate_ = IMM.nextDate(maturityDate_, false);
                    }
                    else
                    {
                        Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                         "end date (" + iborEndDate +
                                         ") must be greater than start date (" +
                                         iborStartDate + ")");
                        maturityDate_ = iborEndDate;
                    }
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    if (iborEndDate == null)
                    {
                        // advance 3 months
                        maturityDate_ = ASX.nextDate(iborStartDate, false);
                        maturityDate_ = ASX.nextDate(maturityDate_, false);
                        maturityDate_ = ASX.nextDate(maturityDate_, false);
                    }
                    else
                    {
                        Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                         "end date (" + iborEndDate +
                                         ") must be greater than start date (" +
                                         iborStartDate + ")");
                        maturityDate_ = iborEndDate;
                    }
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;

            convAdj_.registerWith(update);
        }

        public FuturesRateHelper(double price,
                                 Date iborStartDate,
                                 Date iborEndDate,
                                 DayCounter dayCounter,
                                 double convAdj = 0,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    if (iborEndDate == null)
                    {
                        // advance 3 months
                        maturityDate_ = IMM.nextDate(iborStartDate, false);
                        maturityDate_ = IMM.nextDate(maturityDate_, false);
                        maturityDate_ = IMM.nextDate(maturityDate_, false);
                    }
                    else
                    {
                        Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                         "end date (" + iborEndDate +
                                         ") must be greater than start date (" +
                                         iborStartDate + ")");
                        maturityDate_ = iborEndDate;
                    }
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    if (iborEndDate == null)
                    {
                        // advance 3 months
                        maturityDate_ = ASX.nextDate(iborStartDate, false);
                        maturityDate_ = ASX.nextDate(maturityDate_, false);
                        maturityDate_ = ASX.nextDate(maturityDate_, false);
                    }
                    else
                    {
                        Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                         "end date (" + iborEndDate +
                                         ") must be greater than start date (" +
                                         iborStartDate + ")");
                        maturityDate_ = iborEndDate;
                    }
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
        }

        public FuturesRateHelper(Handle<Quote> price,
                                 Date iborStartDate,
                                 IborIndex i,
                                 Handle<Quote> convAdj = null,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = convAdj ?? new Handle<Quote>();

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            var cal = i.fixingCalendar();
            maturityDate_ = cal.advance(iborStartDate, i.tenor(), i.businessDayConvention());
            yearFraction_ = i.dayCounter().yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
            convAdj_.registerWith(update);
        }

        public FuturesRateHelper(double price,
                                 Date iborStartDate,
                                 IborIndex i,
                                 double convAdj = 0.0,
                                 Futures.Type type = Futures.Type.IMM)
           : base(price)
        {
            convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

            switch (type)
            {
                case Futures.Type.IMM:
                    Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid IMM date");
                    break;
                case Futures.Type.ASX:
                    Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                     iborStartDate + " is not a valid ASX date");
                    break;
                default:
                    Utils.QL_FAIL("unknown futures ExerciseType (" + type + ")");
                    break;
            }
            earliestDate_ = iborStartDate;
            var cal = i.fixingCalendar();
            maturityDate_ = cal.advance(iborStartDate, i.tenor(), i.businessDayConvention());
            yearFraction_ = i.dayCounter().yearFraction(earliestDate_, maturityDate_);
            pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
        }

        //! RateHelper interface
        public override double impliedQuote()
        {
            Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");

            var forwardRate = (termStructure_.discount(earliestDate_) /
                                  termStructure_.discount(maturityDate_) - 1) / yearFraction_;
            var convAdj = convAdj_.empty() ? 0 : convAdj_.link.value();
            // Convexity, as FRA/futures adjustment, has been used in the
            // past to take into account futures margining vs FRA.
            // Therefore, there's no requirement for it to be non-negative.
            var futureRate = forwardRate + convAdj;
            return 100.0 * (1.0 - futureRate);
        }

        //! FuturesRateHelper inspectors
        public double convexityAdjustment() => convAdj_.empty() ? 0.0 : convAdj_.link.value();

        private double yearFraction_;
        private Handle<Quote> convAdj_;

    }

    // Rate helper with date schedule relative to the global evaluation date
    // This class takes care of rebuilding the date schedule when the global evaluation date changes

    // Rate helper for bootstrapping over deposit rates

    //! Rate helper for bootstrapping over %FRA rates

    // Rate helper for bootstrapping over swap rates

    //! Rate helper for bootstrapping over BMA swap rates

    //! Rate helper for bootstrapping over Fx Swap rates
    /*! fwdFx = spotFx + fwdPoint
       isFxBaseCurrencyCollateralCurrency indicates if the base currency
       of the fx currency pair is the one used as collateral
    */
}
