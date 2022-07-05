using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Indexes;
using QLNet.Instruments.Bonds;
using QLNet.Quotes;
using QLNet.Time;

namespace QLNet.Termstructures.Yield
{
    [PublicAPI]
    public class CPIBondHelper : BondHelper
    {
        protected CPIBond cpiBond_;

        public CPIBondHelper(Handle<Quote> price,
            int settlementDays,
            double faceAmount,
            bool growthOnly,
            double baseCPI,
            Period observationLag,
            ZeroInflationIndex cpiIndex,
            InterpolationType observationInterpolation,
            Schedule schedule,
            List<double> fixedRate,
            DayCounter accrualDayCounter,
            BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
            Date issueDate = null,
            Calendar paymentCalendar = null,
            Period exCouponPeriod = null,
            Calendar exCouponCalendar = null,
            BusinessDayConvention exCouponConvention = BusinessDayConvention.Unadjusted,
            bool exCouponEndOfMonth = false,
            bool useCleanPrice = true)
            : base(price, new CPIBond(settlementDays, faceAmount, growthOnly, baseCPI,
                observationLag, cpiIndex, observationInterpolation,
                schedule, fixedRate, accrualDayCounter, paymentConvention,
                issueDate, paymentCalendar, exCouponPeriod, exCouponCalendar,
                exCouponConvention, exCouponEndOfMonth), useCleanPrice)
        {
            cpiBond_ = bond_ as CPIBond;
        }

        public CPIBond cpiBond() => cpiBond_;
    }
}
