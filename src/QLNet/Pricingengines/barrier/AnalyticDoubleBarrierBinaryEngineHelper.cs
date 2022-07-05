using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.processes;

namespace QLNet.Pricingengines.barrier
{
    [PublicAPI]
    public class AnalyticDoubleBarrierBinaryEngineHelper
    {
        protected DoubleBarrierOption.Arguments arguments_;
        protected CashOrNothingPayoff payoff_;
        protected GeneralizedBlackScholesProcess process_;

        public AnalyticDoubleBarrierBinaryEngineHelper(
            GeneralizedBlackScholesProcess process,
            CashOrNothingPayoff payoff,
            DoubleBarrierOption.Arguments arguments)
        {
            process_ = process;
            payoff_ = payoff;
            arguments_ = arguments;
        }

        // helper object methods
        public double payoffAtExpiry(double spot, double variance,
            DoubleBarrier.Type barrierType,
            int maxIteration = 100,
            double requiredConvergence = 1e-8)
        {
            Utils.QL_REQUIRE(spot > 0.0,
                () => "positive spot value required");

            Utils.QL_REQUIRE(variance >= 0.0,
                () => "negative variance not allowed");

            var residualTime = process_.time(arguments_.exercise.lastDate());
            Utils.QL_REQUIRE(residualTime > 0.0,
                () => "expiration time must be > 0");

            // Option::Type ExerciseType   = payoff_->optionType(); // this is not used ?
            var cash = payoff_.cashPayoff();
            var barrier_lo = arguments_.barrier_lo.Value;
            var barrier_hi = arguments_.barrier_hi.Value;

            var sigmaq = variance / residualTime;
            var r = process_.riskFreeRate().currentLink().zeroRate(residualTime, Compounding.Continuous,
                Frequency.NoFrequency).rate();
            var q = process_.dividendYield().currentLink().zeroRate(residualTime,
                Compounding.Continuous, Frequency.NoFrequency).rate();
            var b = r - q;

            var alpha = -0.5 * (2 * b / sigmaq - 1);
            var beta = -0.25 * System.Math.Pow(2 * b / sigmaq - 1, 2) - 2 * r / sigmaq;
            var Z = System.Math.Log(barrier_hi / barrier_lo);
            var factor = 2 * Const.M_PI * cash / System.Math.Pow(Z, 2); // common factor
            var lo_alpha = System.Math.Pow(spot / barrier_lo, alpha);
            var hi_alpha = System.Math.Pow(spot / barrier_hi, alpha);

            double tot = 0, term = 0;
            for (var i = 1; i < maxIteration; ++i)
            {
                var term1 = (lo_alpha - System.Math.Pow(-1.0, i) * hi_alpha) /
                            (System.Math.Pow(alpha, 2) + System.Math.Pow(i * Const.M_PI / Z, 2));
                var term2 = System.Math.Sin(i * Const.M_PI / Z * System.Math.Log(spot / barrier_lo));
                var term3 = System.Math.Exp(-0.5 * (System.Math.Pow(i * Const.M_PI / Z, 2) - beta) * variance);
                term = factor * i * term1 * term2 * term3;
                tot += term;
            }

            // Check if convergence is sufficiently fast (for extreme parameters with big alpha the convergence can be very
            // poor, see for example Hui "One-touch double barrier binary option value")
            Utils.QL_REQUIRE(System.Math.Abs(term) < requiredConvergence, () => "serie did not converge sufficiently fast");

            if (barrierType == DoubleBarrier.Type.KnockOut)
            {
                return System.Math.Max(tot, 0.0); // KO
            }

            var discount = process_.riskFreeRate().currentLink().discount(
                arguments_.exercise.lastDate());
            Utils.QL_REQUIRE(discount > 0.0,
                () => "positive discount required");
            return System.Math.Max(cash * discount - tot, 0.0); // KI
        }

        // helper object methods
        public double payoffKIKO(double spot, double variance,
            DoubleBarrier.Type barrierType,
            int maxIteration = 1000,
            double requiredConvergence = 1e-8)
        {
            Utils.QL_REQUIRE(spot > 0.0,
                () => "positive spot value required");

            Utils.QL_REQUIRE(variance >= 0.0,
                () => "negative variance not allowed");

            var residualTime = process_.time(arguments_.exercise.lastDate());
            Utils.QL_REQUIRE(residualTime > 0.0,
                () => "expiration time must be > 0");

            var cash = payoff_.cashPayoff();
            var barrier_lo = arguments_.barrier_lo.Value;
            var barrier_hi = arguments_.barrier_hi.Value;
            if (barrierType == DoubleBarrier.Type.KOKI)
            {
                Utils.swap(ref barrier_lo, ref barrier_hi);
            }

            var sigmaq = variance / residualTime;
            var r = process_.riskFreeRate().currentLink().zeroRate(residualTime, Compounding.Continuous,
                Frequency.NoFrequency).rate();
            var q = process_.dividendYield().currentLink().zeroRate(residualTime,
                Compounding.Continuous, Frequency.NoFrequency).rate();
            var b = r - q;

            var alpha = -0.5 * (2 * b / sigmaq - 1);
            var beta = -0.25 * System.Math.Pow(2 * b / sigmaq - 1, 2) - 2 * r / sigmaq;
            var Z = System.Math.Log(barrier_hi / barrier_lo);
            var log_S_L = System.Math.Log(spot / barrier_lo);

            double tot = 0, term = 0;
            for (var i = 1; i < maxIteration; ++i)
            {
                var factor = System.Math.Pow(i * Const.M_PI / Z, 2) - beta;
                var term1 = (beta - System.Math.Pow(i * Const.M_PI / Z, 2) * System.Math.Exp(-0.5 * factor * variance)) / factor;
                var term2 = System.Math.Sin(i * Const.M_PI / Z * log_S_L);
                term = 2.0 / (i * Const.M_PI) * term1 * term2;
                tot += term;
            }

            tot += 1 - log_S_L / Z;
            tot *= cash * System.Math.Pow(spot / barrier_lo, alpha);

            // Check if convergence is sufficiently fast
            Utils.QL_REQUIRE(System.Math.Abs(term) < requiredConvergence, () => "serie did not converge sufficiently fast");

            return System.Math.Max(tot, 0.0);
        }
    }
}
