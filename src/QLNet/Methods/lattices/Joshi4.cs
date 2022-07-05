using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class Joshi4 : BinomialTree<Joshi4>, ITreeFactory<Joshi4>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public Joshi4()
        {
        }

        public Joshi4(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps % 2 != 0 ? steps : steps + 1)
        {
            QLNet.Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");

            var oddSteps = steps % 2 != 0 ? steps : steps + 1;
            var variance = process.variance(0.0, x0_, end);
            var ermqdt = System.Math.Exp(driftPerStep_ + 0.5 * variance / oddSteps);
            var d2 = (System.Math.Log(x0_ / strike) + driftPerStep_ * oddSteps) / System.Math.Sqrt(variance);
            pu_ = computeUpProb((oddSteps - 1.0) / 2.0, d2);
            pd_ = 1.0 - pu_;
            var pdash = computeUpProb((oddSteps - 1.0) / 2.0, d2 + System.Math.Sqrt(variance));
            up_ = ermqdt * pdash / pu_;
            down_ = (ermqdt - pu_ * up_) / (1.0 - pu_);
        }

        public Joshi4 factory(StochasticProcess1D process, double end, int steps, double strike) => new Joshi4(process, end, steps, strike);

        public override double probability(int x, int y, int branch) => branch == 1 ? pu_ : pd_;

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);

        protected double computeUpProb(double k, double dj)
        {
            var alpha = dj / System.Math.Sqrt(8.0);
            var alpha2 = alpha * alpha;
            var alpha3 = alpha * alpha2;
            var alpha5 = alpha3 * alpha2;
            var alpha7 = alpha5 * alpha2;
            var beta = -0.375 * alpha - alpha3;
            var gamma = 5.0 / 6.0 * alpha5 + 13.0 / 12.0 * alpha3
                                           + 25.0 / 128.0 * alpha;
            var delta = -0.1025 * alpha - 0.9285 * alpha3
                                        - 1.43 * alpha5 - 0.5 * alpha7;
            var p = 0.5;
            var rootk = System.Math.Sqrt(k);
            p += alpha / rootk;
            p += beta / (k * rootk);
            p += gamma / (k * k * rootk);
            // delete next line to get results for j three tree
            p += delta / (k * k * k * rootk);
            return p;
        }
    }
}
