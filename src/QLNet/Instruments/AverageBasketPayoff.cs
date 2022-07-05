using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class AverageBasketPayoff : BasketPayoff
    {
        private Vector weights_;

        public AverageBasketPayoff(Payoff p, Vector a) : base(p)
        {
            weights_ = a;
        }

        public AverageBasketPayoff(Payoff p, int n) : base(p)
        {
            weights_ = new Vector(n, 1.0 / n);
        }

        public override double accumulate(Vector a)
        {
            var tally = weights_ * a;
            return tally;
        }
    }
}
