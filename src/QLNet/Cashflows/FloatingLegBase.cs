using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    public abstract class FloatingLegBase : RateLegBase
    {
        protected List<double?> caps_ = new List<double?>();
        protected List<int> fixingDays_;
        protected List<double?> floors_ = new List<double?>();
        protected List<double> gearings_;
        protected bool inArrears_;
        protected InterestRateIndex index_;
        protected List<double> spreads_;
        protected bool zeroPayments_;

        public FloatingLegBase inArrears() => inArrears(true);

        public FloatingLegBase inArrears(bool flag)
        {
            inArrears_ = flag;
            return this;
        }

        public FloatingLegBase withCaps(double? cap)
        {
            caps_ = new List<double?> { cap };
            return this;
        }

        public FloatingLegBase withCaps(List<double?> caps)
        {
            caps_ = caps;
            return this;
        }

        public FloatingLegBase withFixingDays(int fixingDays)
        {
            fixingDays_ = new List<int> { fixingDays };
            return this;
        }

        public FloatingLegBase withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }

        public FloatingLegBase withFloors(double? floor)
        {
            floors_ = new List<double?> { floor };
            return this;
        }

        public FloatingLegBase withFloors(List<double?> floors)
        {
            floors_ = floors;
            return this;
        }

        public FloatingLegBase withGearings(double gearing)
        {
            gearings_ = new List<double> { gearing };
            return this;
        }

        public FloatingLegBase withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }

        // initializers
        public FloatingLegBase withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }

        public FloatingLegBase withSpreads(double spread)
        {
            spreads_ = new List<double> { spread };
            return this;
        }

        public FloatingLegBase withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }

        public FloatingLegBase withZeroPayments() => withZeroPayments(true);

        public FloatingLegBase withZeroPayments(bool flag)
        {
            zeroPayments_ = flag;
            return this;
        }
    }
}
