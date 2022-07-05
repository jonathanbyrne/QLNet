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

using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    //! %Bkbm index
    /*! Bkbm rate fixed by NZFMA.

        See <http://www.nzfma.org/Site/data/default.aspx>.
    */
    [PublicAPI]
    public class Bkbm : IborIndex
    {
        public Bkbm(Period tenor, Handle<YieldTermStructure> h = null)
            : base("Bkbm", tenor,
                0, // settlement days
                new NZDCurrency(), new NewZealand(),
                BusinessDayConvention.ModifiedFollowing, true,
                new Actual365Fixed(), h ?? new Handle<YieldTermStructure>())
        {
            QLNet.Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days, () =>
                "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
        }
    }

    //! 1-month %Bkbm index

    //! 2-month %Bkbm index

    //! 3-month %Bkbm index

    //! 4-month %Bkbm index

    //! 5-month %Bkbm index

    //! 6-month %Bkbm index
}
