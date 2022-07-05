using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class CmsLeg : FloatingLegBase
    {
        public CmsLeg(Schedule schedule, SwapIndex swapIndex)
        {
            schedule_ = schedule;
            index_ = swapIndex;
            paymentAdjustment_ = BusinessDayConvention.Following;
            inArrears_ = false;
            zeroPayments_ = false;
        }

        public override List<CashFlow> value() =>
            CashFlowVectors.FloatingLeg<SwapIndex, CmsCoupon, CappedFlooredCmsCoupon>(
                notionals_, schedule_, index_ as SwapIndex, paymentDayCounter_, paymentAdjustment_, fixingDays_, gearings_, spreads_, caps_, floors_, inArrears_, zeroPayments_);
    }
}
