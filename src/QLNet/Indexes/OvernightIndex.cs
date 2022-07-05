using JetBrains.Annotations;
using QLNet.Currencies;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes
{
    [PublicAPI]
    public class OvernightIndex : IborIndex
    {
        public OvernightIndex(string familyName,
            int settlementDays,
            Currency currency,
            Calendar fixingCalendar,
            DayCounter dayCounter,
            Handle<YieldTermStructure> h = null) :
            base(familyName, new Period(1, TimeUnit.Days), settlementDays,
                currency, fixingCalendar, BusinessDayConvention.Following, false, dayCounter, h)
        {
        }

        //! returns a copy of itself linked to a different forwarding curve
        public new OvernightIndex clone(Handle<YieldTermStructure> h) =>
            new OvernightIndex(familyName(), fixingDays(), currency(), fixingCalendar(),
                dayCounter(), h);
    }
}
