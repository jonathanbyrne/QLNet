using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.vanilla
{
    [JetBrains.Annotations.PublicAPI] public class EuropeanHestonPathPricer : PathPricer<IPath>
    {
        public EuropeanHestonPathPricer(QLNet.Option.Type type, double strike, double discount)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;

            Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
        }

        public double value(IPath multiPath)
        {
            var m = multiPath as MultiPath;
            Utils.QL_REQUIRE(m != null, () => "the path is invalid");
            var path = m[0];
            var n = m.pathSize();
            Utils.QL_REQUIRE(n > 0, () => "the path cannot be empty");

            return payoff_.value(path.back()) * discount_;
        }

        private PlainVanillaPayoff payoff_;
        private double discount_;
    }
}