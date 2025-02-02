﻿using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYUKRPI : YoYInflationIndex
    {
        public YYUKRPI(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYUKRPI(bool interpolated, Handle<YoYInflationTermStructure> ts)
            : base("YY_RPI", new UKRegion(), false, interpolated, false, Frequency.Monthly,
                new Period(1, TimeUnit.Months), new GBPCurrency(), ts)
        {
        }
    }
}
