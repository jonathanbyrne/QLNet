/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class FixedRateBond : Bond
    {
        protected DayCounter dayCounter_;
        protected Frequency frequency_;
        //! fixed-rate bond
        /*! \ingroup instruments

            \test calculations are tested by checking results against
                  cached values.
        */

        //! simple annual compounding coupon rates
        public FixedRateBond(int settlementDays, double faceAmount, Schedule schedule, List<double> coupons,
            DayCounter accrualDayCounter, BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100, Date issueDate = null, Calendar paymentCalendar = null,
            Period exCouponPeriod = null,
            Calendar exCouponCalendar = null,
            BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
            bool exCouponEndOfMonth = false)
            : base(settlementDays, paymentCalendar ?? schedule.calendar(),
                issueDate)
        {
            frequency_ = schedule.tenor().frequency();
            dayCounter_ = accrualDayCounter;
            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule)
                .withCouponRates(coupons, accrualDayCounter)
                .withExCouponPeriod(exCouponPeriod,
                    exCouponCalendar,
                    exCouponConvention,
                    exCouponEndOfMonth)
                .withPaymentCalendar(calendar_)
                .withNotionals(faceAmount)
                .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double> { redemption });

            QLNet.Utils.QL_REQUIRE(cashflows().Count != 0, () => "bond with no cashflows!");
            QLNet.Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");
        }

        /*! simple annual compounding coupon rates
            with internal schedule calculation */
        public FixedRateBond(int settlementDays,
            Calendar calendar,
            double faceAmount,
            Date startDate,
            Date maturityDate,
            Period tenor,
            List<double> coupons,
            DayCounter accrualDayCounter,
            BusinessDayConvention accrualConvention = BusinessDayConvention.Following,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100,
            Date issueDate = null,
            Date stubDate = null,
            DateGeneration.Rule rule = DateGeneration.Rule.Backward,
            bool endOfMonth = false,
            Calendar paymentCalendar = null,
            Period exCouponPeriod = null,
            Calendar exCouponCalendar = null,
            BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
            bool exCouponEndOfMonth = false)
            : base(settlementDays, paymentCalendar ?? calendar,
                issueDate)
        {
            frequency_ = tenor.frequency();
            dayCounter_ = accrualDayCounter;
            maturityDate_ = maturityDate;

            Date firstDate = null, nextToLastDate = null;

            switch (rule)
            {
                case DateGeneration.Rule.Backward:
                    firstDate = null;
                    nextToLastDate = stubDate;
                    break;

                case DateGeneration.Rule.Forward:
                    firstDate = stubDate;
                    nextToLastDate = null;
                    break;

                case DateGeneration.Rule.Zero:
                case DateGeneration.Rule.ThirdWednesday:
                case DateGeneration.Rule.Twentieth:
                case DateGeneration.Rule.TwentiethIMM:
                    QLNet.Utils.QL_FAIL("stub date (" + stubDate + ") not allowed with " + rule + " DateGeneration::Rule");
                    break;

                default:
                    QLNet.Utils.QL_FAIL("unknown DateGeneration::Rule (" + rule + ")");
                    break;
            }

            var schedule = new Schedule(startDate, maturityDate_, tenor,
                calendar, accrualConvention, accrualConvention,
                rule, endOfMonth,
                firstDate, nextToLastDate);

            cashflows_ = new FixedRateLeg(schedule)
                .withCouponRates(coupons, accrualDayCounter)
                .withExCouponPeriod(exCouponPeriod,
                    exCouponCalendar,
                    exCouponConvention,
                    exCouponEndOfMonth)
                .withPaymentCalendar(calendar_)
                .withNotionals(faceAmount)
                .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double> { redemption });

            QLNet.Utils.QL_REQUIRE(cashflows().Count != 0, () => "bond with no cashflows!");
            QLNet.Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");
        }

        public FixedRateBond(int settlementDays,
            double faceAmount,
            Schedule schedule,
            List<InterestRate> coupons,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100,
            Date issueDate = null,
            Calendar paymentCalendar = null,
            Period exCouponPeriod = null,
            Calendar exCouponCalendar = null,
            BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
            bool exCouponEndOfMonth = false)
            : base(settlementDays, paymentCalendar ?? schedule.calendar(),
                issueDate)
        {
            frequency_ = schedule.tenor().frequency();
            dayCounter_ = coupons[0].dayCounter();
            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule)
                .withCouponRates(coupons)
                .withExCouponPeriod(exCouponPeriod,
                    exCouponCalendar,
                    exCouponConvention,
                    exCouponEndOfMonth)
                .withPaymentCalendar(calendar_)
                .withNotionals(faceAmount)
                .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double> { redemption });

            QLNet.Utils.QL_REQUIRE(cashflows().Count != 0, () => "bond with no cashflows!");
            QLNet.Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");
        }

        public DayCounter dayCounter() => dayCounter_;

        public Frequency frequency() => frequency_;
    }
}
