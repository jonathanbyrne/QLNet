using QLNet.Currencies;
using QLNet.Indexes;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class DailyTenorLibor : IborIndex
    {
        // http://www.bba.org.uk/bba/jsp/polopoly.jsp?d=225&a=1412 :
        // no o/n or s/n fixings (as the case may be) will take place
        // when the principal centre of the currency concerned is
        // closed but London is open on the fixing day.
        public DailyTenorLibor(string familyName, int settlementDays, Currency currency, Calendar financialCenterCalendar,
            DayCounter dayCounter)
            : this(familyName, settlementDays, currency, financialCenterCalendar, dayCounter, new Handle<YieldTermStructure>())
        {}

        public DailyTenorLibor(string familyName, int settlementDays, Currency currency, Calendar financialCenterCalendar,
            DayCounter dayCounter, Handle<YieldTermStructure> h)
            : base(familyName, new Period(1, TimeUnit.Days), settlementDays, currency,
                new JointCalendar(new UnitedKingdom(UnitedKingdom.Market.Exchange), financialCenterCalendar, JointCalendar.JointCalendarRule.JoinHolidays),
                Utils.liborConvention(new Period(1, TimeUnit.Days)), Utils.liborEOM(new Period(1, TimeUnit.Days)), dayCounter, h)
        {
            Utils.QL_REQUIRE(currency != new EURCurrency(), () =>
                "for EUR Libor dedicated EurLibor constructor must be used");
        }
    }
}