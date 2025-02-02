﻿/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class AmortizingFixedRateBond : Bond
    {
        protected DayCounter dayCounter_;
        protected Frequency frequency_;
        protected Schedule schedule_;

        public AmortizingFixedRateBond(
            int settlementDays,
            List<double> notionals,
            Schedule schedule,
            List<double> coupons,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            Date issueDate = null)
            : base(settlementDays, schedule.calendar(), issueDate)
        {
            frequency_ = schedule.tenor().frequency();
            dayCounter_ = accrualDayCounter;
            schedule_ = schedule;

            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule)
                .withCouponRates(coupons, accrualDayCounter)
                .withNotionals(notionals)
                .withPaymentAdjustment(paymentConvention).value();

            addRedemptionsToCashflows();

            QLNet.Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
        }

        public AmortizingFixedRateBond(
            int settlementDays,
            List<double> notionals,
            Schedule schedule,
            List<InterestRate> coupons,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            Date issueDate = null)
            : base(settlementDays, schedule.calendar(), issueDate)
        {
            frequency_ = schedule.tenor().frequency();
            dayCounter_ = accrualDayCounter;
            schedule_ = schedule;

            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule)
                .withCouponRates(coupons)
                .withNotionals(notionals)
                .withPaymentAdjustment(paymentConvention).value();

            addRedemptionsToCashflows();

            QLNet.Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
        }

        public AmortizingFixedRateBond(
            int settlementDays,
            Calendar calendar,
            double faceAmount,
            Date startDate,
            Period bondTenor,
            Frequency sinkingFrequency,
            double coupon,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            Date issueDate = null)
            : base(settlementDays, calendar, issueDate)
        {
            frequency_ = sinkingFrequency;
            dayCounter_ = accrualDayCounter;

            QLNet.Utils.QL_REQUIRE(bondTenor.length() > 0, () =>
                "bond tenor must be positive. "
                + bondTenor + " is not allowed.");

            maturityDate_ = startDate + bondTenor;
            schedule_ = sinkingSchedule(startDate, bondTenor, sinkingFrequency, calendar);
            cashflows_ = new FixedRateLeg(schedule_)
                .withCouponRates(coupon, accrualDayCounter)
                .withNotionals(sinkingNotionals(bondTenor, sinkingFrequency, coupon, faceAmount))
                .withPaymentAdjustment(paymentConvention).value();

            addRedemptionsToCashflows();
        }

        public DayCounter dayCounter() => dayCounter_;

        public Frequency frequency() => frequency_;

        protected bool isSubPeriod(Period subPeriod, Period superPeriod, out int numSubPeriods)
        {
            numSubPeriods = 0;

            var superDays = daysMinMax(superPeriod);
            var subDays = daysMinMax(subPeriod);

            //obtain the approximate time ratio
            var minPeriodRatio =
                superDays.Key / (double)subDays.Value;
            var maxPeriodRatio =
                superDays.Value / (double)subDays.Key;
            var lowRatio = (int)System.Math.Floor(minPeriodRatio);
            var highRatio = (int)System.Math.Ceiling(maxPeriodRatio);

            try
            {
                for (var i = lowRatio; i <= highRatio; ++i)
                {
                    var testPeriod = subPeriod * i;
                    if (testPeriod == superPeriod)
                    {
                        numSubPeriods = i;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return false;
        }

        protected List<double> sinkingNotionals(Period maturityTenor,
            Frequency sinkingFrequency,
            double couponRate,
            double initialNotional)
        {
            var freqPeriod = new Period(sinkingFrequency);
            var nPeriods = 0;
            QLNet.Utils.QL_REQUIRE(isSubPeriod(freqPeriod, maturityTenor, out nPeriods), () =>
                "Bond frequency is incompatible with the maturity tenor");

            List<double> notionals = new InitializedList<double>(nPeriods + 1);
            notionals[0] = initialNotional;
            var coupon = couponRate / (double)sinkingFrequency;
            var compoundedInterest = 1.0;
            var totalValue = System.Math.Pow(1.0 + coupon, nPeriods);
            for (var i = 0; i < nPeriods - 1; ++i)
            {
                compoundedInterest *= 1.0 + coupon;
                var currentNotional = 0.0;
                if (coupon < 1.0e-12)
                {
                    currentNotional =
                        initialNotional * (1.0 - (i + 1.0) / nPeriods);
                }
                else
                {
                    currentNotional =
                        initialNotional * (compoundedInterest - (compoundedInterest - 1.0) / (1.0 - 1.0 / totalValue));
                }

                notionals[i + 1] = currentNotional;
            }

            notionals[notionals.Count - 1] = 0.0;
            return notionals;
        }

        protected Schedule sinkingSchedule(Date startDate,
            Period maturityTenor,
            Frequency sinkingFrequency,
            Calendar paymentCalendar)
        {
            var freqPeriod = new Period(sinkingFrequency);
            var maturityDate = new Date(startDate + maturityTenor);
            var retVal = new Schedule(startDate, maturityDate, freqPeriod,
                paymentCalendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward, false);
            return retVal;
        }

        private KeyValuePair<int, int> daysMinMax(Period p)
        {
            switch (p.units())
            {
                case TimeUnit.Days:
                    return new KeyValuePair<int, int>(p.length(), p.length());
                case TimeUnit.Weeks:
                    return new KeyValuePair<int, int>(7 * p.length(), 7 * p.length());
                case TimeUnit.Months:
                    return new KeyValuePair<int, int>(28 * p.length(), 31 * p.length());
                case TimeUnit.Years:
                    return new KeyValuePair<int, int>(365 * p.length(), 366 * p.length());
                default:
                    QLNet.Utils.QL_FAIL("unknown time unit (" + p.units() + ")");
                    return new KeyValuePair<int, int>();
            }
        }
    }
}
