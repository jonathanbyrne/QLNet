using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class FixedLoan : Loan
    {
        private DayCounter fixedDayCount_;
        private double fixedRate_;
        private Schedule fixedSchedule_;
        private double nominal_;
        private BusinessDayConvention paymentConvention_;
        private Schedule principalSchedule_;
        private Type type_;

        public FixedLoan(Type type, double nominal,
            Schedule fixedSchedule, double fixedRate, DayCounter fixedDayCount,
            Schedule principalSchedule, BusinessDayConvention? paymentConvention) :
            base(2)
        {
            type_ = type;
            nominal_ = nominal;
            fixedSchedule_ = fixedSchedule;
            fixedRate_ = fixedRate;
            fixedDayCount_ = fixedDayCount;
            principalSchedule_ = principalSchedule;

            if (paymentConvention.HasValue)
            {
                paymentConvention_ = paymentConvention.Value;
            }
            else
            {
                paymentConvention_ = fixedSchedule_.businessDayConvention();
            }

            List<CashFlow> principalLeg = new PricipalLeg(principalSchedule, fixedDayCount)
                .withNotionals(nominal)
                .withPaymentAdjustment(paymentConvention_)
                .withSign(type == Type.Loan ? -1 : 1);

            // temporary
            for (var i = 0; i < principalLeg.Count - 1; i++)
            {
                var p = (Principal)principalLeg[i];
                notionals_.Add(p.nominal());
            }

            List<CashFlow> fixedLeg = new FixedRateLeg(fixedSchedule)
                .withCouponRates(fixedRate, fixedDayCount)
                .withPaymentAdjustment(paymentConvention_)
                .withNotionals(notionals_);

            legs_[0] = fixedLeg;
            legs_[1] = principalLeg;
            if (type_ == Type.Loan)
            {
                payer_[0] = +1;
                payer_[1] = -1;
            }
            else
            {
                payer_[0] = -1;
                payer_[1] = +1;
            }
        }

        public List<CashFlow> fixedLeg() => legs_[0];

        public List<CashFlow> principalLeg() => legs_[1];
    }
}
