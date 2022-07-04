namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public interface IConvergenceSteps
    {
        int initialSamples();
        int nextSamples(int current);
    }
}