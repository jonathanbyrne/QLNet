using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYZACPIr : YoYInflationIndex
    {
        public YYZACPIr(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYZACPIr(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YYR_CPI",
                new ZARegion(),
                false,
                interpolated,
                true,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new ZARCurrency(),
                ts)
        {
        }
    }
}
