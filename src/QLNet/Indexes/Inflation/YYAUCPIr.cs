using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [PublicAPI]
    public class YYAUCPIr : YoYInflationIndex
    {
        public YYAUCPIr(Frequency frequency,
            bool revised,
            bool interpolated)
            : this(frequency, revised, interpolated, new Handle<YoYInflationTermStructure>())
        {
        }

        public YYAUCPIr(Frequency frequency,
            bool revised,
            bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YYR_CPI",
                new AustraliaRegion(),
                revised,
                interpolated,
                true,
                frequency,
                new Period(2, TimeUnit.Months),
                new AUDCurrency(),
                ts)
        {
        }
    }
}
