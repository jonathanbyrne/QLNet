using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class DailyTenorJPYLibor : DailyTenorLibor
    {
        public DailyTenorJPYLibor(int settlementDays) : this(settlementDays, new Handle<YieldTermStructure>())
        {
        }

        public DailyTenorJPYLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("JPYLibor", settlementDays, new JPYCurrency(), new Japan(), new Actual360(), h)
        {
        }
    }
}
