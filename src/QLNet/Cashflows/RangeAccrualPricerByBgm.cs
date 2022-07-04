using System.Collections.Generic;
using System.Linq;
using QLNet.Math.Distributions;
using QLNet.Termstructures.Volatility;

namespace QLNet.Cashflows
{
    [JetBrains.Annotations.PublicAPI] public class RangeAccrualPricerByBgm : RangeAccrualPricer
    {
        public RangeAccrualPricerByBgm(double correlation,
            SmileSection smilesOnExpiry,
            SmileSection smilesOnPayment,
            bool withSmile,
            bool byCallSpread)
        {
            correlation_ = correlation;
            withSmile_ = withSmile;
            byCallSpread_ = byCallSpread;
            smilesOnExpiry_ = smilesOnExpiry;
            smilesOnPayment_ = smilesOnPayment;
            eps_ = 1.0e-8;
        }
        // Observer interface
        public override double swapletPrice()
        {
            var result = 0.0;
            var deflator = discount_ * initialValues_[0];
            for (var i = 0; i < observationsNo_; i++)
            {
                var digitalFloater = digitalRangePrice(lowerTrigger_, upperTrigger_, initialValues_[i + 1],
                    observationTimes_[i], deflator);
                result += digitalFloater;
            }
            return gearing_ * (result * accrualFactor_ / observationsNo_) + spreadLegValue_;
        }

        protected double drift(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing =
                p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
                * (p * lambdaT * lambdaT + q * lambdaS * lambdaT * correlation) +
                q * lambdaS * lambdaS + p * lambdaS * lambdaT * correlation;
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5) * lambdaT * lambdaT;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }
        protected double derDriftDerLambdaS(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing = p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
                * (q * lambdaT * correlation) + 2 * q * lambdaS + p * lambdaT * correlation;
            var driftAfterFixing = 0.0;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }
        protected double derDriftDerLambdaT(double U, double lambdaS, double lambdaT, double correlation)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing = p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_)
                * (2 * p * lambdaT + q * lambdaS * correlation) + +p * lambdaS * correlation;
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5)
                                   * 2 * lambdaT;

            return startTime_ > 0 ? driftBeforeFixing : driftAfterFixing;
        }

        protected double lambda(double U, double lambdaS, double lambdaT)
        {
            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;

            return startTime_ > 0 ? q * lambdaS + p * lambdaT : lambdaT;
        }
        protected double derLambdaDerLambdaS(double U) => startTime_ > 0 ? (endTime_ - U) / accrualFactor_ : 0.0;

        protected double derLambdaDerLambdaT(double U) => startTime_ > 0 ? (U - startTime_) / accrualFactor_ : 0.0;

        protected List<double> driftsOverPeriod(double U, double lambdaS, double lambdaT, double correlation)
        {
            var result = new List<double>();

            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;
            var L0T = initialValues_.Last();

            var driftBeforeFixing =
                p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) * (p * lambdaT * lambdaT + q * lambdaS * lambdaT * correlation) +
                q * lambdaS * lambdaS + p * lambdaS * lambdaT * correlation
                - 0.5 * lambda(U, lambdaS, lambdaT) * lambda(U, lambdaS, lambdaT);
            var driftAfterFixing = (p * accrualFactor_ * L0T / (1.0 + L0T * accrualFactor_) - 0.5) * lambdaT * lambdaT;

            result.Add(driftBeforeFixing);
            result.Add(driftAfterFixing);

            return result;
        }
        protected List<double> lambdasOverPeriod(double U, double lambdaS, double lambdaT)
        {
            var result = new List<double>();

            var p = (U - startTime_) / accrualFactor_;
            var q = (endTime_ - U) / accrualFactor_;

            var lambdaBeforeFixing = q * lambdaS + p * lambdaT;
            var lambdaAfterFixing = lambdaT;

            result.Add(lambdaBeforeFixing);
            result.Add(lambdaAfterFixing);

            return result;
        }

        protected double digitalRangePrice(double lowerTrigger, double upperTrigger, double initialValue, double expiry,
            double deflator)
        {
            var lowerPrice = digitalPrice(lowerTrigger, initialValue, expiry, deflator);
            var upperPrice = digitalPrice(upperTrigger, initialValue, expiry, deflator);
            var result = lowerPrice - upperPrice;
            Utils.QL_REQUIRE(result > 0.0, () =>
                "RangeAccrualPricerByBgm::digitalRangePrice:\n digitalPrice(" + upperTrigger +
                "): " + upperPrice + " >  digitalPrice(" + lowerTrigger + "): " + lowerPrice);
            return result;
        }

        protected double digitalPrice(double strike, double initialValue, double expiry, double deflator)
        {
            var result = deflator;
            if (strike > eps_ / 2)
            {
                result = withSmile_
                    ? digitalPriceWithSmile(strike, initialValue, expiry, deflator)
                    : digitalPriceWithoutSmile(strike, initialValue, expiry, deflator);
            }
            return result;
        }

        protected double digitalPriceWithoutSmile(double strike, double initialValue, double expiry, double deflator)
        {
            var lambdaS = smilesOnExpiry_.volatility(strike);
            var lambdaT = smilesOnPayment_.volatility(strike);

            var lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
            var variance = startTime_ * lambdaU[0] * lambdaU[0] + (expiry - startTime_) * lambdaU[1] * lambdaU[1];

            var lambdaSATM = smilesOnExpiry_.volatility(initialValue);
            var lambdaTATM = smilesOnPayment_.volatility(initialValue);
            //drift of Lognormal process (of Libor) "a_U()" nel paper
            var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
            var adjustment = startTime_ * muU[0] + (expiry - startTime_) * muU[1];


            var d2 = (System.Math.Log(initialValue / strike) + adjustment - 0.5 * variance) / System.Math.Sqrt(variance);

            var phi = new CumulativeNormalDistribution();
            var result = deflator * phi.value(d2);

            Utils.QL_REQUIRE(result > 0.0, () =>
                "RangeAccrualPricerByBgm::digitalPriceWithoutSmile: result< 0. Result:" + result);
            Utils.QL_REQUIRE(result / deflator <= 1.0, () =>
                "RangeAccrualPricerByBgm::digitalPriceWithoutSmile: result/deflator > 1. Ratio: "
                + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }

        protected double digitalPriceWithSmile(double strike, double initialValue, double expiry, double deflator)
        {
            double result;
            if (byCallSpread_)
            {
                // Previous strike
                var previousStrike = strike - eps_ / 2;
                var lambdaS = smilesOnExpiry_.volatility(previousStrike);
                var lambdaT = smilesOnPayment_.volatility(previousStrike);

                //drift of Lognormal process (of Libor) "a_U()" nel paper
                var lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
                var previousVariance = System.Math.Max(startTime_, 0.0) * lambdaU[0] * lambdaU[0] +
                                       System.Math.Min(expiry - startTime_, expiry) * lambdaU[1] * lambdaU[1];

                var lambdaSATM = smilesOnExpiry_.volatility(initialValue);
                var lambdaTATM = smilesOnPayment_.volatility(initialValue);
                var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
                var previousAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                         System.Math.Min(expiry - startTime_, expiry) * muU[1]);
                var previousForward = initialValue * previousAdjustment;

                // Next strike
                var nextStrike = strike + eps_ / 2;
                lambdaS = smilesOnExpiry_.volatility(nextStrike);
                lambdaT = smilesOnPayment_.volatility(nextStrike);

                lambdaU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
                var nextVariance = System.Math.Max(startTime_, 0.0) * lambdaU[0] * lambdaU[0] +
                                   System.Math.Min(expiry - startTime_, expiry) * lambdaU[1] * lambdaU[1];
                //drift of Lognormal process (of Libor) "a_U()" nel paper
                muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);
                var nextAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                     System.Math.Min(expiry - startTime_, expiry) * muU[1]);
                var nextForward = initialValue * nextAdjustment;

                result = callSpreadPrice(previousForward, nextForward, previousStrike, nextStrike,
                    deflator, previousVariance, nextVariance);
            }
            else
            {
                result = digitalPriceWithoutSmile(strike, initialValue, expiry, deflator) +
                         smileCorrection(strike, initialValue, expiry, deflator);
            }

            Utils.QL_REQUIRE(result > -System.Math.Pow(eps_, .5), () =>
                "RangeAccrualPricerByBgm::digitalPriceWithSmile: result< 0 Result:" + result);
            Utils.QL_REQUIRE(result / deflator <= 1.0 + System.Math.Pow(eps_, .2), () =>
                "RangeAccrualPricerByBgm::digitalPriceWithSmile: result/deflator > 1. Ratio: "
                + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }

        protected double callSpreadPrice(double previousForward,
            double nextForward,
            double previousStrike,
            double nextStrike,
            double deflator,
            double previousVariance,
            double nextVariance)
        {
            var nextCall = Utils.blackFormula(QLNet.Option.Type.Call, nextStrike, nextForward,
                System.Math.Sqrt(nextVariance), deflator);
            var previousCall = Utils.blackFormula(QLNet.Option.Type.Call, previousStrike, previousForward,
                System.Math.Sqrt(previousVariance), deflator);

            Utils.QL_REQUIRE(nextCall < previousCall, () =>
                "RangeAccrualPricerByBgm::callSpreadPrice: nextCall > previousCall" +
                "\n nextCall: strike :" + nextStrike + "; variance: " + nextVariance +
                " adjusted initial value " + nextForward +
                "\n previousCall: strike :" + previousStrike + "; variance: " + previousVariance +
                " adjusted initial value " + previousForward);

            return (previousCall - nextCall) / (nextStrike - previousStrike);
        }

        protected double smileCorrection(double strike,
            double forward,
            double expiry,
            double deflator)
        {
            var previousStrike = strike - eps_ / 2;
            var nextStrike = strike + eps_ / 2;

            var derSmileS = (smilesOnExpiry_.volatility(nextStrike) -
                             smilesOnExpiry_.volatility(previousStrike)) / eps_;
            var derSmileT = (smilesOnPayment_.volatility(nextStrike) -
                             smilesOnPayment_.volatility(previousStrike)) / eps_;

            var lambdaS = smilesOnExpiry_.volatility(strike);
            var lambdaT = smilesOnPayment_.volatility(strike);

            var derLambdaDerK = derLambdaDerLambdaS(expiry) * derSmileS +
                                derLambdaDerLambdaT(expiry) * derSmileT;


            var lambdaSATM = smilesOnExpiry_.volatility(forward);
            var lambdaTATM = smilesOnPayment_.volatility(forward);
            var lambdasOverPeriodU = lambdasOverPeriod(expiry, lambdaS, lambdaT);
            //drift of Lognormal process (of Libor) "a_U()" nel paper
            var muU = driftsOverPeriod(expiry, lambdaSATM, lambdaTATM, correlation_);

            var variance = System.Math.Max(startTime_, 0.0) * lambdasOverPeriodU[0] * lambdasOverPeriodU[0] +
                           System.Math.Min(expiry - startTime_, expiry) * lambdasOverPeriodU[1] * lambdasOverPeriodU[1];

            var forwardAdjustment = System.Math.Exp(System.Math.Max(startTime_, 0.0) * muU[0] +
                                                    System.Math.Min(expiry - startTime_, expiry) * muU[1]);
            var forwardAdjusted = forward * forwardAdjustment;

            var d1 = (System.Math.Log(forwardAdjusted / strike) + 0.5 * variance) / System.Math.Sqrt(variance);

            var sqrtOfTimeToExpiry = (System.Math.Max(startTime_, 0.0) * lambdasOverPeriodU[0] +
                                      System.Math.Min(expiry - startTime_, expiry) * lambdasOverPeriodU[1]) * (1.0 / System.Math.Sqrt(variance));

            var phi = new CumulativeNormalDistribution();
            var psi = new NormalDistribution();
            var result = -forwardAdjusted * psi.value(d1) * sqrtOfTimeToExpiry * derLambdaDerK;

            result *= deflator;

            Utils.QL_REQUIRE(System.Math.Abs(result / deflator) <= 1.0 + System.Math.Pow(eps_, .2), () =>
                "RangeAccrualPricerByBgm::smileCorrection: abs(result/deflator) > 1. Ratio: "
                + result / deflator + " result: " + result + " deflator: " + deflator);

            return result;
        }


        private double correlation_;   // correlation between L(S) and L(T)
        private bool withSmile_;
        private bool byCallSpread_;

        private SmileSection smilesOnExpiry_;
        private SmileSection smilesOnPayment_;
        private double eps_;
    }
}