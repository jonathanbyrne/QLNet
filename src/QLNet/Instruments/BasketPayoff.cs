using QLNet.Math;

namespace QLNet.Instruments
{
    public abstract class BasketPayoff : Payoff
    {
        private Payoff basePayoff_;

        protected BasketPayoff(Payoff p)
        {
            basePayoff_ = p;
        }

        public override string name() => basePayoff_.name();

        public override string description() => basePayoff_.description();

        public override double value(double price) => basePayoff_.value(price);

        public virtual double value(Vector a) => basePayoff_.value(accumulate(a));

        public abstract double accumulate(Vector a);

        public Payoff basePayoff() => basePayoff_;
    }
}