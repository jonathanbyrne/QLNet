using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [JetBrains.Annotations.PublicAPI] public class DailyTenorCHFLibor : DailyTenorLibor
    {
        public DailyTenorCHFLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("CHFLibor", settlementDays, new CHFCurrency(), new Switzerland(), new Actual360(), h)
        { }
    }
}