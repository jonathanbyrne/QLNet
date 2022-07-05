using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace QLNet.Math.statistics
{
    [PublicAPI]
    public interface IGeneralStatistics
    {
        void add
            (double value, double weight);

        void addSequence(List<double> data, List<double> weight);

        double errorEstimate();

        KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange);

        double kurtosis();

        double max();

        double mean();

        double min();

        double percentile(double percent);

        void reset();

        int samples();

        double skewness();

        double standardDeviation();

        double variance();

        double weightSum();
    }
}
