using System.Collections.Generic;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    public abstract class CPILegBase : RateLegBase
    {
        public CPILegBase withObservationInterpolation(InterpolationType interp)
        {
            observationInterpolation_ = interp;
            return this;
        }

        public CPILegBase withFixedRates(double fixedRate)
        {
            fixedRates_ = new List<double>() { fixedRate };
            return this;
        }

        public CPILegBase withFixedRates(List<double> fixedRates)
        {
            fixedRates_ = fixedRates;
            return this;
        }

        public CPILegBase withSubtractInflationNominal(bool growthOnly)
        {
            subtractInflationNominal_ = growthOnly;
            return this;
        }

        public CPILegBase withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }

        public CPILegBase withPaymentCalendar(Calendar cal)
        {
            paymentCalendar_ = cal;
            return this;
        }


        public CPILegBase withFixingDays(int fixingDays)
        {
            fixingDays_ = new List<int>() { fixingDays };
            return this;
        }

        public CPILegBase withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }

        public CPILegBase withSpreads(double spread)
        {
            spreads_ = new List<double>() { spread };
            return this;
        }

        public CPILegBase withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }

        public CPILegBase withCaps(double cap)
        {
            caps_ = new List<double?>() { cap };
            return this;
        }

        public CPILegBase withCaps(List<double?> cap)
        {
            caps_ = cap;
            return this;
        }

        public CPILegBase withFloors(double floors)
        {
            floors_ = new List<double?>() { floors };
            return this;
        }

        public CPILegBase withFloors(List<double?> floors)
        {
            floors_ = floors;
            return this;
        }

        public CPILegBase withExCouponPeriod(Period period, Calendar cal, BusinessDayConvention convention, bool endOfMonth = false)
        {
            exCouponPeriod_ = period;
            exCouponCalendar_ = cal;
            exCouponAdjustment_ = convention;
            exCouponEndOfMonth_ = endOfMonth;
            return this;
        }

        protected ZeroInflationIndex index_;
        protected double baseCPI_;
        protected Period observationLag_;
        protected List<double> fixedRates_;  // aka gearing
        protected List<int> fixingDays_;
        protected InterpolationType observationInterpolation_;
        protected bool subtractInflationNominal_;
        protected List<double> spreads_;
        protected List<double?> caps_, floors_;
        protected Calendar paymentCalendar_;

        protected Period exCouponPeriod_;
        protected Calendar exCouponCalendar_;
        protected BusinessDayConvention exCouponAdjustment_;
        protected bool exCouponEndOfMonth_;
    }
}