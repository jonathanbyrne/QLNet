using System.Linq;
using QLNet.Math;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class MinBasketPayoff : BasketPayoff
    {
        public MinBasketPayoff(Payoff p) : base(p)
        { }

        public override double accumulate(Vector a) => a.Min();
    }
}