using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class StrikedTypePayoff : TypePayoff
    {
        protected double strike_;

        public StrikedTypePayoff(Option.Type type, double strike) : base(type)
        {
            strike_ = strike;
        }

        public StrikedTypePayoff(Payoff p)
            : base((p as StrikedTypePayoff).type_)
        {
            strike_ = (p as StrikedTypePayoff).strike_;
        }

        // Payoff interface
        public override string description() => base.description() + ", " + strike() + " strike";

        public double strike() => strike_;
    }
}
