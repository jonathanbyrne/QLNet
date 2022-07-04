using QLNet.Currencies;
using QLNet.Indexes.Ibor;
using QLNet.Termstructures;
using QLNet.Time;
using QLNet.Time.Calendars;
using QLNet.Time.DayCounters;

namespace QLNet.Indexes.swap
{
    [JetBrains.Annotations.PublicAPI] public class UsdLiborSwapIsdaFixPm : SwapIndex
    {
        public UsdLiborSwapIsdaFixPm(Period tenor)
            : this(tenor, new Handle<YieldTermStructure>()) { }

        public UsdLiborSwapIsdaFixPm(Period tenor, Handle<YieldTermStructure> h)
            : base("UsdLiborSwapIsdaFixPm", // familyName
                tenor,
                2, // settlementDays
                new USDCurrency(),
                new TARGET(),
                new Period(6, TimeUnit.Months), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                new USDLibor(new Period(3, TimeUnit.Months), h))
        { }
    }
}