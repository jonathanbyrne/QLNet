namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class ContinuousFixedLookbackOption : OneAssetOption
    {
        //! %Arguments for continuous fixed lookback option calculation
        public new class Arguments : Option.Arguments
        {
            public double? minmax { get; set; }
            public override void validate()
            {
                base.validate();

                Utils.QL_REQUIRE(minmax != null, () => "null prior extremum");
                Utils.QL_REQUIRE(minmax >= 0.0, () => "nonnegative prior extremum required: "
                                                      + minmax + " not allowed");
            }
        }

        //! %Continuous fixed lookback %engine base class
        public new class Engine : GenericEngine<Arguments,
            Results>
        { }
        public ContinuousFixedLookbackOption(double minmax, StrikedTypePayoff payoff, Exercise exercise)
            : base(payoff, exercise)
        {
            minmax_ = minmax;
        }
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");
            moreArgs.minmax = minmax_;
        }

        protected double minmax_;
    }
}