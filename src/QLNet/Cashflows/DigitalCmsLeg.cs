using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class DigitalCmsLeg
    {
        private bool callATM_;
        private List<double> callPayoffs_;
        private List<double> callStrikes_;
        private List<int> fixingDays_;
        private List<double> gearings_;
        private bool inArrears_;
        private SwapIndex index_;
        private Position.Type longCallOption_;
        private Position.Type longPutOption_;
        private List<double> notionals_;
        private BusinessDayConvention paymentAdjustment_;
        private DayCounter paymentDayCounter_;
        private bool putATM_;
        private List<double> putPayoffs_;
        private List<double> putStrikes_;
        private DigitalReplication replication_;
        private Schedule schedule_;
        private List<double> spreads_;

        public DigitalCmsLeg(Schedule schedule, SwapIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
            inArrears_ = false;
            longCallOption_ = Position.Type.Long;
            callATM_ = false;
            longPutOption_ = Position.Type.Long;
            putATM_ = false;
        }

        public DigitalCmsLeg inArrears() => inArrears(true);

        public DigitalCmsLeg inArrears(bool flag)
        {
            inArrears_ = flag;
            return this;
        }

        public List<CashFlow> value() => CashFlowVectors.FloatingDigitalLeg<SwapIndex, CmsCoupon, DigitalCmsCoupon>(notionals_, schedule_, index_, paymentDayCounter_, paymentAdjustment_, fixingDays_, gearings_, spreads_, inArrears_, callStrikes_, longCallOption_, callATM_, callPayoffs_, putStrikes_, longPutOption_, putATM_, putPayoffs_, replication_);

        public DigitalCmsLeg withCallATM() => withCallATM(true);

        public DigitalCmsLeg withCallATM(bool flag)
        {
            callATM_ = flag;
            return this;
        }

        public DigitalCmsLeg withCallPayoffs(double payoff)
        {
            callPayoffs_ = new List<double>();
            callPayoffs_.Add(payoff);
            return this;
        }

        public DigitalCmsLeg withCallPayoffs(List<double> payoffs)
        {
            callPayoffs_ = payoffs;
            return this;
        }

        public DigitalCmsLeg withCallStrikes(double strike)
        {
            callStrikes_ = new List<double>();
            callStrikes_.Add(strike);
            return this;
        }

        public DigitalCmsLeg withCallStrikes(List<double> strikes)
        {
            callStrikes_ = strikes;
            return this;
        }

        public DigitalCmsLeg withFixingDays(int fixingDays)
        {
            fixingDays_ = new List<int>();
            fixingDays_.Add(fixingDays);
            return this;
        }

        public DigitalCmsLeg withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }

        public DigitalCmsLeg withGearings(double gearing)
        {
            gearings_ = new List<double>();
            gearings_.Add(gearing);
            return this;
        }

        public DigitalCmsLeg withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }

        public DigitalCmsLeg withLongCallOption(Position.Type type)
        {
            longCallOption_ = type;
            return this;
        }

        public DigitalCmsLeg withLongPutOption(Position.Type type)
        {
            longPutOption_ = type;
            return this;
        }

        public DigitalCmsLeg withNotionals(double notional)
        {
            notionals_ = new List<double>();
            notionals_.Add(notional);
            return this;
        }

        public DigitalCmsLeg withNotionals(List<double> notionals)
        {
            notionals_ = notionals;
            return this;
        }

        public DigitalCmsLeg withPaymentAdjustment(BusinessDayConvention convention)
        {
            paymentAdjustment_ = convention;
            return this;
        }

        public DigitalCmsLeg withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }

        public DigitalCmsLeg withPutATM() => withPutATM(true);

        public DigitalCmsLeg withPutATM(bool flag)
        {
            putATM_ = flag;
            return this;
        }

        public DigitalCmsLeg withPutPayoffs(double payoff)
        {
            putPayoffs_ = new List<double>();
            putPayoffs_.Add(payoff);
            return this;
        }

        public DigitalCmsLeg withPutPayoffs(List<double> payoffs)
        {
            putPayoffs_ = payoffs;
            return this;
        }

        public DigitalCmsLeg withPutStrikes(double strike)
        {
            putStrikes_ = new List<double>();
            putStrikes_.Add(strike);
            return this;
        }

        public DigitalCmsLeg withPutStrikes(List<double> strikes)
        {
            putStrikes_ = strikes;
            return this;
        }

        public DigitalCmsLeg withReplication() => withReplication(new DigitalReplication());

        public DigitalCmsLeg withReplication(DigitalReplication replication)
        {
            replication_ = replication;
            return this;
        }

        public DigitalCmsLeg withSpreads(double spread)
        {
            spreads_ = new List<double>();
            spreads_.Add(spread);
            return this;
        }

        public DigitalCmsLeg withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }
    }
}
