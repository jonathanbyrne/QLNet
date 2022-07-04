using QLNet.Currencies;
using QLNet.Time;

namespace QLNet.Indexes.Inflation
{
    [JetBrains.Annotations.PublicAPI] public class YYAUCPI : YoYInflationIndex
    {
        public YYAUCPI(Frequency frequency,
            bool revised,
            bool interpolated)
            : this(frequency, revised, interpolated, new Handle<YoYInflationTermStructure>()) { }

        public YYAUCPI(Frequency frequency,
            bool revised,
            bool interpolated,
            Handle<YoYInflationTermStructure> ts)
            : base("YY_CPI",
                new AustraliaRegion(),
                revised,
                interpolated,
                false,
                frequency,
                new Period(2, TimeUnit.Months),
                new AUDCurrency(),
                ts)
        { }
    }
}