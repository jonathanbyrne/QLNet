using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class Tian : BinomialTree<Tian>, ITreeFactory<Tian>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public Tian()
        {
        }

        public Tian(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps)
        {
            var q = System.Math.Exp(process.variance(0.0, x0_, dt_));
            var r = System.Math.Exp(driftPerStep_) * System.Math.Sqrt(q);

            up_ = 0.5 * r * q * (q + 1 + System.Math.Sqrt(q * q + 2 * q - 3));
            down_ = 0.5 * r * q * (q + 1 - System.Math.Sqrt(q * q + 2 * q - 3));

            pu_ = (r - down_) / (up_ - down_);
            pd_ = 1.0 - pu_;

            Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public Tian factory(StochasticProcess1D process, double end, int steps, double strike) => new Tian(process, end, steps, strike);

        public override double probability(int i, int j, int branch) => branch == 1 ? pu_ : pd_;

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);
    }
}
