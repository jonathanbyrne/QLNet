﻿//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    //! %Nzocr index
    /*! %Nzocr (New Zealand official cash rate) rate fixed by the RBNZ.

        See <http://www.rbnz.govt.nz/monetary-policy/official-cash-rate-decisions>.
    */
    [PublicAPI]
    public class Nzocr : OvernightIndex
    {
        public Nzocr(Handle<YieldTermStructure> h = null)
            : base("Nzocr", 0, new NZDCurrency(),
                new NewZealand(),
                new Actual365Fixed(), h ?? new Handle<YieldTermStructure>())
        {
        }
    }
}
