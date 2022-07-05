using System;
using JetBrains.Annotations;
using QLNet.Indexes;

namespace QLNet.Cashflows
{
    [PublicAPI]
    public class OvernightIndexedCouponPricer : FloatingRateCouponPricer
    {
        private OvernightIndexedCoupon coupon_;

        public override double capletPrice(double d)
        {
            Utils.QL_FAIL("capletPrice not available");
            return 0;
        }

        public override double capletRate(double d)
        {
            Utils.QL_FAIL("capletRate not available");
            return 0;
        }

        public override double floorletPrice(double d)
        {
            Utils.QL_FAIL("floorletPrice not available");
            return 0;
        }

        public override double floorletRate(double d)
        {
            Utils.QL_FAIL("floorletRate not available");
            return 0;
        }

        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as OvernightIndexedCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "wrong coupon ExerciseType");
        }

        public override double swapletPrice()
        {
            Utils.QL_FAIL("swapletPrice not available");
            return 0;
        }

        public override double swapletRate()
        {
            var index = coupon_.index() as OvernightIndex;

            var fixingDates = coupon_.fixingDates();
            var dt = coupon_.dt();

            var n = dt.Count;
            var i = 0;

            var compoundFactor = 1.0;

            // already fixed part
            var today = Settings.evaluationDate();
            while (fixingDates[i] < today && i < n)
            {
                // rate must have been fixed
                var pastFixing = IndexManager.instance().getHistory(index.name())[fixingDates[i]];

                Utils.QL_REQUIRE(pastFixing != null, () => "Missing " + index.name() + " fixing for " + fixingDates[i]);

                compoundFactor *= 1.0 + pastFixing.GetValueOrDefault() * dt[i];
                ++i;
            }

            // today is a border case
            if (fixingDates[i] == today && i < n)
            {
                // might have been fixed
                try
                {
                    var pastFixing = IndexManager.instance().getHistory(index.name())[fixingDates[i]];

                    if (pastFixing != null)
                    {
                        compoundFactor *= 1.0 + pastFixing.GetValueOrDefault() * dt[i];
                        ++i;
                    }
                }
                catch (Exception)
                {
                    // fall through and forecast
                }
            }

            // forward part using telescopic property in order
            // to avoid the evaluation of multiple forward fixings
            if (i < n)
            {
                var curve = index.forwardingTermStructure();
                Utils.QL_REQUIRE(!curve.empty(), () => "null term structure set to this instance of" + index.name());

                var dates = coupon_.valueDates();
                var startDiscount = curve.link.discount(dates[i]);
                var endDiscount = curve.link.discount(dates[n]);

                compoundFactor *= startDiscount / endDiscount;
            }

            var rate = (compoundFactor - 1.0) / coupon_.accrualPeriod();
            return coupon_.gearing() * rate + coupon_.spread();
        }
    }
}
