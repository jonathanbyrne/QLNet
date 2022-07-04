using QLNet.Cashflows;
using QLNet.Extensions;
using QLNet.Indexes;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.Optionlet;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class BlackIborCouponPricer : IborCouponPricer
    {
        public enum TimingAdjustment { Black76, BivariateLognormal }
        public BlackIborCouponPricer(Handle<OptionletVolatilityStructure> v = null,
            TimingAdjustment timingAdjustment  = TimingAdjustment.Black76,
            Handle<Quote> correlation = null)
            : base(v)
        {
            timingAdjustment_ = timingAdjustment;
            correlation_ = correlation ?? new Handle<Quote>(new SimpleQuote(1.0));

            Utils.QL_REQUIRE(timingAdjustment_ == TimingAdjustment.Black76 ||
                             timingAdjustment_ == TimingAdjustment.BivariateLognormal, () =>
                "unknown timing adjustment (code " + timingAdjustment_ + ")");
            correlation_.registerWith(update);
        }


        public override void initialize(FloatingRateCoupon coupon)
        {
            gearing_ = coupon.gearing();
            spread_ = coupon.spread();
            accrualPeriod_ = coupon.accrualPeriod();
            Utils.QL_REQUIRE(accrualPeriod_.IsNotEqual(0.0), () => "null accrual period");

            index_ = coupon.index() as IborIndex;
            if (index_ == null)
            {
                // check if the coupon was right
                var c = coupon as IborCoupon;
                Utils.QL_REQUIRE(c != null, () => "IborCoupon required");
                // coupon was right, index is not
                Utils.QL_FAIL("IborIndex required");
            }

            var rateCurve = index_.forwardingTermStructure();
            var paymentDate = coupon.date();
            if (paymentDate > rateCurve.link.referenceDate())
                discount_ = rateCurve.link.discount(paymentDate);
            else
                discount_ = 1.0;

            spreadLegValue_ = spread_ * accrualPeriod_ * discount_;

            coupon_ = coupon ;
        }
        public override double swapletPrice()
        {
            // past or future fixing is managed in InterestRateIndex::fixing()

            var swapletPrice = adjustedFixing() * accrualPeriod_ * discount_;
            return gearing_ * swapletPrice + spreadLegValue_;
        }
        public override double swapletRate() => swapletPrice() / (accrualPeriod_ * discount_);

        public override double capletPrice(double effectiveCap)
        {
            var capletPrice = optionletPrice(QLNet.Option.Type.Call, effectiveCap);
            return gearing_ * capletPrice;
        }
        public override double capletRate(double effectiveCap) => capletPrice(effectiveCap) / (accrualPeriod_ * discount_);

        public override double floorletPrice(double effectiveFloor)
        {
            var floorletPrice = optionletPrice(QLNet.Option.Type.Put, effectiveFloor);
            return gearing_ * floorletPrice;
        }
        public override double floorletRate(double effectiveFloor) => floorletPrice(effectiveFloor) / (accrualPeriod_ * discount_);

        protected double optionletPrice(Option.Type optionType, double effStrike)
        {
            var fixingDate = coupon_.fixingDate();
            if (fixingDate <= Settings.evaluationDate())
            {
                // the amount is determined
                double a;
                double b;
                if (optionType == QLNet.Option.Type.Call)
                {
                    a = coupon_.indexFixing();
                    b = effStrike;
                }
                else
                {
                    a = effStrike;
                    b = coupon_.indexFixing();
                }
                return System.Math.Max(a - b, 0.0) * accrualPeriod_ * discount_;
            }
            else
            {
                // not yet determined, use Black model
                Utils.QL_REQUIRE(!capletVolatility().empty(), () => "missing optionlet volatility");

                var stdDev = System.Math.Sqrt(capletVolatility().link.blackVariance(fixingDate, effStrike));
                var shift = capletVolatility().link.displacement();
                var shiftedLn = capletVolatility().link.volatilityType() == VolatilityType.ShiftedLognormal;
                var fixing =
                    shiftedLn
                        ? Utils.blackFormula(optionType, effStrike, adjustedFixing(), stdDev, 1.0, shift)
                        : Utils.bachelierBlackFormula(optionType, effStrike, adjustedFixing(), stdDev, 1.0);
                return fixing * accrualPeriod_ * discount_;
            }
        }
        protected virtual double adjustedFixing(double? fixing = null)
        {
            if (fixing == null)
                fixing = coupon_.indexFixing();

            if (!coupon_.isInArrears() && timingAdjustment_ == TimingAdjustment.Black76)
                return fixing.Value;

            Utils.QL_REQUIRE(!capletVolatility().empty(), () => "missing optionlet volatility");
            var d1 = coupon_.fixingDate();
            var referenceDate = capletVolatility().link.referenceDate();
            if (d1 <= referenceDate)
                return fixing.Value;
            var d2 = index_.valueDate(d1);
            var d3 = index_.maturityDate(d2);
            var tau = index_.dayCounter().yearFraction(d2, d3);
            var variance = capletVolatility().link.blackVariance(d1, fixing.Value);

            var shift = capletVolatility().link.displacement();
            var shiftedLn = capletVolatility().link.volatilityType() == VolatilityType.ShiftedLognormal;

            var adjustment = shiftedLn
                ? (fixing.Value + shift) * (fixing.Value + shift) * variance * tau / (1.0 + fixing.Value * tau)
                : variance * tau / (1.0 + fixing.Value * tau);

            if (timingAdjustment_ == TimingAdjustment.BivariateLognormal)
            {
                Utils.QL_REQUIRE(!correlation_.empty(), () => "no correlation given");
                var d4 = coupon_.date();
                var d5 = d4 >= d3 ? d3 : d2;
                var tau2 = index_.dayCounter().yearFraction(d5, d4);
                if (d4 >= d3)
                    adjustment = 0.0;
                // if d4 < d2 (payment before index start) we just apply the
                // Black76 in arrears adjustment
                if (tau2 > 0.0)
                {
                    var fixing2 = (index_.forwardingTermStructure().link.discount(d5) /
                                   index_.forwardingTermStructure().link.discount(d4) -
                                   1.0) / tau2;
                    adjustment -= shiftedLn
                        ? correlation_.link.value() * tau2 * variance * (fixing.Value + shift) * (fixing2 + shift) / (1.0 + fixing2 * tau2)
                        : correlation_.link.value() * tau2 * variance / (1.0 + fixing2 * tau2);
                }
            }
            return fixing.Value + adjustment;
        }

        protected double gearing_;
        protected double spread_;
        protected double accrualPeriod_;
        protected IborIndex index_;
        protected double discount_;
        protected double spreadLegValue_;
        protected FloatingRateCoupon coupon_;

        private TimingAdjustment timingAdjustment_;
        private Handle<Quote> correlation_;


    }
}