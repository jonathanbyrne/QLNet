using JetBrains.Annotations;
using QLNet.Extensions;
using QLNet.Instruments;
using QLNet.Methods.montecarlo;

namespace QLNet.Pricingengines.asian
{
    [PublicAPI]
    public class ArithmeticASOPathPricer : PathPricer<Path>
    {
        private double discount_;
        private int pastFixings_;
        private double runningSum_;
        private QLNet.Option.Type type_;

        public ArithmeticASOPathPricer(QLNet.Option.Type type,
            double discount,
            double runningSum,
            int pastFixings)
        {
            type_ = type;
            discount_ = discount;
            runningSum_ = runningSum;
            pastFixings_ = pastFixings;
        }

        public ArithmeticASOPathPricer(QLNet.Option.Type type,
            double discount,
            double runningSum)
            : this(type, discount, runningSum, 0)
        {
        }

        public ArithmeticASOPathPricer(QLNet.Option.Type type,
            double discount)
            : this(type, discount, 0.0, 0)
        {
        }

        public double value(Path path)
        {
            var n = path.length();
            Utils.QL_REQUIRE(n > 1, () => "the path cannot be empty");
            var averageStrike = runningSum_;
            if (path.timeGrid().mandatoryTimes()[0].IsEqual(0.0))
            {
                //averageStrike =
                //std::accumulate(path.begin(),path.end(),runningSum_)/(pastFixings_ + n)
                for (var i = 0; i < path.length(); i++)
                {
                    averageStrike += path[i];
                }

                averageStrike /= pastFixings_ + n;
            }
            else
            {
                for (var i = 1; i < path.length(); i++)
                {
                    averageStrike += path[i];
                }

                averageStrike /= pastFixings_ + n - 1;
            }

            return discount_
                   * new PlainVanillaPayoff(type_, averageStrike).value(path.back());
        }
    }
}
