using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class ContinuousPartialFloatingLookbackOption : ContinuousFloatingLookbackOption
    {
        //! %Arguments for continuous partial floating lookback option calculation
        public new class Arguments : ContinuousFloatingLookbackOption.Arguments
        {
            public double lambda { get; set; }

            public Date lookbackPeriodEnd { get; set; }

            public override void validate()
            {
                base.validate();

                var europeanExercise = exercise as EuropeanExercise;
                Utils.QL_REQUIRE(lookbackPeriodEnd <= europeanExercise.lastDate(), () =>
                    "lookback start date must be earlier than exercise date");

                var floatingTypePayoff = payoff as FloatingTypePayoff;

                if (floatingTypePayoff.optionType() == Type.Call)
                {
                    Utils.QL_REQUIRE(lambda >= 1.0, () =>
                        "lambda should be greater than or equal to 1 for calls");
                }

                if (floatingTypePayoff.optionType() == Type.Put)
                {
                    Utils.QL_REQUIRE(lambda <= 1.0, () =>
                        "lambda should be smaller than or equal to 1 for puts");
                }
            }
        }

        //! %Continuous partial floating lookback %engine base class
        public new class Engine : GenericEngine<Arguments,
            Results>
        {
        }

        protected double lambda_;
        protected Date lookbackPeriodEnd_;

        public ContinuousPartialFloatingLookbackOption(double minmax, double lambda,
            Date lookbackPeriodEnd, TypePayoff payoff, Exercise exercise)
            : base(minmax, payoff, exercise)
        {
            lambda_ = lambda;
            lookbackPeriodEnd_ = lookbackPeriodEnd;
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");
            moreArgs.lambda = lambda_;
            moreArgs.lookbackPeriodEnd = lookbackPeriodEnd_;
        }
    }
}
