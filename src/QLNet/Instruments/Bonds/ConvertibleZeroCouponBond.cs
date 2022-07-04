using System.Collections.Generic;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    [JetBrains.Annotations.PublicAPI] public class ConvertibleZeroCouponBond : ConvertibleBond
    {
        public ConvertibleZeroCouponBond(Exercise exercise,
            double conversionRatio,
            DividendSchedule dividends,
            CallabilitySchedule callability,
            Handle<Quote> creditSpread,
            Date issueDate,
            int settlementDays,
            DayCounter dayCounter,
            Schedule schedule,
            double redemption = 100)
            : base(
                exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule,
                redemption)
        {
            cashflows_ = new List<CashFlow>();

            // !!! notional forcibly set to 100
            setSingleRedemption(100.0, redemption, maturityDate_);

            option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_,
                dayCounter, schedule,
                issueDate, settlementDays, redemption);
        }
    }
}