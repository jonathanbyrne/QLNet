using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YYEUHICPr : YoYInflationIndex
    {
        public YYEUHICPr(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>()) { }

        public YYEUHICPr(bool interpolated, Handle<YoYInflationTermStructure> ts)
            : base("YYR_HICP", new EURegion(), false, interpolated, true, Frequency.Monthly,
                new Period(1, TimeUnit.Months), new EURCurrency(), ts)
        { }
    }
}