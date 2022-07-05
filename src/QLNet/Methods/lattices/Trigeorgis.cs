using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class Trigeorgis : EqualJumpsBinomialTree<Trigeorgis>, ITreeFactory<Trigeorgis>
    {
        // parameterless constructor is requried for generics
        public Trigeorgis()
        {
        }

        public Trigeorgis(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps)
        {
            dx_ = System.Math.Sqrt(process.variance(0.0, x0_, dt_) + driftPerStep_ * driftPerStep_);
            pu_ = 0.5 + 0.5 * driftPerStep_ / dx_;
            pd_ = 1.0 - pu_;

            QLNet.Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            QLNet.Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public Trigeorgis factory(StochasticProcess1D process, double end, int steps, double strike) => new Trigeorgis(process, end, steps, strike);
    }
}
