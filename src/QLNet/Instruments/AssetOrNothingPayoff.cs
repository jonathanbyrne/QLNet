using System;
using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class AssetOrNothingPayoff : StrikedTypePayoff
    {
        public AssetOrNothingPayoff(Option.Type type, double strike) : base(type, strike)
        {
        }

        // Payoff interface
        public override string name() => "AssetOrNothing";

        public override double value(double price)
        {
            switch (type_)
            {
                case Option.Type.Call:
                    return price - strike_ > 0.0 ? price : 0.0;
                case Option.Type.Put:
                    return strike_ - price > 0.0 ? price : 0.0;
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }
}
