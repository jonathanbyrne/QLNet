using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class MaxBasketPayoff : BasketPayoff
    {
        public MaxBasketPayoff(Payoff p) : base(p)
        {
        }

        public override double accumulate(Vector a) => a.Max();
    }
}
