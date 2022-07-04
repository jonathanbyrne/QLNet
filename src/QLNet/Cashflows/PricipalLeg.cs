using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class PricipalLeg : PrincipalLegBase
    {
        // constructor
        public PricipalLeg(Schedule schedule, DayCounter paymentDayCounter)
        {
            schedule_ = schedule;
            paymentAdjustment_ = BusinessDayConvention.Following;
            dayCounter_ = paymentDayCounter;
        }

        // creator
        public override List<CashFlow> value()
        {
            var leg = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule_.calendar();

            // first period
            Date start = schedule_[0], end = schedule_[schedule_.Count - 1];
            var paymentDate = calendar.adjust(start, paymentAdjustment_);
            var nominal = notionals_[0];
            var quota = nominal / (schedule_.Count - 1);

            leg.Add(new Principal(nominal * sign_, nominal, paymentDate, start, end, dayCounter_, start, end));

            if (schedule_.Count == 2)
            {
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                leg.Add(new Principal(nominal * sign_ * -1, 0, paymentDate, start, end, dayCounter_, start, end));
            }
            else
            {
                end = schedule_[0];
                // regular periods
                for (var i = 1; i <= schedule_.Count - 1; ++i)
                {
                    start = end; end = schedule_[i];
                    paymentDate = calendar.adjust(start, paymentAdjustment_);
                    nominal -= quota;

                    leg.Add(new Principal(quota * sign_ * -1, nominal, paymentDate, start, end, dayCounter_, start, end));
                }
            }

            return leg;
        }
    }
}