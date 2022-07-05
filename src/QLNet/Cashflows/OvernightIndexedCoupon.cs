/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 *
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
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class OvernightIndexedCoupon : FloatingRateCoupon
    {
        private List<double> dt_;
        private List<double> fixings_;
        private int n_;
        private List<Date> valueDates_, fixingDates_;

        public OvernightIndexedCoupon(
            Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            OvernightIndex overnightIndex,
            double gearing = 1.0,
            double spread = 0.0,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            DayCounter dayCounter = null)
            : base(paymentDate, nominal, startDate, endDate,
                overnightIndex.fixingDays(), overnightIndex,
                gearing, spread,
                refPeriodStart, refPeriodEnd,
                dayCounter)
        {
            // value dates
            var sch = new MakeSchedule()
                .from(startDate)
                .to(endDate)
                .withTenor(new Period(1, TimeUnit.Days))
                .withCalendar(overnightIndex.fixingCalendar())
                .withConvention(overnightIndex.businessDayConvention())
                .backwards()
                .value();

            valueDates_ = sch.dates();
            Utils.QL_REQUIRE(valueDates_.Count >= 2, () => "degenerate schedule");

            // fixing dates
            n_ = valueDates_.Count - 1;
            if (overnightIndex.fixingDays() == 0)
            {
                fixingDates_ = new List<Date>(valueDates_);
            }
            else
            {
                fixingDates_ = new InitializedList<Date>(n_);
                for (var i = 0; i < n_; ++i)
                {
                    fixingDates_[i] = overnightIndex.fixingDate(valueDates_[i]);
                }
            }

            // accrual (compounding) periods
            dt_ = new List<double>(n_);
            var dc = overnightIndex.dayCounter();
            for (var i = 0; i < n_; ++i)
            {
                dt_.Add(dc.yearFraction(valueDates_[i], valueDates_[i + 1]));
            }

            setPricer(new OvernightIndexedCouponPricer());
        }

        //! accrual (compounding) periods
        public List<double> dt() => dt_;

        //! fixing dates for the rates to be compounded
        public List<Date> fixingDates() => fixingDates_;

        public List<double> indexFixings()
        {
            fixings_ = new InitializedList<double>(n_);
            for (var i = 0; i < n_; ++i)
            {
                fixings_[i] = index_.fixing(fixingDates_[i]);
            }

            return fixings_;
        }

        //! value dates for the rates to be compounded
        public List<Date> valueDates() => valueDates_;
    }

    //! helper class building a sequence of overnight coupons
}
