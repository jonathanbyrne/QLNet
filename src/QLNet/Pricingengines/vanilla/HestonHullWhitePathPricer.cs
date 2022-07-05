using JetBrains.Annotations;
using QLNet.Math;
using QLNet.Methods.montecarlo;
using QLNet.processes;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class HestonHullWhitePathPricer : PathPricer<IPath>
    {
        private double exerciseTime_;
        private Payoff payoff_;
        private HybridHestonHullWhiteProcess process_;

        public HestonHullWhitePathPricer(double exerciseTime, Payoff payoff, HybridHestonHullWhiteProcess process)
        {
            exerciseTime_ = exerciseTime;
            payoff_ = payoff;
            process_ = process;
        }

        public double value(IPath path)
        {
            var p = path as MultiPath;
            Utils.QL_REQUIRE(p != null, () => "invalid path");

            Utils.QL_REQUIRE(p.pathSize() > 0, () => "the path cannot be empty");

            var states = new Vector(p.assetNumber());
            for (var j = 0; j < states.size(); ++j)
            {
                states[j] = p[j][p.pathSize() - 1];
            }

            var df = 1.0 / process_.numeraire(exerciseTime_, states);
            return payoff_.value(states[0]) * df;
        }
    }
}
