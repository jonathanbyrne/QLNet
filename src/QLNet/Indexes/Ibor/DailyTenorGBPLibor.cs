using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [JetBrains.Annotations.PublicAPI] public class DailyTenorGBPLibor : DailyTenorLibor
    {
        public DailyTenorGBPLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("GBPLibor", settlementDays, new GBPCurrency(), new UnitedKingdom(UnitedKingdom.Market.Exchange),
                new Actual365Fixed(), h)
        { }
    }
}