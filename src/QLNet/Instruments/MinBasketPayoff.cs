using System.Linq;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class MinBasketPayoff : BasketPayoff
    {
        public MinBasketPayoff(Payoff p) : base(p)
        {
        }

        public override double accumulate(Vector a) => a.Min();
    }
}
