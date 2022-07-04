using System.Collections.Generic;
using QLNet.Time;

namespace QLNet.Instruments.Bonds
{
    /// <summary>
    /// Callable zero coupon bond class.
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class CallableZeroCouponBond : CallableFixedRateBond
    {
        public CallableZeroCouponBond(int settlementDays,
            double faceAmount,
            Calendar calendar,
            Date maturityDate,
            DayCounter dayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100.0,
            Date issueDate = null,
            CallabilitySchedule putCallSchedule = null)
            : base(settlementDays, faceAmount, new Schedule(issueDate, maturityDate,
                    new Period(Frequency.Once),
                    calendar,
                    paymentConvention,
                    paymentConvention,
                    DateGeneration.Rule.Backward,
                    false),
                new List<double>() { 0.0 }, dayCounter, paymentConvention, redemption, issueDate, putCallSchedule)
        { }
    }
}