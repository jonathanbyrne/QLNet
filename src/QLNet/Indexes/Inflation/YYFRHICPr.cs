﻿using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYFRHICPr : YoYInflationIndex
    {
        public YYFRHICPr(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYFRHICPr(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YYR_HICP",
                new FranceRegion(),
                false,
                interpolated,
                true,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new EURCurrency(),
                ts)
        {
        }
    }
}
