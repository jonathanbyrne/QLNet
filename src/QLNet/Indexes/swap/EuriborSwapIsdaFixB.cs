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
    public class EuriborSwapIsdaFixB : SwapIndex
    {
        public EuriborSwapIsdaFixB(Period tenor)
            : this(tenor, new Handle<YieldTermStructure>())
        {
        }

        public EuriborSwapIsdaFixB(Period tenor, Handle<YieldTermStructure> h)
            : base("EuriborSwapIsdaFixB", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1, TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1, TimeUnit.Years) ? new Euribor(new Period(6, TimeUnit.Months), h) : new Euribor(new Period(3, TimeUnit.Months), h))
        {
        }

        public EuriborSwapIsdaFixB(Period tenor,
            Handle<YieldTermStructure> forwarding,
            Handle<YieldTermStructure> discounting)
            : base("EuriborSwapIsdaFixB", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1, TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1, TimeUnit.Years) ? new Euribor(new Period(6, TimeUnit.Months), forwarding) : new Euribor(new Period(3, TimeUnit.Months), forwarding),
                discounting)
        {
        }
    }
}
