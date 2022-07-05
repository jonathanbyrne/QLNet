using System;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class PercentageStrikePayoff : StrikedTypePayoff
    {
        public PercentageStrikePayoff(Option.Type type, double moneyness) : base(type, moneyness)
        {
        }

        // Payoff interface
        public override string name() => "PercentageStrike";

        public override double value(double price)
        {
            switch (type_)
            {
                case Option.Type.Call:
                    return price * System.Math.Max(1.0 - strike_, 0.0);
                case Option.Type.Put:
                    return price * System.Math.Max(strike_ - 1.0, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }
}
