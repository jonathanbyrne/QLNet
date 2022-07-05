using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class DailyTenorEURLibor : IborIndex
    {
        // http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1412 :
        // no o/n or s/n fixings (as the case may be) will take place
        // when the principal centre of the currency concerned is
        // closed but London is open on the fixing day.
        public DailyTenorEURLibor(int settlementDays)
            : this(settlementDays, new Handle<YieldTermStructure>())
        {
        }

        public DailyTenorEURLibor()
            : base("EURLibor", new Period(1, TimeUnit.Days), 0, new EURCurrency(), new TARGET(),
                Utils.eurliborConvention(new Period(1, TimeUnit.Days)), Utils.eurliborEOM(new Period(1, TimeUnit.Days)), new Actual360(), new Handle<YieldTermStructure>())
        {
        }

        public DailyTenorEURLibor(int settlementDays, Handle<YieldTermStructure> h)
            : base("EURLibor", new Period(1, TimeUnit.Days), settlementDays, new EURCurrency(), new TARGET(),
                Utils.eurliborConvention(new Period(1, TimeUnit.Days)), Utils.eurliborEOM(new Period(1, TimeUnit.Days)), new Actual360(), h)
        {
        }
    }
}
