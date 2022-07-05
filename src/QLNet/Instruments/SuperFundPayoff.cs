using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class SuperFundPayoff : StrikedTypePayoff
    {
        protected double secondStrike_;

        public SuperFundPayoff(double strike, double secondStrike) : base(Option.Type.Call, strike)
        {
            secondStrike_ = secondStrike;

            QLNet.Utils.QL_REQUIRE(strike > 0.0, () => "strike (" + strike + ") must be positive");
            QLNet.Utils.QL_REQUIRE(secondStrike > strike, () => "second strike (" + secondStrike +
                                                                         ") must be higher than first strike (" + strike + ")");
        }

        // Payoff interface
        public override string name() => "SuperFund";

        public double secondStrike() => secondStrike_;

        public override double value(double price) => price >= strike_ && price < secondStrike_ ? price / strike_ : 0.0;
    }
}
