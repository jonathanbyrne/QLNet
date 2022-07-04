namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class SuperSharePayoff : StrikedTypePayoff
    {
        protected double secondStrike_;
        public double secondStrike() => secondStrike_;

        protected double cashPayoff_;
        public double cashPayoff() => cashPayoff_;

        public SuperSharePayoff(double strike, double secondStrike, double cashPayoff)
            : base(QLNet.Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;
            cashPayoff_ = cashPayoff;

            Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                                                          ") must be higher than first strike (" + strike + ")");
        }

        // Payoff interface
        public override string name() => "SuperShare";

        public override string description() => base.description() + ", " + secondStrike() + " second strike" + ", " + cashPayoff() + " amount";

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? cashPayoff_ : 0.0;
    }
}