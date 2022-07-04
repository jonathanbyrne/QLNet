using System;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class PlainVanillaPayoff : StrikedTypePayoff
    {
        public PlainVanillaPayoff(Option.Type type, double strike) : base(type, strike) { }

        // Payoff interface
        public override string name() => "Vanilla";

        public override double value(double price)
        {
            switch (type_)
            {
                case QLNet.Option.Type.Call:
                    return System.Math.Max(price - strike_, 0.0);
                case QLNet.Option.Type.Put:
                    return System.Math.Max(strike_ - price, 0.0);
                default:
                    throw new ArgumentException("unknown/illegal option ExerciseType");
            }
        }
    }
}