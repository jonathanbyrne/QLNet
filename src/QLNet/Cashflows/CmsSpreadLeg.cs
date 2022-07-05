using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes.swap;
using QLNet.Time;

namespace QLNet.Cashflows
{
    /// <summary>
    ///     helper class building a sequence of capped/floored cms-spread-rate coupons
    /// </summary>
    [PublicAPI]
    public class CmsSpreadLeg : FloatingLegBase
    {
        private SwapSpreadIndex swapSpreadIndex_;

        public CmsSpreadLeg(Schedule schedule, SwapSpreadIndex swapSpreadIndex)
        {
            schedule_ = schedule;
            swapSpreadIndex_ = swapSpreadIndex;
            paymentAdjustment_ = BusinessDayConvention.Following;
            inArrears_ = false;
            zeroPayments_ = false;
        }

        public override List<CashFlow> value() =>
            CashFlowVectors.FloatingLeg<SwapSpreadIndex, CmsSpreadCoupon, CappedFlooredCmsSpreadCoupon>(
                notionals_, schedule_, swapSpreadIndex_, paymentDayCounter_,
                paymentAdjustment_, fixingDays_, gearings_, spreads_, caps_,
                floors_, inArrears_, zeroPayments_);
    }
}
