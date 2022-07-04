using System;
using System.Collections.Generic;

namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public class StatsHolder : IGeneralStatistics
    {
        private double mean_, standardDeviation_;
        public double mean() => mean_;

        public double standardDeviation() => standardDeviation_;

        public StatsHolder() { } // required for generics
        public StatsHolder(double mean, double standardDeviation)
        {
            mean_ = mean;
            standardDeviation_ = standardDeviation;
        }

        #region IGeneralStatistics
        public int samples() => throw new NotSupportedException();

        public double min() => throw new NotSupportedException();

        public double max() => throw new NotSupportedException();

        public double variance() => throw new NotSupportedException();

        public double skewness() => throw new NotSupportedException();

        public double kurtosis() => throw new NotSupportedException();

        public double percentile(double percent) => throw new NotSupportedException();

        public double weightSum() => throw new NotSupportedException();

        public double errorEstimate() => throw new NotSupportedException();

        public void reset() { throw new NotSupportedException(); }
        public void add
            (double value, double weight)
        { throw new NotSupportedException(); }
        public void addSequence(List<double> data, List<double> weight) { throw new NotSupportedException(); }

        public KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange) =>
            throw new NotSupportedException();

        #endregion
    }
}