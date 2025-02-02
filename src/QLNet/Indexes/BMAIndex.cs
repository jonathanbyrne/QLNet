﻿/*
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

using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes
{
    //! Bond Market Association index
    /*! The BMA index is the short-term tax-exempt reference index of
        the Bond Market Association.  It has tenor one week, is fixed
        weekly on Wednesdays and is applied with a one-day's fixing
        gap from Thursdays on for one week.  It is the tax-exempt
        correspondent of the 1M USD-Libor.
    */
    [PublicAPI]
    public class BMAIndex : InterestRateIndex
    {
        protected Handle<YieldTermStructure> termStructure_;

        public BMAIndex(Handle<YieldTermStructure> h = null)
            : base("BMA", new Period(1, TimeUnit.Weeks), 1, new USDCurrency(),
                new UnitedStates(UnitedStates.Market.NYSE), new ActualActual(ActualActual.Convention.ISDA))
        {
            termStructure_ = h ?? new Handle<YieldTermStructure>();
            termStructure_.registerWith(update);
        }

        // This method returns a schedule of fixing dates between start and end.
        public Schedule fixingSchedule(Date start, Date end) =>
            new MakeSchedule().from(Utils.previousWednesday(start))
                .to(Utils.nextWednesday(end))
                .withFrequency(Frequency.Weekly)
                .withCalendar(fixingCalendar())
                .withConvention(BusinessDayConvention.Following)
                .forwards()
                .value();

        public override double forecastFixing(Date fixingDate)
        {
            QLNet.Utils.QL_REQUIRE(!termStructure_.empty(), () => "null term structure set to this instance of " + name());
            var start = fixingCalendar().advance(fixingDate, 1, TimeUnit.Days);
            var end = maturityDate(start);
            return termStructure_.link.forwardRate(start, end, dayCounter_, Compounding.Simple).rate();
        }

        // Inspectors
        public Handle<YieldTermStructure> forwardingTermStructure() => termStructure_;

        public override bool isValidFixingDate(Date fixingDate)
        {
            var cal = fixingCalendar();
            // either the fixing date is last Wednesday, or all days
            // between last Wednesday included and the fixing date are
            // holidays
            for (var d = Utils.previousWednesday(fixingDate); d < fixingDate; ++d)
            {
                if (cal.isBusinessDay(d))
                {
                    return false;
                }
            }

            // also, the fixing date itself must be a business day
            return cal.isBusinessDay(fixingDate);
        }

        // Date calculations
        public override Date maturityDate(Date valueDate)
        {
            var cal = fixingCalendar();
            var fixingDate = cal.advance(valueDate, -1, TimeUnit.Days);
            var nextWednesday = Utils.previousWednesday(fixingDate + 7);
            return cal.advance(nextWednesday, 1, TimeUnit.Days);
        }

        // Index interface
        // BMA is fixed weekly on Wednesdays.
        public override string name() => "BMA";
    }
}
