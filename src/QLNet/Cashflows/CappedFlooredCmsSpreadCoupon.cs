using JetBrains.Annotations;
using QLNet.Indexes.swap;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class CappedFlooredCmsSpreadCoupon : CappedFlooredCoupon
    {
        public CappedFlooredCmsSpreadCoupon()
        {
        }

        public CappedFlooredCmsSpreadCoupon(Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            SwapSpreadIndex index,
            double gearing = 1.0,
            double spread = 0.0,
            double? cap = null,
            double? floor = null,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            DayCounter dayCounter = null,
            bool isInArrears = false)
            : base(new CmsSpreadCoupon(paymentDate, nominal, startDate, endDate, fixingDays,
                index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears), cap, floor)
        {
        }
    }
}
