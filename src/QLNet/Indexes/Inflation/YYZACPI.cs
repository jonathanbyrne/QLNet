using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYZACPI : YoYInflationIndex
    {
        public YYZACPI(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYZACPI(bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YY_CPI",
                new ZARegion(),
                false,
                interpolated,
                false,
                Frequency.Monthly,
                new Period(1, TimeUnit.Months),
                new ZARCurrency(),
                ts)
        {
        }
    }
}
