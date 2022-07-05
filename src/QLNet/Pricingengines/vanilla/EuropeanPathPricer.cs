using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.vanilla
{
    [PublicAPI]
    public class EuropeanPathPricer : PathPricer<IPath>
    {
        private double discount_;
        private PlainVanillaPayoff payoff_;

        public EuropeanPathPricer(QLNet.Option.Type type, double strike, double discount)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;
            Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
        }

        public double value(IPath path)
        {
            Utils.QL_REQUIRE(path.length() > 0, () => "the path cannot be empty");
            return payoff_.value((path as Path).back()) * discount_;
        }
    }
}
