using QLNet.Math.statistics;

namespace QLNet.Tests;

class IncrementalGaussianStatistics : GenericGaussianStatistics<IncrementalStatistics>
{
    public double downsideVariance() => impl_.downsideVariance();
}