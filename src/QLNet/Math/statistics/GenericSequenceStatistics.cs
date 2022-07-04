using System.Collections.Generic;
using QLNet.Extensions;

namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public class GenericSequenceStatistics<S> where S : IGeneralStatistics, new()
    {
        protected int dimension_;
        public int size() => dimension_;

        protected List<S> stats_;
        protected List<double> results_;
        protected Matrix quadraticSum_;

        public GenericSequenceStatistics(int dimension)
        {
            dimension_ = 0;
            reset(dimension);
        }

        //! returns the covariance Matrix
        public Matrix covariance()
        {
            var sampleWeight = weightSum();
            Utils.QL_REQUIRE(sampleWeight > 0.0, () => "sampleWeight=0, unsufficient");

            double sampleNumber = samples();
            Utils.QL_REQUIRE(sampleNumber > 1.0, () => "sample number <=1, unsufficient");

            var m = mean();
            var inv = 1.0 / sampleWeight;

            var result = inv * quadraticSum_;
            result -= Matrix.outerProduct(m, m);

            result *= sampleNumber / (sampleNumber - 1.0);
            return result;
        }
        //! returns the correlation Matrix
        public Matrix correlation()
        {
            var correlation = covariance();
            var variances = correlation.diagonal();
            for (var i = 0; i < dimension_; i++)
            {
                for (var j = 0; j < dimension_; j++)
                {
                    if (i == j)
                    {
                        if (variances[i].IsEqual(0.0))
                        {
                            correlation[i, j] = 1.0;
                        }
                        else
                        {
                            correlation[i, j] *= 1.0 / System.Math.Sqrt(variances[i] * variances[j]);
                        }
                    }
                    else
                    {
                        if (variances[i].IsEqual(0.0) && variances[j].IsEqual(0.0))
                        {
                            correlation[i, j] = 1.0;
                        }
                        else if (variances[i].IsEqual(0.0) || variances[j].IsEqual(0.0))
                        {
                            correlation[i, j] = 0.0;
                        }
                        else
                        {
                            correlation[i, j] *= 1.0 / System.Math.Sqrt(variances[i] * variances[j]);
                        }
                    }
                } // j for
            } // i for
            return correlation;
        }

        // 1-D inspectors lifted from underlying statistics class
        public int samples() => stats_.Count == 0 ? 0 : stats_[0].samples();

        public double weightSum() => stats_.Count == 0 ? 0.0 : stats_[0].weightSum();

        // N-D inspectors lifted from underlying statistics class
        // no argument list
        private List<double> noArg(string method)
        {
            // do not check for null - in this case we throw anyways
            for (var i = 0; i < dimension_; i++)
            {
                var methodInfo = Utils.GetMethodInfo(stats_[i], method);
                results_[i] = (double)methodInfo.Invoke(stats_[i], new object[] { });
            }
            return results_;
        }
        // single argument list
        private List<double> singleArg(double x, string method)
        {
            // do not check for null - in this case we throw anyways
            for (var i = 0; i < dimension_; i++)
            {
                var methodInfo = Utils.GetMethodInfo(stats_[i], method);
                results_[i] = (double)methodInfo.Invoke(stats_[i], new object[] { x });
            }
            return results_;
        }

        // void argument list
        public List<double> mean()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].mean();
            return results_;
        }
        public List<double> variance()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].variance();
            return results_;
        }
        public List<double> standardDeviation()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].standardDeviation();
            return results_;
        }
        public List<double> downsideVariance() => noArg("downsideVariance");

        public List<double> downsideDeviation() => noArg("downsideDeviation");

        public List<double> semiVariance() => noArg("semiVariance");

        public List<double> semiDeviation() => noArg("semiDeviation");

        public List<double> errorEstimate() => noArg("errorEstimate");

        public List<double> skewness()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].skewness();
            return results_;
        }
        public List<double> kurtosis()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].kurtosis();
            return results_;
        }
        public List<double> min()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].min();
            return results_;
        }
        public List<double> max()
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].max();
            return results_;
        }

        // single argument list
        public List<double> gaussianPercentile(double x) => singleArg(x, "gaussianPercentile");

        public List<double> percentile(double x)
        {
            for (var i = 0; i < dimension_; i++)
                results_[i] = stats_[i].percentile(x);
            return results_;
        }
        public List<double> gaussianPotentialUpside(double x) => singleArg(x, "gaussianPotentialUpside");

        public List<double> potentialUpside(double x) => singleArg(x, "potentialUpside");

        public List<double> gaussianValueAtRisk(double x) => singleArg(x, "gaussianValueAtRisk");

        public List<double> valueAtRisk(double x) => singleArg(x, "valueAtRisk");

        public List<double> gaussianExpectedShortfall(double x) => singleArg(x, "gaussianExpectedShortfall");

        public List<double> expectedShortfall(double x) => singleArg(x, "expectedShortfall");

        public List<double> gaussianShortfall(double x) => singleArg(x, "gaussianShortfall");

        public List<double> shortfall(double x) => singleArg(x, "shortfall");

        public List<double> gaussianAverageShortfall(double x) => singleArg(x, "gaussianAverageShortfall");

        public List<double> averageShortfall(double x) => singleArg(x, "averageShortfall");

        public List<double> regret(double x) => singleArg(x, "regret");

        // Modifiers
        public virtual void reset(int dimension)
        {
            // (re-)initialize
            if (dimension > 0)
            {
                if (dimension == dimension_)
                {
                    for (var i = 0; i < dimension_; ++i)
                        stats_[i].reset();
                }
                else
                {
                    dimension_ = dimension;
                    stats_ = new InitializedList<S>(dimension);
                    results_ = new InitializedList<double>(dimension);
                }
                quadraticSum_ = new Matrix(dimension_, dimension_, 0.0);
            }
            else
            {
                dimension_ = dimension;
            }
        }

        public virtual void add
            (List<double> begin)
        { add(begin, 1); }

        public virtual void add
            (List<double> begin, double weight)
        {
            if (dimension_ == 0)
            {
                // stat wasn't initialized yet
                var dimension = begin.Count;
                Utils.QL_REQUIRE(dimension > 0, () => "sample error: end<=begin");
                reset(dimension);
            }

            Utils.QL_REQUIRE(begin.Count == dimension_, () =>
                "sample size mismatch: " + dimension_ + " required, " + begin.Count + " provided");

            quadraticSum_ += weight * Matrix.outerProduct(begin, begin);

            for (var i = 0; i < dimension_; ++i)
                stats_[i].add(begin[i], weight);
        }
    }
}