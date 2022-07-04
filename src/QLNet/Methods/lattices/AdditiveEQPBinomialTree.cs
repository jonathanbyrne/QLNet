namespace QLNet.Methods.lattices
{
    [JetBrains.Annotations.PublicAPI] public class AdditiveEQPBinomialTree : EqualProbabilitiesBinomialTree<AdditiveEQPBinomialTree>,
        ITreeFactory<AdditiveEQPBinomialTree>
    {
        // parameterless constructor is requried for generics
        public AdditiveEQPBinomialTree()
        { }

        public AdditiveEQPBinomialTree(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps)
        {
            up_ = -0.5 * driftPerStep_ +
                  0.5 * System.Math.Sqrt(4.0 * process.variance(0.0, x0_, dt_) - 3.0 * driftPerStep_ * driftPerStep_);
        }

        public AdditiveEQPBinomialTree factory(StochasticProcess1D process, double end, int steps, double strike) => new AdditiveEQPBinomialTree(process, end, steps, strike);
    }
}