using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class SuperSharePayoff : StrikedTypePayoff
    {
        protected double cashPayoff_;
        protected double secondStrike_;

        public SuperSharePayoff(double strike, double secondStrike, double cashPayoff)
            : base(Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;
            cashPayoff_ = cashPayoff;

            Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                                                          ") must be higher than first strike (" + strike + ")");
        }

        public double cashPayoff() => cashPayoff_;

        public override string description() => base.description() + ", " + secondStrike() + " second strike" + ", " + cashPayoff() + " amount";

        // Payoff interface
        public override string name() => "SuperShare";

        public double secondStrike() => secondStrike_;

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? cashPayoff_ : 0.0;
    }
}
