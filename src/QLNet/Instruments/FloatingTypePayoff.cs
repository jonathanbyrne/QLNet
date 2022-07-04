using System;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class FloatingTypePayoff : TypePayoff
    {
        public FloatingTypePayoff(Option.Type type) : base(type) { }

        // Payoff interface
        public override string name() => "FloatingType";

        public override double value(double k) => throw new NotSupportedException("floating payoff not handled");
    }
}