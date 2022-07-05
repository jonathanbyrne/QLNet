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

using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class RangeAccrualFloatersCoupon : FloatingRateCoupon
    {
        private double endTime_;
        private double lowerTrigger_;
        private List<Date> observationDates_;
        private int observationsNo_;
        private Schedule observationsSchedule_;
        private List<double> observationTimes_;
        private double startTime_;
        private double upperTrigger_;

        public RangeAccrualFloatersCoupon(Date paymentDate,
            double nominal,
            IborIndex index,
            Date startDate,
            Date endDate,
            int fixingDays,
            DayCounter dayCounter,
            double gearing,
            double spread,
            Date refPeriodStart,
            Date refPeriodEnd,
            Schedule observationsSchedule,
            double lowerTrigger,
            double upperTrigger)
            : base(paymentDate, nominal, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd,
                dayCounter)
        {
            observationsSchedule_ = observationsSchedule;
            lowerTrigger_ = lowerTrigger;
            upperTrigger_ = upperTrigger;

            QLNet.Utils.QL_REQUIRE(lowerTrigger_ < upperTrigger, () => "lowerTrigger_>=upperTrigger");
            QLNet.Utils.QL_REQUIRE(observationsSchedule_.startDate() == startDate, () => "incompatible start date");
            QLNet.Utils.QL_REQUIRE(observationsSchedule_.endDate() == endDate, () => "incompatible end date");

            observationDates_ = new List<Date>(observationsSchedule_.dates());
            observationDates_.RemoveAt(observationDates_.Count - 1); //remove end date
            observationDates_.RemoveAt(0); //remove start date
            observationsNo_ = observationDates_.Count;

            var rateCurve = index.forwardingTermStructure();
            var referenceDate = rateCurve.link.referenceDate();

            startTime_ = dayCounter.yearFraction(referenceDate, startDate);
            endTime_ = dayCounter.yearFraction(referenceDate, endDate);
            observationTimes_ = new List<double>();
            for (var i = 0; i < observationsNo_; i++)
            {
                observationTimes_.Add(dayCounter.yearFraction(referenceDate, observationDates_[i]));
            }
        }

        public double endTime() => endTime_;

        public double lowerTrigger() => lowerTrigger_;

        public List<Date> observationDates() => observationDates_;

        public int observationsNo() => observationsNo_;

        public Schedule observationsSchedule() => observationsSchedule_;

        public List<double> observationTimes() => observationTimes_;

        public double priceWithoutOptionality(Handle<YieldTermStructure> discountCurve) =>
            accrualPeriod() * (gearing_ * indexFixing() + spread_) *
            nominal() * discountCurve.link.discount(date());

        public double startTime() => startTime_;

        public double upperTrigger() => upperTrigger_;
    }

    //! helper class building a sequence of range-accrual floating-rate coupons
}
