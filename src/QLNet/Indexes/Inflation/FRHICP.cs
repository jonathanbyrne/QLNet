﻿/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet.Indexes.Inflation
{
    //! FR HICP index
    [PublicAPI]
    public class FRHICP : ZeroInflationIndex
    {
        public FRHICP(bool interpolated)
            : this(interpolated, new Handle<ZeroInflationTermStructure>())
        {
        }

        public FRHICP(bool interpolated,
            Handle<ZeroInflationTermStructure> ts)
            : base("HICP",
                new FranceRegion(),
                false,
                interpolated,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new EURCurrency(),
                ts)
        {
        }
    }

    //! Genuine year-on-year FR HICP (i.e. not a ratio)

    //! Fake year-on-year FR HICP (i.e. a ratio)
}
