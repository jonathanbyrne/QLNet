using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYUKRPIr : YoYInflationIndex
    {
        public YYUKRPIr(bool interpolated)
            : this(interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYUKRPIr(bool interpolated, Handle<YoYInflationTermStructure> ts)
            : base("YYR_RPI", new UKRegion(), false, interpolated, true, Frequency.Monthly,
                new Period(1, TimeUnit.Months), new GBPCurrency(), ts)
        {
        }
    }
}
