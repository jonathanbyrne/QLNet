using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YYFRHICP : YoYInflationIndex
    {
        public YYFRHICP(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>()) { }

        public YYFRHICP(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YY_HICP",
                new FranceRegion(),
                false,
                interpolated,
                false,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new EURCurrency(),
                ts)
        { }
    }
}