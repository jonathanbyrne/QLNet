using System.Linq;
using QLNet.Math;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class MaxBasketPayoff : BasketPayoff
    {
        public MaxBasketPayoff(Payoff p) : base(p)
        { }

        public override double accumulate(Vector a) => a.Max();
    }
}