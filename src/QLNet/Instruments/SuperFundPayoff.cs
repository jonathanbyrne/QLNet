namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class SuperFundPayoff : StrikedTypePayoff
    {
        protected double secondStrike_;
        public double secondStrike() => secondStrike_;

        public SuperFundPayoff(double strike, double secondStrike) : base(QLNet.Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;

            Utils.QL_REQUIRE(strike > 0.0, () => "strike (" + strike + ") must be positive");
            Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                                                          ") must be higher than first strike (" + strike + ")");
        }

        // Payoff interface
        public override string name() => "SuperFund";

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? price / strike_ : 0.0;
    }
}