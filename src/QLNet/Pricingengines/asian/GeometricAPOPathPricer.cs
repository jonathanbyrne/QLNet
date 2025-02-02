﻿using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.PricingEngines.asian
{
    [PublicAPI]
    public class GeometricAPOPathPricer : PathPricer<Path>
    {
        private double discount_;
        private int pastFixings_;
        private PlainVanillaPayoff payoff_;
        private double runningProduct_;

        public GeometricAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount,
            double runningProduct,
            int pastFixings)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;
            runningProduct_ = runningProduct;
            pastFixings_ = pastFixings;
            QLNet.Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
        }

        public GeometricAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount,
            double runningProduct)
            : this(type, strike, discount, runningProduct, 0)
        {
        }

        public GeometricAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount)
            : this(type, strike, discount, 1.0, 0)
        {
        }

        public double value(Path path)
        {
            var n = path.length() - 1;
            QLNet.Utils.QL_REQUIRE(n > 0, () => "the path cannot be empty");

            double averagePrice;
            var product = runningProduct_;
            var fixings = n + pastFixings_;
            if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
            {
                fixings += 1;
                product *= path.front();
            }

            // care must be taken not to overflow product
            var maxValue = double.MaxValue;
            averagePrice = 1.0;
            for (var i = 1; i < n + 1; i++)
            {
                var price = path[i];
                if (product < maxValue / price)
                {
                    product *= price;
                }
                else
                {
                    averagePrice *= System.Math.Pow(product, 1.0 / fixings);
                    product = price;
                }
            }

            averagePrice *= System.Math.Pow(product, 1.0 / fixings);
            return discount_ * payoff_.value(averagePrice);
        }
    }
}
