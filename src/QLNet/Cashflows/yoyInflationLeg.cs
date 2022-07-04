using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class yoyInflationLeg : yoyInflationLegBase
    {
        public yoyInflationLeg(Schedule schedule, Calendar cal,
            YoYInflationIndex index,
            Period observationLag)
        {
            schedule_ = schedule;
            index_ = index;
            observationLag_ = observationLag;
            paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
            paymentCalendar_ = cal;
        }


        public override List<CashFlow> value() =>
            CashFlowVectors.yoyInflationLeg(notionals_,
                schedule_,
                paymentAdjustment_,
                index_,
                gearings_,
                spreads_,
                paymentDayCounter_,
                caps_,
                floors_,
                paymentCalendar_,
                fixingDays_,
                observationLag_);
    }
}