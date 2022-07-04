using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class CappedFlooredIborCoupon : CappedFlooredCoupon
    {
        // need by CashFlowVectors
        public CappedFlooredIborCoupon() { }

        public CappedFlooredIborCoupon(Date paymentDate,
            double nominal,
            Date startDate,
            Date endDate,
            int fixingDays,
            IborIndex index,
            double gearing = 1.0,
            double spread = 0.0,
            double? cap = null,
            double? floor = null,
            Date refPeriodStart = null,
            Date refPeriodEnd = null,
            DayCounter dayCounter = null,
            bool isInArrears = false)
            : base(new IborCoupon(paymentDate, nominal, startDate, endDate, fixingDays, index, gearing, spread, refPeriodStart, refPeriodEnd, dayCounter, isInArrears) as FloatingRateCoupon, cap, floor)
        {
        }

        // Factory - for Leg generators
        public virtual CashFlow Factory(double nominal, Date paymentDate, Date startDate, Date endDate, int fixingDays, IborIndex index, double gearing, double spread, double? cap, double? floor, Date refPeriodStart, Date refPeriodEnd, DayCounter dayCounter, bool isInArrears) => new CappedFlooredIborCoupon(paymentDate, nominal, startDate, endDate, fixingDays, index, gearing, spread, cap, floor, refPeriodStart, refPeriodEnd, dayCounter, isInArrears);
    }
}