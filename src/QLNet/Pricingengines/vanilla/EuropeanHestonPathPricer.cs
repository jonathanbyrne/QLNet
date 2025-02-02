﻿using JetBrains.Annotations;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.PricingEngines.vanilla
{
    [PublicAPI]
    public class EuropeanHestonPathPricer : PathPricer<IPath>
    {
        private double discount_;
        private PlainVanillaPayoff payoff_;

        public EuropeanHestonPathPricer(QLNet.Option.Type type, double strike, double discount)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;

            QLNet.Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
        }

        public double value(IPath multiPath)
        {
            var m = multiPath as MultiPath;
            QLNet.Utils.QL_REQUIRE(m != null, () => "the path is invalid");
            var path = m[0];
            var n = m.pathSize();
            QLNet.Utils.QL_REQUIRE(n > 0, () => "the path cannot be empty");

            return payoff_.value(path.back()) * discount_;
        }
    }
}
