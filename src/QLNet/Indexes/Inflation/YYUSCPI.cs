using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YYUSCPI : YoYInflationIndex
    {
        public YYUSCPI(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>()) { }

        public YYUSCPI(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YY_CPI",
                new USRegion(),
                false,
                interpolated,
                false,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new USDCurrency(),
                ts)
        { }
    }
}