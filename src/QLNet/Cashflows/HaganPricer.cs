using System;
using QLNet.Quotes;
using QLNet.Termstructures;
using QLNet.Termstructures.Volatility.swaption;
using QLNet.Time;

namespace QLNet.Cashflows
{
    public abstract class HaganPricer : CmsCouponPricer
    {
        protected double annuity_;
        protected CmsCoupon coupon_;
        protected double cutoffForCaplet_;
        protected double cutoffForFloorlet_;
        protected double discount_;
        protected Date fixingDate_;
        protected double gearing_;
        protected GFunction gFunction_;
        protected Handle<Quote> meanReversion_;
        protected GFunctionFactory.YieldCurveModel modelOfYieldCurve_;
        protected Date paymentDate_;
        protected YieldTermStructure rateCurve_;
        protected double spread_;
        protected double spreadLegValue_;
        protected double swapRateValue_;
        protected Period swapTenor_;
        protected VanillaOptionPricer vanillaOptionPricer_;

        protected HaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol, GFunctionFactory.YieldCurveModel modelOfYieldCurve, Handle<Quote> meanReversion)
            : base(swaptionVol)
        {
            modelOfYieldCurve_ = modelOfYieldCurve;
            cutoffForCaplet_ = 2;
            cutoffForFloorlet_ = 0;
            meanReversion_ = meanReversion;

            if (meanReversion_.link != null)
            {
                meanReversion_.registerWith(update);
            }
        }

        public override double capletPrice(double effectiveCap)
        {
            // caplet is equivalent to call option on fixing
            var today = Settings.evaluationDate();
            if (fixingDate_ <= today)
            {
                // the fixing is determined
                var Rs = System.Math.Max(coupon_.swapIndex().fixing(fixingDate_) - effectiveCap, 0.0);
                var price = gearing_ * Rs * (coupon_.accrualPeriod() * discount_);
                return price;
            }

            var cutoffNearZero = 1e-10;
            double capletPrice = 0;
            if (effectiveCap < cutoffForCaplet_)
            {
                var effectiveStrikeForMax = System.Math.Max(effectiveCap, cutoffNearZero);
                capletPrice = optionletPrice(Option.Type.Call, effectiveStrikeForMax);
            }

            return gearing_ * capletPrice;
        }

        public override double capletRate(double effectiveCap) => capletPrice(effectiveCap) / (coupon_.accrualPeriod() * discount_);

        public override double floorletPrice(double effectiveFloor)
        {
            // floorlet is equivalent to put option on fixing
            var today = Settings.evaluationDate();
            if (fixingDate_ <= today)
            {
                // the fixing is determined
                var Rs = System.Math.Max(effectiveFloor - coupon_.swapIndex().fixing(fixingDate_), 0.0);
                var price = gearing_ * Rs * (coupon_.accrualPeriod() * discount_);
                return price;
            }

            var cutoffNearZero = 1e-10;
            double floorletPrice = 0;
            if (effectiveFloor > cutoffForFloorlet_)
            {
                var effectiveStrikeForMin = System.Math.Max(effectiveFloor, cutoffNearZero);
                floorletPrice = optionletPrice(Option.Type.Put, effectiveStrikeForMin);
            }

            return gearing_ * floorletPrice;
        }

        public override double floorletRate(double effectiveFloor) => floorletPrice(effectiveFloor) / (coupon_.accrualPeriod() * discount_);

        public override void initialize(FloatingRateCoupon coupon)
        {
            coupon_ = coupon as CmsCoupon;
            Utils.QL_REQUIRE(coupon_ != null, () => "CMS coupon needed");
            gearing_ = coupon_.gearing();
            spread_ = coupon_.spread();

            fixingDate_ = coupon_.fixingDate();
            paymentDate_ = coupon_.date();
            var swapIndex = coupon_.swapIndex();
            rateCurve_ = swapIndex.forwardingTermStructure().link;

            var today = Settings.evaluationDate();

            if (paymentDate_ > today)
            {
                discount_ = rateCurve_.discount(paymentDate_);
            }
            else
            {
                discount_ = 1.0;
            }

            spreadLegValue_ = spread_ * coupon_.accrualPeriod() * discount_;

            if (fixingDate_ > today)
            {
                swapTenor_ = swapIndex.tenor();
                var swap = swapIndex.underlyingSwap(fixingDate_);

                swapRateValue_ = swap.fairRate();

                annuity_ = System.Math.Abs(swap.fixedLegBPS() / Const.BASIS_POINT);

                var q = (int)swapIndex.fixedLegTenor().frequency();
                var schedule = swap.fixedSchedule();
                var dc = swapIndex.dayCounter();
                var startTime = dc.yearFraction(rateCurve_.referenceDate(), swap.startDate());
                var swapFirstPaymentTime = dc.yearFraction(rateCurve_.referenceDate(), schedule.date(1));
                var paymentTime = dc.yearFraction(rateCurve_.referenceDate(), paymentDate_);
                var delta = (paymentTime - startTime) / (swapFirstPaymentTime - startTime);

                switch (modelOfYieldCurve_)
                {
                    case GFunctionFactory.YieldCurveModel.Standard:
                        gFunction_ = GFunctionFactory.newGFunctionStandard(q, delta, swapTenor_.length());
                        break;
                    case GFunctionFactory.YieldCurveModel.ExactYield:
                        gFunction_ = GFunctionFactory.newGFunctionExactYield(coupon_);
                        break;
                    case GFunctionFactory.YieldCurveModel.ParallelShifts:
                    {
                        var nullMeanReversionQuote = new Handle<Quote>(new SimpleQuote(0.0));
                        gFunction_ = GFunctionFactory.newGFunctionWithShifts(coupon_, nullMeanReversionQuote);
                    }
                        break;
                    case GFunctionFactory.YieldCurveModel.NonParallelShifts:
                        gFunction_ = GFunctionFactory.newGFunctionWithShifts(coupon_, meanReversion_);
                        break;
                    default:
                        Utils.QL_FAIL("unknown/illegal gFunction ExerciseType");
                        break;
                }

                vanillaOptionPricer_ = new BlackVanillaOptionPricer(swapRateValue_, fixingDate_, swapTenor_, swaptionVolatility().link);
            }
        }

        //
        public double meanReversion() => meanReversion_.link.value();

        public void setMeanReversion(Handle<Quote> meanReversion)
        {
            if (meanReversion_ != null)
            {
                meanReversion_.unregisterWith(update);
            }

            meanReversion_ = meanReversion;
            if (meanReversion_ != null)
            {
                meanReversion_.registerWith(update);
            }

            update();
        }

        public override double swapletRate() => swapletPrice() / (coupon_.accrualPeriod() * discount_);

        protected virtual double optionletPrice(Option.Type optionType, double strike) => throw new NotImplementedException();
    }
}
