using System;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class CashOrNothingPayoff : StrikedTypePayoff
    {
        protected double cashPayoff_;
        public double cashPayoff() => cashPayoff_;

        public CashOrNothingPayoff(Option.Type type, double strike, double cashPayoff) : base(type, strike)
        {
            cashPayoff_ = cashPayoff;
        }
        // Payoff interface
        public override string name() => "CashOrNothing";

        public override string description() => base.description() + ", " + cashPayoff() + " cash payoff";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return price - strike_ > 0.0 ? cashPayoff_ : 0.0;
                case QLNet.Option.Type.Put:
                    return strike_ - price > 0.0 ? cashPayoff_ : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }
}