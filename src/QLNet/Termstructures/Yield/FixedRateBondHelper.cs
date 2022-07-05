using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Instruments.Bonds;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class FixedRateBondHelper : BondHelper
    {
        protected FixedRateBond fixedRateBond_;

        public FixedRateBondHelper(Handle<Quote> price,
            int settlementDays,
            double faceAmount,
            Schedule schedule,
            List<double> coupons,
            DayCounter dayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            double redemption = 100,
            Date issueDate = null,
            Calendar paymentCalendar = null,
            Period exCouponPeriod = null,
            Calendar exCouponCalendar = null,
            BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
            bool exCouponEndOfMonth = false,
            bool useCleanPrice = true)
            : base(price, new FixedRateBond(settlementDays, faceAmount, schedule,
                coupons, dayCounter, paymentConvention,
                redemption, issueDate, paymentCalendar,
                exCouponPeriod, exCouponCalendar,
                exCouponConvention, exCouponEndOfMonth), useCleanPrice)
        {
            fixedRateBond_ = bond_ as FixedRateBond;
        }

        public FixedRateBond fixedRateBond() => fixedRateBond_;
    }
}
