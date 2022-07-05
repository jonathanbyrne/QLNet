using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class OvernightLeg : RateLegBase
    {
        private List<double> gearings_;
        private OvernightIndex overnightIndex_;
        private List<double> spreads_;

        public OvernightLeg(Schedule schedule, OvernightIndex overnightIndex)
        {
            schedule_ = schedule;
            overnightIndex_ = overnightIndex;
            paymentAdjustment_ = BusinessDayConvention.Following;
        }

        public override List<CashFlow> value() => CashFlowVectors.OvernightLeg(notionals_, schedule_, paymentAdjustment_, overnightIndex_, gearings_, spreads_, paymentDayCounter_);

        public OvernightLeg withGearings(double gearing)
        {
            gearings_ = new List<double>();
            gearings_.Add(gearing);
            return this;
        }

        public OvernightLeg withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }

        public new OvernightLeg withNotionals(double notional)
        {
            notionals_ = new List<double>();
            notionals_.Add(notional);
            return this;
        }

        public new OvernightLeg withNotionals(List<double> notionals)
        {
            notionals_ = notionals;
            return this;
        }

        public new OvernightLeg withPaymentAdjustment(BusinessDayConvention convention)
        {
            paymentAdjustment_ = convention;
            return this;
        }

        public OvernightLeg withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }

        public OvernightLeg withSpreads(double spread)
        {
            spreads_ = new List<double>();
            spreads_.Add(spread);
            return this;
        }

        public OvernightLeg withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }
    }
}
