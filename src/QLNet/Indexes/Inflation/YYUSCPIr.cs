using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYUSCPIr : YoYInflationIndex
    {
        public YYUSCPIr(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYUSCPIr(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YYR_CPI",
                new USRegion(),
                false,
                interpolated,
                true,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new USDCurrency(),
                ts)
        {
        }
    }
}
