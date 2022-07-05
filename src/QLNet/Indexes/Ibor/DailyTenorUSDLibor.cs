using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class DailyTenorUSDLibor : DailyTenorLibor
    {
        public DailyTenorUSDLibor(int settlementDays) : this(settlementDays, new Handle<YieldTermStructure>())
        {
        }

        public DailyTenorUSDLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("USDLibor", settlementDays, new USDCurrency(), new UnitedStates(UnitedStates.Market.Settlement), new Actual360(), h)
        {
        }
    }
}
