using JetBrains.Annotations;

namespace QLNet.Math.statistics
{
    [PublicAPI]
    public interface IConvergenceSteps
    {
        int initialSamples();

        int nextSamples(int current);
    }
}
