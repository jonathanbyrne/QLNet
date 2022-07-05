using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Cashflows;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class Cash : Loan
    {
        private double nominal_;
        private BusinessDayConvention paymentConvention_;
        private Schedule principalSchedule_;
        private Type type_;

        public Cash(Type type, double nominal,
            Schedule principalSchedule, BusinessDayConvention? paymentConvention) :
            base(1)
        {
            type_ = type;
            nominal_ = nominal;
            principalSchedule_ = principalSchedule;
            paymentConvention_ = paymentConvention.Value;

            List<CashFlow> principalLeg = new PricipalLeg(principalSchedule, new Actual365Fixed())
                .withNotionals(nominal)
                .withPaymentAdjustment(paymentConvention_)
                .withSign(type == Type.Loan ? -1 : 1);

            legs_[0] = principalLeg;
            if (type_ == Type.Loan)
            {
                payer_[0] = +1;
            }
            else
            {
                payer_[0] = -1;
            }
        }

        public List<CashFlow> principalLeg() => legs_[0];
    }
}
