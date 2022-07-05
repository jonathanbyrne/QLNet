/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    //! South African CPI index
    [PublicAPI]
    public class ZACPI : ZeroInflationIndex
    {
        public ZACPI(bool interpolated)
            : this(interpolated, new Handle<ZeroInflationTermStructure>())
        {
        }

        public ZACPI(bool interpolated,
            Handle<ZeroInflationTermStructure> ts)
            : base("CPI",
                new ZARegion(),
                false,
                interpolated,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months), // availability
                new ZARCurrency(),
                ts)
        {
        }
    }

    //! Genuine year-on-year South African CPI (i.e. not a ratio of South African CPI)

    //! Fake year-on-year South African CPI (i.e. a ratio of South African CPI)
}
