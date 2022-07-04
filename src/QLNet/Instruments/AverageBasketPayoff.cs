using QLNet.Math;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class AverageBasketPayoff : BasketPayoff
    {
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

        private Vector weights_;
    }
}