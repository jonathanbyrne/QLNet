using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math.Distributions;
using QLNet.Patterns;

namespace QLNet.Math.statistics
{
    [PublicAPI]
    public class GenericGaussianStatistics<Stat> : IGeneralStatistics where Stat : IGeneralStatistics, new()
    {
        public GenericGaussianStatistics()
        {
        }

        public GenericGaussianStatistics(Stat s)
        {
            impl_ = s;
        }

        //! gaussian-assumption Average Shortfall (averaged shortfallness)
        public double gaussianAverageShortfall(double target)
        {
            var m = mean();
            var std = standardDeviation();
            var gIntegral = new CumulativeNormalDistribution(m, std);
            var g = new NormalDistribution(m, std);
            return target - m + std * std * g.value(target) / gIntegral.value(target);
        }

        /*! returns the downside deviation, defined as the square root of the downside variance. */
        public double gaussianDownsideDeviation() => System.Math.Sqrt(gaussianDownsideVariance());

        // Gaussian risk measures
        /*! returns the downside variance
        */
        public double gaussianDownsideVariance() => gaussianRegret(0.0);

        //! gaussian-assumption Expected Shortfall at a given percentile
        /*! Assuming a gaussian distribution it
            returns the expected loss in case that the loss exceeded
            a VaR threshold,

            that is the average of observations below the
            given percentile \f$ p \f$.
            Also know as conditional value-at-risk.

            See Artzner, Delbaen, Eber and Heath,
            "Coherent measures of risk", Mathematical Finance 9 (1999)
        */
        public double gaussianExpectedShortfall(double percentile)
        {
            QLNet.Utils.QL_REQUIRE(percentile < 1.0 && percentile >= 0.9, () => "percentile (" + percentile + ") out of range [0.9, 1)");

            var m = mean();
            var std = standardDeviation();
            var gInverse = new InverseCumulativeNormal(m, std);
            var var = gInverse.value(1.0 - percentile);
            var g = new NormalDistribution(m, std);
            var result = m - std * std * g.value(var) / (1.0 - percentile);
            // expectedShortfall must be a loss
            // this means that it has to be MIN(result, 0.0)
            // expectedShortfall must also be a positive quantity, so -MIN(*)
            return -System.Math.Min(result, 0.0);
        }

        /*! gaussian-assumption y-th percentile
        */
        /*! \pre percentile must be in range (0%-100%) extremes excluded */
        public double gaussianPercentile(double percentile)
        {
            QLNet.Utils.QL_REQUIRE(percentile > 0.0 && percentile < 1.0, () => "percentile (" + percentile + ") must be in (0.0, 1.0)");

            var gInverse = new InverseCumulativeNormal(mean(), standardDeviation());
            return gInverse.value(percentile);
        }

        //! gaussian-assumption Potential-Upside at a given percentile
        public double gaussianPotentialUpside(double percentile)
        {
            QLNet.Utils.QL_REQUIRE(percentile < 1.0 && percentile >= 0.9, () => "percentile (" + percentile + ") out of range [0.9, 1)");

            var result = gaussianPercentile(percentile);
            // potential upside must be a gain, i.e., floored at 0.0
            return System.Math.Max(result, 0.0);
        }

        /*! returns the variance of observations below target
            See Dembo, Freeman "The Rules Of Risk", Wiley (2001)
        */
        public double gaussianRegret(double target)
        {
            var m = mean();
            var std = standardDeviation();
            var variance = std * std;
            var gIntegral = new CumulativeNormalDistribution(m, std);
            var g = new NormalDistribution(m, std);
            var firstTerm = variance + m * m - 2.0 * target * m + target * target;
            var alfa = gIntegral.value(target);
            var secondTerm = m - target;
            var beta = variance * g.value(target);
            var result = alfa * firstTerm - beta * secondTerm;
            return result / alfa;
        }

        //! gaussian-assumption Shortfall (observations below target)
        public double gaussianShortfall(double target)
        {
            var gIntegral = new CumulativeNormalDistribution(mean(), standardDeviation());
            return gIntegral.value(target);
        }

        public double gaussianTopPercentile(double percentile) => gaussianPercentile(1.0 - percentile);

        //! gaussian-assumption Value-At-Risk at a given percentile
        public double gaussianValueAtRisk(double percentile)
        {
            QLNet.Utils.QL_REQUIRE(percentile < 1.0 && percentile >= 0.9, () => "percentile (" + percentile + ") out of range [0.9, 1)");

            var result = gaussianPercentile(1.0 - percentile);
            // VAR must be a loss
            // this means that it has to be MIN(dist(1.0-percentile), 0.0)
            // VAR must also be a positive quantity, so -MIN(*)
            return -System.Math.Min(result, 0.0);
        }

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

        public void reset()
        {
            impl_.reset();
        }

        public void add
            (double value, double weight)
        {
            impl_.add(value, weight);
        }

        public void addSequence(List<double> data, List<double> weight)
        {
            impl_.addSequence(data, weight);
        }

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange) =>
            impl_.expectationValue(f, inRange);

        #endregion
    }
}
