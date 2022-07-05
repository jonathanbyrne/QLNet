using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class EqualProbabilitiesBinomialTree<T> : BinomialTree<T>
    {
        protected double up_;

        // parameterless constructor is requried for generics
        public EqualProbabilitiesBinomialTree()
        {
        }

        public EqualProbabilitiesBinomialTree(StochasticProcess1D process, double end, int steps)
            : base(process, end, steps)
        {
        }

        public override double probability(int x, int y, int z) => 0.5;

        public override double underlying(int i, int index)
        {
            long j = 2 * index - i;
            // exploiting the forward value tree centering
            return x0_ * System.Math.Exp(i * driftPerStep_ + j * up_);
        }
    }
}
