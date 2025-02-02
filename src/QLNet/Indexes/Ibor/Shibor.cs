﻿/*
 Copyright (C) 2008-2015 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Shibor : IborIndex
    {
        public Shibor(Period tenor)
            : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public Shibor(Period tenor, Handle<YieldTermStructure> h)
            : base("Shibor", tenor, tenor == new Period(1, TimeUnit.Days) ? 0 : 1, new CNYCurrency(),
                new China(China.Market.IB), shiborConvention(tenor), false,
                new Actual360(), h)
        {
        }

        private static BusinessDayConvention shiborConvention(Period p)
        {
            switch (p.units())
            {
                case TimeUnit.Days:
                case TimeUnit.Weeks:
                    return BusinessDayConvention.Following;
                case TimeUnit.Months:
                case TimeUnit.Years:
                    return BusinessDayConvention.ModifiedFollowing;
                default:
                    QLNet.Utils.QL_FAIL("invalid time units");
                    return BusinessDayConvention.Unadjusted;
            }
        }
    }
}
