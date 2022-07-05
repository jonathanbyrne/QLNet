using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Indexes;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class RangeAccrualPricer : FloatingRateCouponPricer
    {
        protected double accrualFactor_; // T-S
        protected RangeAccrualFloatersCoupon coupon_;
        protected double discount_;
        protected double endTime_; // T
        protected double gearing_;
        protected List<double> initialValues_;
        protected double lowerTrigger_;
        protected int observationsNo_;
        protected List<double> observationTimeLags_; // d
        protected List<double> observationTimes_; // U
        protected double spread_;
        protected double spreadLegValue_;
        protected double startTime_; // S
        protected double upperTrigger_;

        public override double capletPrice(double effectiveCap)
        {
            Utils.QL_FAIL("RangeAccrualPricer::capletPrice not implemented");
            return 0;
        }

        public override double capletRate(double effectiveCap)
        {
            Utils.QL_FAIL("RangeAccrualPricer::capletRate not implemented");
            return 0;
        }

        public override double floorletPrice(double effectiveFloor)
        {
            Utils.QL_FAIL("RangeAccrualPricer::floorletPrice not implemented");
            return 0;
        }

        public override double floorletRate(double effectiveFloor)
        {
            Utils.QL_FAIL("RangeAccrualPricer::floorletRate not implemented");
            return 0;
        }

        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as RangeAccrualFloatersCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "range-accrual coupon required");
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();

            var paymentDate = coupon_.date();

            var index = coupon_.index() as IborIndex;
            Utils.QL_REQUIRE(index != null, () => "invalid index");
            var rateCurve = index.forwardingTermStructure();
            discount_ = rateCurve.link.discount(paymentDate);
            accrualFactor_ = coupon_.accrualPeriod();
            spreadLegValue_ = spread_ * accrualFactor_ * discount_;

            startTime_ = coupon_.startTime();
            endTime_ = coupon_.endTime();
            observationTimes_ = coupon_.observationTimes();
            lowerTrigger_ = coupon_.lowerTrigger();
            upperTrigger_ = coupon_.upperTrigger();
            observationsNo_ = coupon_.observationsNo();

            var observationDates = coupon_.observationsSchedule().dates();
            Utils.QL_REQUIRE(observationDates.Count == observationsNo_ + 2, () => "incompatible size of initialValues vector");
            initialValues_ = new InitializedList<double>(observationDates.Count, 0.0);

            var calendar = index.fixingCalendar();
            for (var i = 0; i < observationDates.Count; i++)
            {
                initialValues_[i] = index.fixing(
                    calendar.advance(observationDates[i], -coupon_.fixingDays, TimeUnit.Days));
            }
        }

        // Observer interface
        public override double swapletPrice() => throw new NotImplementedException();

        public override double swapletRate() => swapletPrice() / (accrualFactor_ * discount_);
    }
}
