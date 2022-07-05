using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class SpreadBasketPayoff : BasketPayoff
    {
        public SpreadBasketPayoff(Payoff p)
            : base(p)
        {
        }

        public override double accumulate(Vector a)
        {
            Utils.QL_REQUIRE(a.size() == 2, () => "payoff is only defined for two underlyings");
            return a[0] - a[1];
        }
    }
}
