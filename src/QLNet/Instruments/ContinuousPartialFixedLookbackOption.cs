using QLNet.Time;

namespace QLNet.Instruments
{
    [JetBrains.Annotations.PublicAPI] public class ContinuousPartialFixedLookbackOption : ContinuousFixedLookbackOption
    {
        //! %Arguments for continuous partial fixed lookback option calculation
        public new class Arguments : ContinuousFixedLookbackOption.Arguments
        {
            public Date lookbackPeriodStart { get; set; }
            public override void validate()
            {
                base.validate();

                var europeanExercise = exercise as EuropeanExercise;
                Utils.QL_REQUIRE(lookbackPeriodStart <= europeanExercise.lastDate(), () =>
                    "lookback start date must be earlier than exercise date");
            }
        }
        //! %Continuous partial fixed lookback %engine base class
        public new class Engine : GenericEngine<Arguments,
            Results>
        { }
        public ContinuousPartialFixedLookbackOption(Date lookbackPeriodStart, StrikedTypePayoff payoff, Exercise exercise)
            : base(0, payoff, exercise)
        {
            lookbackPeriodStart_ = lookbackPeriodStart;
        }
        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");
            moreArgs.lookbackPeriodStart = lookbackPeriodStart_;
        }

        protected Date lookbackPeriodStart_;
    }
}