using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    public abstract class yoyInflationLegBase : RateLegBase
    {
        protected List<double?> caps_, floors_;
        protected List<int> fixingDays_;
        protected List<double> gearings_;
        protected YoYInflationIndex index_;
        protected Period observationLag_;
        protected Calendar paymentCalendar_;
        protected List<double> spreads_;

        public yoyInflationLegBase withCaps(double cap)
        {
            caps_ = new List<double?>();
            caps_.Add(cap);
            return this;
        }

        public yoyInflationLegBase withCaps(List<double?> caps)
        {
            caps_ = caps;
            return this;
        }

        public yoyInflationLegBase withFixingDays(int fixingDays)
        {
            fixingDays_ = new List<int>();
            fixingDays_.Add(fixingDays);
            return this;
        }

        public yoyInflationLegBase withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }

        public yoyInflationLegBase withFloors(double floor)
        {
            floors_ = new List<double?>();
            floors_.Add(floor);
            return this;
        }

        public yoyInflationLegBase withFloors(List<double?> floors)
        {
            floors_ = floors;
            return this;
        }

        public yoyInflationLegBase withGearings(double gearing)
        {
            gearings_ = new List<double>();
            gearings_.Add(gearing);
            return this;
        }

        public yoyInflationLegBase withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }

        public yoyInflationLegBase withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }

        public yoyInflationLegBase withSpreads(double spread)
        {
            spreads_ = new List<double>();
            spreads_.Add(spread);
            return this;
        }

        public yoyInflationLegBase withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }
    }
}
