using System.Collections.Generic;
using System.Linq;
using QLNet.Time;

namespace QLNet.Cashflows
{
    /// <summary>
    /// Helper class building a sequence of average BMA coupons
    /// </summary>
    [JetBrains.Annotations.PublicAPI] public class AverageBmaLeg : RateLegBase
    {
        private BMAIndex index_;
        private List<double> gearings_;
        private List<double> spreads_;

        public AverageBmaLeg(Schedule schedule, BMAIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        public AverageBmaLeg WithPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }
        public AverageBmaLeg WithGearings(double gearing)
        {
            gearings_ = new List<double>() { gearing };
            return this;
        }
        public AverageBmaLeg WithGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }
        public AverageBmaLeg WithSpreads(double spread)
        {
            spreads_ = new List<double>() { spread };
            return this;
        }
        public AverageBmaLeg WithSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }

        public override List<CashFlow> value()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var cashflows = new List<CashFlow>();

            // the following is not always correct
            var calendar = schedule_.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            var n = schedule_.Count - 1;
            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule_.date(i);
                refEnd = end = schedule_.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i + 1))
                    refStart = calendar.adjust(end - schedule_.tenor(), paymentAdjustment_);
                if (i == n - 1 && !schedule_.isRegular(i + 1))
                    refEnd = calendar.adjust(start + schedule_.tenor(), paymentAdjustment_);

                cashflows.Add(new AverageBmaCoupon(paymentDate,
                    notionals_.Get(i, notionals_.Last()),
                    start, end,
                    index_,
                    gearings_.Get(i, 1.0),
                    spreads_.Get(i, 0.0),
                    refStart, refEnd,
                    paymentDayCounter_));
            }

            return cashflows;
        }
    }
}