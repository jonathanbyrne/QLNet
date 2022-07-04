using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class BulletPricipalLeg : PrincipalLegBase
    {
        // constructor
        public BulletPricipalLeg(Schedule schedule)
        {
            schedule_ = schedule;
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        // creator
        public override List<CashFlow> value()
        {
            var leg = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule_.calendar();

            // first period might be short or long
            Date start = schedule_[0], end = schedule_[1];
            var paymentDate = calendar.adjust(start, paymentAdjustment_);
            var nominal = notionals_[0];

            leg.Add(new Principal(nominal, nominal, paymentDate, start, end, dayCounter_, start, end));

            paymentDate = calendar.adjust(end, paymentAdjustment_);
            leg.Add(new Principal(nominal * -1, 0, paymentDate, start, end, dayCounter_, start, end));

            return leg;
        }
    }
}