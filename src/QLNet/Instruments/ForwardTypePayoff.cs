namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class ForwardTypePayoff : Payoff
    {
        protected Position.Type type_;
        public Position.Type forwardType() => type_;

        protected double strike_;
        public double strike() => strike_;

        public ForwardTypePayoff(Position.Type type, double strike)
        {
            type_ = type;
            strike_ = strike;
            Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
        }

        // Payoff interface
        public override string name() => "Forward";

        public override string description()
        {
            var result = name() + ", " + strike() + " strike";
            return result;
        }
        public override double value(double price)
        {
            switch (type_)
            {
                case Position.Type.Long:
                    return price - strike_;
                case Position.Type.Short:
                    return strike_ - price;
                default:
                    Utils.QL_FAIL("unknown/illegal position ExerciseType");
                    return 0;
            }
        }
    }
}