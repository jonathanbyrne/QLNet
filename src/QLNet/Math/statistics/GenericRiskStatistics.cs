using System;
using System.Collections.Generic;
using QLNet.Patterns;

namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public class GenericRiskStatistics<Stat> : IGeneralStatistics where Stat : IGeneralStatistics, new()
    {

        #region wrap-up Stat
        protected Stat impl_ = FastActivator<Stat>.Create();

        public int samples() => impl_.samples();

        public double mean() => impl_.mean();

        public double min() => impl_.min();

        public double max() => impl_.max();

        public double standardDeviation() => impl_.standardDeviation();

        public double variance() => impl_.variance();

        public double skewness() => impl_.skewness();

        public double kurtosis() => impl_.kurtosis();

        public double percentile(double percent) => impl_.percentile(percent);

        public double weightSum() => impl_.weightSum();

        public double errorEstimate() => impl_.errorEstimate();

        public void reset() { impl_.reset(); }
        public void add
            (double value, double weight)
        { impl_.add(value, weight); }
        public void addSequence(List<double> data, List<double> weight) { impl_.addSequence(data, weight); }

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange) =>
            impl_.expectationValue(f, inRange);

        #endregion


        /*! returns the variance of observations below the mean,
            See Markowitz (1959).
        */
        public double semiVariance() => regret(mean());

        /*! returns the semi deviation, defined as the square root of the semi variance. */
        public double semiDeviation() => System.Math.Sqrt(semiVariance());

        // returns the variance of observations below 0.0,
        public double downsideVariance() => regret(0.0);

        /*! returns the downside deviation, defined as the square root of the downside variance. */
        public double downsideDeviation() => System.Math.Sqrt(downsideVariance());

        /*! returns the variance of observations below target,
            See Dembo and Freeman, "The Rules Of Risk", Wiley (2001).
        */
        public double regret(double target)
        {
            // average over the range below the target
            var result = expectationValue(z => System.Math.Pow(z.Key - target, 2),
                z => z.Key < target);
            var x = result.Key;
            var N = result.Value;
            Utils.QL_REQUIRE(N > 1, () => "samples under target <= 1, unsufficient");
            return N / (N - 1.0) * x;
        }

        //! potential upside (the reciprocal of VAR) at a given percentile
        public double potentialUpside(double centile)
        {
            Utils.QL_REQUIRE(centile < 1.0 && centile >= 0.9, () => "percentile (" + centile + ") out of range [0.9, 1)");

            // potential upside must be a gain, i.e., floored at 0.0
            return System.Math.Max(percentile(centile), 0.0);
        }

        //! value-at-risk at a given percentile
        public double valueAtRisk(double centile)
        {
            Utils.QL_REQUIRE(centile < 1.0 && centile >= 0.9, () => "percentile (" + centile + ") out of range [0.9, 1)");

            // must be a loss, i.e., capped at 0.0 and negated
            return -System.Math.Min(percentile(1.0 - centile), 0.0);
        }

        //! expected shortfall at a given percentile
        /*! returns the expected loss in case that the loss exceeded
            a VaR threshold,

            that is the average of observations below the
            given percentile \f$ p \f$.
            Also know as conditional value-at-risk.

            See Artzner, Delbaen, Eber and Heath,
            "Coherent measures of risk", Mathematical Finance 9 (1999)
        */
        public double expectedShortfall(double centile)
        {
            Utils.QL_REQUIRE(centile < 1.0 && centile >= 0.9, () => "percentile (" + centile + ") out of range [0.9, 1)");

            Utils.QL_REQUIRE(samples() != 0, () => "empty sample set");

            var target = -valueAtRisk(centile);
            var result = expectationValue(z => z.Key, z => z.Key < target);
            var x = result.Key;
            var N = result.Value;
            Utils.QL_REQUIRE(N != 0, () => "no data below the target");
            // must be a loss, i.e., capped at 0.0 and negated
            return -System.Math.Min(x, 0.0);
        }

        // probability of missing the given target
        public double shortfall(double target)
        {
            Utils.QL_REQUIRE(samples() != 0, () => "empty sample set");
            return expectationValue(x => x.Key < target ? 1 : 0, x => true).Key;
        }

        // averaged shortfallness
        public double averageShortfall(double target)
        {
            var result = expectationValue(z => target - z.Key, z => z.Key < target);
            var x = result.Key;
            var N = result.Value;
            Utils.QL_REQUIRE(N != 0, () => "no data below the target");
            return x;
        }
    }
}