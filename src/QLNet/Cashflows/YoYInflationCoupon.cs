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

using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    //! %Coupon paying a YoY-inflation ExerciseType index
    [PublicAPI]
    public class YoYInflationCoupon : InflationCoupon
    {
        protected double gearing_;
        protected double spread_;
        private YoYInflationIndex yoyIndex_;

        public YoYInflationCoupon(Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            YoYInflationIndex yoyIndex,
            Period observationLag,
            DayCounter dayCounter,
            double gearing = 1.0,
            double spread = 0.0,
            Date refPeriodStart = null,
            Date refPeriodEnd = null)
            : base(paymentDate, nominal, startDate, endDate,
                fixingDays, yoyIndex, observationLag,
                dayCounter, refPeriodStart, refPeriodEnd)
        {
            yoyIndex_ = yoyIndex;
            gearing_ = gearing;
            spread_ = spread;
        }

        public double adjustedFixing() => (rate() - spread()) / gearing();

        // Inspectors
        // index gearing, i.e. multiplicative coefficient for the index
        public double gearing() => gearing_;

        //! spread paid over the fixing of the underlying index
        public double spread() => spread_;

        public YoYInflationIndex yoyIndex() => yoyIndex_;

        protected override bool checkPricerImpl(InflationCouponPricer i) => i is YoYInflationCouponPricer;
    }

    //! Helper class building a sequence of capped/floored yoy inflation coupons
    //! payoff is: spread + gearing x index
}
