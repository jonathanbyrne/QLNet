using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class LeisenReimer : BinomialTree<LeisenReimer>, ITreeFactory<LeisenReimer>
    {
        protected double up_, down_, pu_, pd_;

        // parameterless constructor is requried for generics
        public LeisenReimer()
        {
        }

        public LeisenReimer(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps % 2 != 0 ? steps : steps + 1)
        {
            Utils.QL_REQUIRE(strike > 0.0, () => "strike must be positive");
            var oddSteps = steps % 2 != 0 ? steps : steps + 1;
            var variance = process.variance(0.0, x0_, end);
            var ermqdt = System.Math.Exp(driftPerStep_ + 0.5 * variance / oddSteps);
            var d2 = (System.Math.Log(x0_ / strike) + driftPerStep_ * oddSteps) / System.Math.Sqrt(variance);
            pu_ = Utils.PeizerPrattMethod2Inversion(d2, oddSteps);
            pd_ = 1.0 - pu_;
            var pdash = Utils.PeizerPrattMethod2Inversion(d2 + System.Math.Sqrt(variance), oddSteps);
            up_ = ermqdt * pdash / pu_;
            down_ = (ermqdt - pu_ * up_) / (1.0 - pu_);
        }

        public LeisenReimer factory(StochasticProcess1D process, double end, int steps, double strike) => new LeisenReimer(process, end, steps, strike);

        public override double probability(int i, int j, int branch) => branch == 1 ? pu_ : pd_;

        public override double underlying(int i, int index) => x0_ * System.Math.Pow(down_, i - index) * System.Math.Pow(up_, index);
    }
}
