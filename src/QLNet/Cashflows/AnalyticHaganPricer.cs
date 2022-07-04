using QLNet.Math.Distributions;
using QLNet.Quotes;
using QLNet.Termstructures.Volatility.swaption;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class AnalyticHaganPricer : HaganPricer
    {
        public AnalyticHaganPricer(Handle<SwaptionVolatilityStructure> swaptionVol, GFunctionFactory.YieldCurveModel modelOfYieldCurve, Handle<Quote> meanReversion)
            : base(swaptionVol, modelOfYieldCurve, meanReversion)
        {
        }

        //Hagan, 3.5b, 3.5c
        protected override double optionletPrice(Option.Type optionType, double strike)
        {
            var variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
            var firstDerivativeOfGAtForwardValue = gFunction_.firstDerivative(swapRateValue_);
            double price = 0;

            var CK = vanillaOptionPricer_.value(strike, optionType, annuity_);
            price += discount_ / annuity_ * CK;
            var sqrtSigma2T = System.Math.Sqrt(variance);
            var lnRoverK = System.Math.Log(swapRateValue_ / strike);
            var d32 = (lnRoverK + 1.5 * variance) / sqrtSigma2T;
            var d12 = (lnRoverK + .5 * variance) / sqrtSigma2T;
            var dminus12 = (lnRoverK - .5 * variance) / sqrtSigma2T;

            var cumulativeOfNormal = new CumulativeNormalDistribution();
            var N32 = cumulativeOfNormal.value((int)optionType * d32);
            var N12 = cumulativeOfNormal.value((int)optionType * d12);
            var Nminus12 = cumulativeOfNormal.value((int)optionType * dminus12);

            price += (int)optionType * firstDerivativeOfGAtForwardValue * annuity_ * swapRateValue_ * (swapRateValue_ * System.Math.Exp(variance) * N32 - (swapRateValue_ + strike) * N12 + strike * Nminus12);
            price *= coupon_.accrualPeriod();
            return price;
        }

        //Hagan 3.4c
        public override double swapletPrice()
        {
            var today = Settings.evaluationDate();
            if (fixingDate_ <= today)
            {
                // the fixing is determined
                var Rs = coupon_.swapIndex().fixing(fixingDate_);
                var price = (gearing_ * Rs + spread_) * (coupon_.accrualPeriod() * discount_);
                return price;
            }
            else
            {
                var variance = swaptionVolatility().link.blackVariance(fixingDate_, swapTenor_, swapRateValue_);
                var firstDerivativeOfGAtForwardValue = gFunction_.firstDerivative(swapRateValue_);
                double price = 0;
                price += discount_ * swapRateValue_;
                price += firstDerivativeOfGAtForwardValue * annuity_ * swapRateValue_ * swapRateValue_ * (System.Math.Exp(variance) - 1.0);
                return gearing_ * price * coupon_.accrualPeriod() + spreadLegValue_;
            }
        }
    }
}