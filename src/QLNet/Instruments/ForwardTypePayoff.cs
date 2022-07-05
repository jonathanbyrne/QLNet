using JetBrains.Annotations;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class ForwardTypePayoff : Payoff
    {
        protected double strike_;
        protected Position.Type type_;

        public ForwardTypePayoff(Position.Type type, double strike)
        {
            type_ = type;
            strike_ = strike;
            QLNet.Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
        }

        public override string description()
        {
            var result = name() + ", " + strike() + " strike";
            return result;
        }

        public Position.Type forwardType() => type_;

        // Payoff interface
        public override string name() => "Forward";

        public double strike() => strike_;

        public override double value(double price)
        {
            switch (type_)
            {
                case Position.Type.Long:
                    return price - strike_;
                case Position.Type.Short:
                    return strike_ - price;
                default:
                    QLNet.Utils.QL_FAIL("unknown/illegal position ExerciseType");
                    return 0;
            }
        }
    }
}
