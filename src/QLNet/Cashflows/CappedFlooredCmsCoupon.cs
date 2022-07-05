using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class CappedFlooredCmsCoupon : CappedFlooredCoupon
    {
        // need by CashFlowVectors
        public CappedFlooredCmsCoupon()
        {
        }

        public CappedFlooredCmsCoupon(double nominal,
            Date paymentDate,
            Date startDate,
            Date endDate,
            int fixingDays,
            SwapIndex index,
            double gearing = 1.0,
            double spread = 0.0,
            double? cap = null,
            double? floor = null,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            DayCounter dayCounter = null,
            bool isInArrears = false)
            : base(new CmsCoupon(nominal, paymentDate, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears), cap, floor)
        {
        }

        // Factory - for Leg generators
        public override CashFlow Factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, InterestRateIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears) => new CappedFlooredCmsCoupon(nominal, paymentDate, startDate, endDate, fixingDays, (SwapIndex)index, gearing, spread, cap, floor, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
    }
}
