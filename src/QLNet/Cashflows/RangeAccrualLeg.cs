using System.Collections.Generic;
using System.Linq;
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class RangeAccrualLeg
    {
        public RangeAccrualLeg(Schedule schedule, IborIndex index)
        {
            schedule_ = schedule;
            index_ = index;
            paymentAdjustment_ = BusinessDayConvention.Following;
            observationConvention_ = BusinessDayConvention.ModifiedFollowing;
        }
        public RangeAccrualLeg withNotionals(double notional)
        {
            notionals_ = new InitializedList<double>(1, notional);
            return this;
        }
        public RangeAccrualLeg withNotionals(List<double> notionals)
        {
            notionals_ = notionals;
            return this;
        }
        public RangeAccrualLeg withPaymentDayCounter(DayCounter dayCounter)
        {
            paymentDayCounter_ = dayCounter;
            return this;
        }
        public RangeAccrualLeg withPaymentAdjustment(BusinessDayConvention convention)
        {
            paymentAdjustment_ = convention;
            return this;
        }
        public RangeAccrualLeg withFixingDays(int fixingDays)
        {
            fixingDays_ = new InitializedList<int>(1, fixingDays);
            return this;
        }
        public RangeAccrualLeg withFixingDays(List<int> fixingDays)
        {
            fixingDays_ = fixingDays;
            return this;
        }
        public RangeAccrualLeg withGearings(double gearing)
        {
            gearings_ = new InitializedList<double>(1, gearing);
            return this;
        }
        public RangeAccrualLeg withGearings(List<double> gearings)
        {
            gearings_ = gearings;
            return this;
        }
        public RangeAccrualLeg withSpreads(double spread)
        {
            spreads_ = new InitializedList<double>(1, spread);
            return this;
        }
        public RangeAccrualLeg withSpreads(List<double> spreads)
        {
            spreads_ = spreads;
            return this;
        }
        public RangeAccrualLeg withLowerTriggers(double trigger)
        {
            lowerTriggers_ = new InitializedList<double>(1, trigger);
            return this;
        }
        public RangeAccrualLeg withLowerTriggers(List<double> triggers)
        {
            lowerTriggers_ = triggers;
            return this;
        }
        public RangeAccrualLeg withUpperTriggers(double trigger)
        {
            upperTriggers_ = new InitializedList<double>(1, trigger);
            return this;
        }
        public RangeAccrualLeg withUpperTriggers(List<double> triggers)
        {
            upperTriggers_ = triggers;
            return this;
        }
        public RangeAccrualLeg withObservationTenor(Period tenor)
        {
            observationTenor_ = tenor;
            return this;
        }
        public RangeAccrualLeg withObservationConvention(BusinessDayConvention convention)
        {
            observationConvention_ = convention;
            return this;
        }
        public List<CashFlow> Leg()
        {
            Utils.QL_REQUIRE(!notionals_.empty(), () => "no notional given");

            var n = schedule_.Count - 1;
            Utils.QL_REQUIRE(notionals_.Count <= n, () =>
                "too many nominals (" + notionals_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(fixingDays_.Count <= n, () =>
                "too many fixingDays (" + fixingDays_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(gearings_.Count <= n, () =>
                "too many gearings (" + gearings_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(spreads_.Count <= n, () =>
                "too many spreads (" + spreads_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(lowerTriggers_.Count <= n, () =>
                "too many lowerTriggers (" + lowerTriggers_.Count + "), only " + n + " required");
            Utils.QL_REQUIRE(upperTriggers_.Count <= n, () =>
                "too many upperTriggers (" + upperTriggers_.Count + "), only " + n + " required");

            var leg = new List<CashFlow>();


            // the following is not always correct
            var calendar = schedule_.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;
            var observationsSchedules = new List<Schedule>();

            for (var i = 0; i < n; ++i)
            {
                refStart = start = schedule_.date(i);
                refEnd = end = schedule_.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdjustment_);
                if (i == 0 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refStart = calendar.adjust(end - schedule_.tenor(), bdc);
                }
                if (i == n - 1 && !schedule_.isRegular(i + 1))
                {
                    var bdc = schedule_.businessDayConvention();
                    refEnd = calendar.adjust(start + schedule_.tenor(), bdc);
                }
                if (gearings_.Get(i, 1.0).IsEqual(0.0))
                {
                    // fixed coupon
                    leg.Add(new FixedRateCoupon(paymentDate,
                        notionals_.Get(i),
                        spreads_.Get(i, 0.0),
                        paymentDayCounter_,
                        start, end, refStart, refEnd));
                }
                else
                {
                    // floating coupon
                    observationsSchedules.Add(new Schedule(start, end,
                        observationTenor_, calendar,
                        observationConvention_,
                        observationConvention_,
                        DateGeneration.Rule.Forward, false));

                    leg.Add(new RangeAccrualFloatersCoupon(paymentDate,
                        notionals_.Get(i),
                        index_,
                        start, end,
                        fixingDays_.Get(i, 2),
                        paymentDayCounter_,
                        gearings_.Get(i, 1.0),
                        spreads_.Get(i, 0.0),
                        refStart, refEnd,
                        observationsSchedules.Last(),
                        lowerTriggers_.Get(i),
                        upperTriggers_.Get(i)));
                }
            }
            return leg;

        }


        private Schedule schedule_;
        private IborIndex index_;
        private List<double> notionals_;
        private DayCounter paymentDayCounter_;
        private BusinessDayConvention paymentAdjustment_;
        private List<int> fixingDays_;
        private List<double> gearings_;
        private List<double> spreads_;
        private List<double> lowerTriggers_, upperTriggers_;
        private Period observationTenor_;
        private BusinessDayConvention observationConvention_;
    }
}