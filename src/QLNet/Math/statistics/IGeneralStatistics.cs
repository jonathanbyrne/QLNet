using System;
using System.Collections.Generic;

namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public interface IGeneralStatistics
    {
        int samples();
        double mean();
        double min();
        double max();
        double standardDeviation();
        double variance();
        double skewness();
        double kurtosis();
        double percentile(double percent);
        double weightSum();
        double errorEstimate();

        void reset();
        void add
            (double value, double weight);
        void addSequence(List<double> data, List<double> weight);

        KeyValuePair<double, int> expectationValue(Func<KeyValuePair<double, double>, double> f,
            Func<KeyValuePair<double, double>, bool> inRange);
    }
}