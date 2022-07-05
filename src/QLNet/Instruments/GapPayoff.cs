using System;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class GapPayoff : StrikedTypePayoff
    {
        protected double secondStrike_;

        public GapPayoff(Option.Type type, double strike, double secondStrike) // a.k.a. payoff strike
            : base(type, strike)
        {
            secondStrike_ = secondStrike;
        }

        public override string description() => base.description() + ", " + secondStrike() + " strike payoff";

        // Payoff interface
        public override string name() => "Gap";

        public double secondStrike() => secondStrike_;

        public override double value(double price)
        {
            switch (type_)
            {
                case Option.Type.Call:
                    return price - strike_ >= 0.0 ? price - secondStrike_ : 0.0;
                case Option.Type.Put:
                    return strike_ - price >= 0.0 ? secondStrike_ - price : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }
}
