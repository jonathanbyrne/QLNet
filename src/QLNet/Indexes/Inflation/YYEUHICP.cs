﻿using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YYEUHICP : YoYInflationIndex
    {
        public YYEUHICP(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>()) { }

        public YYEUHICP(bool interpolated, Handle<YoYInflationTermStructure> ts)
            : base("YY_HICP", new EURegion(), false, interpolated, false, Frequency.Monthly,
                new Period(1, TimeUnit.Months), new EURCurrency(), ts)
        { }
    }
}