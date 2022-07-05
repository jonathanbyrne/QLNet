using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [PublicAPI]
    public class ConvertibleFloatingRateBond : ConvertibleBond
    {
        public ConvertibleFloatingRateBond(Exercise exercise,
            double conversionRatio,
            DividendSchedule dividends,
            CallabilitySchedule callability,
            Handle<Quote> creditSpread,
            Date issueDate,
            int settlementDays,
            IborIndex index,
            int fixingDays,
            List<double> spreads,
            DayCounter dayCounter,
            Schedule schedule,
            double redemption = 100)
            : base(
                exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule,
                redemption)
        {
            // !!! notional forcibly set to 100
            cashflows_ = new IborLeg(schedule, index)
                .withPaymentDayCounter(dayCounter)
                .withFixingDays(fixingDays)
                .withSpreads(spreads)
                .withNotionals(100.0)
                .withPaymentAdjustment(schedule.businessDayConvention());

            addRedemptionsToCashflows(new List<double> { redemption });

            Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

            option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_,
                dayCounter, schedule,
                issueDate, settlementDays, redemption);
        }
    }
}
