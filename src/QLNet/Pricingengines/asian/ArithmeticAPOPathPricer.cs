using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.asian
{
    [PublicAPI]
    public class ArithmeticAPOPathPricer : PathPricer<IPath>
    {
        private double discount_;
        private int pastFixings_;
        private PlainVanillaPayoff payoff_;
        private double runningSum_;

        public ArithmeticAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount,
            double runningSum,
            int pastFixings)
        {
            payoff_ = new PlainVanillaPayoff(type, strike);
            discount_ = discount;
            runningSum_ = runningSum;
            pastFixings_ = pastFixings;
            Utils.QL_REQUIRE(strike >= 0.0, () => "strike less than zero not allowed");
        }

        public ArithmeticAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount,
            double runningSum)
            : this(type, strike, discount, runningSum, 0)
        {
        }

        public ArithmeticAPOPathPricer(QLNet.Option.Type type,
            double strike,
            double discount)
            : this(type, strike, discount, 0.0, 0)
        {
        }

        public double value(Path path)
        {
            var n = path.length();
            Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");

            var sum = runningSum_;
            int fixings;
            if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
            {
                // include initial fixing
                for (var i = 0; i < path.length(); i++)
                {
                    sum += path[i];
                }

                fixings = pastFixings_ + n;
            }
            else
            {
                for (var i = 1; i < path.length(); i++)
                {
                    sum += path[i];
                }

                fixings = pastFixings_ + n - 1;
            }

            var averagePrice = sum / fixings;
            return discount_ * payoff_.value(averagePrice);
        }

        public double value(IPath path) => value((Path)path);
    }
}
