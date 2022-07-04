/*
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

using QLNet.Currencies;
using QLNet.Indexes.Ibor;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.swap
{
    [JetBrains.Annotations.PublicAPI] public class UsdLiborSwapIsdaFixAm : SwapIndex
    {
        public UsdLiborSwapIsdaFixAm(Period tenor)
           : this(tenor, new Handle<YieldTermStructure>()) { }

        public UsdLiborSwapIsdaFixAm(Period tenor, Handle<YieldTermStructure> h)
           : base("UsdLiborSwapIsdaFixAm", // familyName
                  tenor,
                  2, // settlementDays
                  new USDCurrency(),
                  new TARGET(),
                  new Period(6, TimeUnit.Months), // fixedLegTenor
                  BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                  new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                  new USDLibor(new Period(3, TimeUnit.Months), h))
        { }
    }
}
