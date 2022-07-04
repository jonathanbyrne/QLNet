using QLNet.Indexes;
using QLNet.Termstructures.Volatility.Inflation;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class YoYInflationCouponPricer : InflationCouponPricer
    {
        public YoYInflationCouponPricer(Handle<YoYOptionletVolatilitySurface> capletVol = null)
        {
            capletVol_ = capletVol ?? new Handle<YoYOptionletVolatilitySurface>();

            if (!capletVol_.empty())
                capletVol_.registerWith(update);
        }

        public virtual Handle<YoYOptionletVolatilitySurface> capletVolatility() => capletVol_;

        public virtual void setCapletVolatility(Handle<YoYOptionletVolatilitySurface> capletVol)
        {
            Utils.QL_REQUIRE(!capletVol.empty(), () => "empty capletVol handle");

            capletVol_ = capletVol;
            capletVol_.registerWith(update);
        }

        // InflationCouponPricer interface
        public override double swapletPrice()
        {
            var swapletPrice = adjustedFixing() * coupon_.accrualPeriod() * discount_;
            return gearing_ * swapletPrice + spreadLegValue_;
        }

        public override double swapletRate() =>
            // This way we do not require the index to have
            // a yield curve, i.e. we do not get the problem
            // that a discounting-instrument-pricer is used
            // with a different yield curve
            gearing_ * adjustedFixing() + spread_;

        public override double capletPrice(double effectiveCap)
        {
            var capletPrice = optionletPrice(QLNet.Option.Type.Call, effectiveCap);
            return gearing_ * capletPrice;
        }

        public override double capletRate(double effectiveCap) => capletPrice(effectiveCap) / (coupon_.accrualPeriod() * discount_);

        public override double floorletPrice(double effectiveFloor)
        {
            var floorletPrice = optionletPrice(QLNet.Option.Type.Put, effectiveFloor);
            return gearing_ * floorletPrice;
        }
        public override double floorletRate(double effectiveFloor) =>
            floorletPrice(effectiveFloor) /
            (coupon_.accrualPeriod() * discount_);

        public override void initialize(InflationCoupon coupon)
        {
            coupon_ = coupon as YoYInflationCoupon;
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();
            paymentDate_ = coupon_.date();
            var y = (YoYInflationIndex)coupon.index();
            rateCurve_ = y.yoyInflationTermStructure().link.nominalTermStructure();

            // past or future fixing is managed in YoYInflationIndex::fixing()
            // use yield curve from index (which sets discount)

            discount_ = 1.0;
            if (paymentDate_ > rateCurve_.link.referenceDate())
                discount_ = rateCurve_.link.discount(paymentDate_);

            spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;
        }

        //! car replace this if really required
        protected virtual double optionletPrice(Option.Type optionType, double effStrike)
        {

            var fixingDate = coupon_.fixingDate();
            if (fixingDate <= Settings.evaluationDate())
            {
                // the amount is determined
                double a, b;
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
                return System.Math.Max(a - b, 0.0) * coupon_.accrualPeriod() * discount_;

            }
            else
            {
                // not yet determined, use Black/DD1/Bachelier/whatever from Impl
                Utils.QL_REQUIRE(!capletVolatility().empty(), () => "missing optionlet volatility");

                var stdDev = System.Math.Sqrt(capletVolatility().link.totalVariance(fixingDate, effStrike));

                var fixing = optionletPriceImp(optionType,
                    effStrike,
                    adjustedFixing(),
                    stdDev);
                return fixing * coupon_.accrualPeriod() * discount_;

            }
        }

        //! usually only need implement this (of course they may need
        //! to re-implement initialize too ...)
        protected virtual double optionletPriceImp(Option.Type t, double strike,
            double forward, double stdDev)
        {
            Utils.QL_FAIL("you must implement this to get a vol-dependent price");
            return 0;
        }

        protected virtual double adjustedFixing() => adjustedFixing(null);

        protected virtual double adjustedFixing(double? fixing)
        {
            if (fixing == null)
                fixing = coupon_.indexFixing();

            // no adjustment
            return fixing.Value;
        }

        //! data
        Handle<YoYOptionletVolatilitySurface> capletVol_;
        YoYInflationCoupon coupon_;
        double gearing_;
        double spread_;
        double discount_;
        double spreadLegValue_;
    }
}