using QLNet.Math;

namespace QLNet.Instruments
{
    public abstract class BasketPayoff : Payoff
    {
        private readonly Payoff basePayoff_;

        protected BasketPayoff(Payoff p)
        {
            basePayoff_ = p;
        }

        public abstract double accumulate(Vector a);

        public Payoff basePayoff() => basePayoff_;

        public override string description() => basePayoff_.description();

        public override string name() => basePayoff_.name();

        public override double value(double price) => basePayoff_.value(price);

        public virtual double value(Vector a) => basePayoff_.value(accumulate(a));
    }
}
