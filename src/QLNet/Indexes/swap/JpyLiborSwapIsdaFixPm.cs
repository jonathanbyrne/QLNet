using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Indexes.Ibor;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.swap
{
    [PublicAPI]
    public class JpyLiborSwapIsdaFixPm : SwapIndex
    {
        public JpyLiborSwapIsdaFixPm(Period tenor)
            : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public JpyLiborSwapIsdaFixPm(Period tenor, Handle<YieldTermStructure> h)
            : base("JpyLiborSwapIsdaFixPm", // familyName
                tenor,
                2, // settlementDays
                new JPYCurrency(),
                new TARGET(),
                new Period(6, TimeUnit.Months), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new ActualActual(ActualActual.Convention.ISDA), // fixedLegDaycounter
                new JPYLibor(new Period(6, TimeUnit.Months), h))
        {
        }
    }
}
