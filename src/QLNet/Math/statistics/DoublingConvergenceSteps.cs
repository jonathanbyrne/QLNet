namespace QLNet.Math.statistics
{
    [JetBrains.Annotations.PublicAPI] public class DoublingConvergenceSteps : IConvergenceSteps
    {

        public int initialSamples() => 1;

        public int nextSamples(int current) => 2 * current + 1;
    }
}