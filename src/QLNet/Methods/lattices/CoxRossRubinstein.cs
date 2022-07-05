using JetBrains.Annotations;

namespace QLNet.Methods.lattices
{
    [PublicAPI]
    public class CoxRossRubinstein : EqualJumpsBinomialTree<CoxRossRubinstein>, ITreeFactory<CoxRossRubinstein>
    {
        // parameterless constructor is requried for generics
        public CoxRossRubinstein()
        {
        }

        public CoxRossRubinstein(StochasticProcess1D process, double end, int steps, double strike)
            : base(process, end, steps)
        {
            dx_ = process.stdDeviation(0.0, x0_, dt_);
            pu_ = 0.5 + 0.5 * driftPerStep_ / dx_;
            pd_ = 1.0 - pu_;

            Utils.QL_REQUIRE(pu_ <= 1.0, () => "negative probability");
            Utils.QL_REQUIRE(pu_ >= 0.0, () => "negative probability");
        }

        public CoxRossRubinstein factory(StochasticProcess1D process, double end, int steps, double strike) => new CoxRossRubinstein(process, end, steps, strike);
    }
}
