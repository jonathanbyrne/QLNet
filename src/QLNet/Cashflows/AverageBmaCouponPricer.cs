using System.Linq;
using QLNet.Time;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class AverageBmaCouponPricer : FloatingRateCouponPricer
    {
        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as AverageBmaCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "wrong coupon ExerciseType");
        }

        public override double swapletRate()
        {
            var fixingDates = coupon_.FixingDates();
            var index = coupon_.index();

            var cutoffDays = 0; // to be verified
            Date startDate = coupon_.accrualStartDate() - cutoffDays,
                endDate = coupon_.accrualEndDate() - cutoffDays,
                d1 = startDate;

            Utils.QL_REQUIRE(fixingDates.Count > 0, () => "fixing date list empty");
            Utils.QL_REQUIRE(index.valueDate(fixingDates.First()) <= startDate, () => "first fixing date valid after period start");
            Utils.QL_REQUIRE(index.valueDate(fixingDates.Last()) >= endDate, () => "last fixing date valid before period end");

            var avgBma = 0.0;
            var days = 0;
            for (var i = 0; i < fixingDates.Count - 1; ++i)
            {
                var valueDate = index.valueDate(fixingDates[i]);
                var nextValueDate = index.valueDate(fixingDates[i + 1]);

                if (fixingDates[i] >= endDate || valueDate >= endDate)
                    break;
                if (fixingDates[i + 1] < startDate || nextValueDate <= startDate)
                    continue;

                var d2 = Date.Min(nextValueDate, endDate);

                avgBma += index.fixing(fixingDates[i]) * (d2 - d1);

                days += d2 - d1;
                d1 = d2;
            }
            avgBma /= endDate - startDate;

            Utils.QL_REQUIRE(days == endDate - startDate, () =>
                "averaging days " + days + " differ from " + "interest days " + (endDate - startDate));

            return coupon_.gearing() * avgBma + coupon_.spread();
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double swapletPrice()
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double capletPrice(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double capletRate(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double floorletPrice(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        /// <summary>
        /// not applicable here
        /// </summary>
        public override double floorletRate(double d)
        {
            Utils.QL_FAIL("not available");
            return 0;
        }

        // recheck
        //protected override double optionletPrice( QLNet.Option.Type t, double d )
        //{
        //   throw new Exception( "not available" );
        //}

        private AverageBmaCoupon coupon_;
    }
}