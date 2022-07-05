using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class Euribor365 : IborIndex
    {
        public Euribor365(Period tenor) : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public Euribor365(Period tenor, Handle<YieldTermStructure> h)
            : base("Euribor365", tenor,
                2, // settlement days
                new EURCurrency(), new TARGET(), Utils.euriborConvention(tenor), Utils.euriborEOM(tenor),
                new Actual365Fixed(), h)
        {
            QLNet.Utils.QL_REQUIRE(this.tenor().units() != TimeUnit.Days, () =>
                "for daily tenors (" + this.tenor() + ") dedicated DailyTenor constructor must be used");
        }
    }
}
