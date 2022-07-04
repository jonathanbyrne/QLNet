using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class IborLeg : FloatingLegBase
    {
        // constructor
        public IborLeg(Schedule schedule, IborIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
            inArrears_ = false;
            zeroPayments_ = false;
        }

        public override List<CashFlow> value()
        {
            var cashflows = CashFlowVectors.FloatingLeg<IborIndex, IborCoupon, CappedFlooredIborCoupon>(
                notionals_, schedule_, index_ as IborIndex, paymentDayCounter_,
                paymentAdjustment_, fixingDays_, gearings_, spreads_,
                caps_, floors_, inArrears_, zeroPayments_);

            if (caps_.Count == 0 && floors_.Count == 0 && !inArrears_)
            {
                Utils.setCouponPricer(cashflows, new BlackIborCouponPricer());
            }
            return cashflows;
        }
    }
}