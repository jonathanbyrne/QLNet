using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet.Instruments
{
    [PublicAPI]
    public class DiscreteAveragingAsianOption : OneAssetOption
    {
        public new class Arguments : Option.Arguments
        {
            public Arguments()
            {
                averageType = Average.Type.NULL;
                runningAccumulator = null;
                pastFixings = null;
            }

            public Average.Type averageType { get; set; }

            public List<Date> fixingDates { get; set; }

            public int? pastFixings { get; set; }

            public double? runningAccumulator { get; set; }

            public override void validate()
            {
                base.validate();

                QLNet.Utils.QL_REQUIRE(averageType != Average.Type.NULL, () => "unspecified average ExerciseType");
                QLNet.Utils.QL_REQUIRE(pastFixings != null, () => "null past-fixing number");
                QLNet.Utils.QL_REQUIRE(runningAccumulator != null, () => "null running product");

                switch (averageType)
                {
                    case Average.Type.Arithmetic:
                        QLNet.Utils.QL_REQUIRE(runningAccumulator >= 0.0, () =>
                            "non negative running sum required: " + runningAccumulator + " not allowed");
                        break;
                    case Average.Type.Geometric:
                        QLNet.Utils.QL_REQUIRE(runningAccumulator > 0.0, () =>
                            "positive running product required: " + runningAccumulator + " not allowed");
                        break;
                    default:
                        QLNet.Utils.QL_FAIL("invalid average ExerciseType");
                        break;
                }

                // check fixingTimes_ here
            }
        }

        public new class Engine : GenericEngine<Arguments, Results>
        {
        }

        protected Average.Type averageType_;
        protected List<Date> fixingDates_;
        protected int? pastFixings_;
        protected double? runningAccumulator_;

        public DiscreteAveragingAsianOption(Average.Type averageType, double? runningAccumulator, int? pastFixings, List<Date> fixingDates, StrikedTypePayoff payoff, Exercise exercise)
            : base(payoff, exercise)
        {
            averageType_ = averageType;
            runningAccumulator_ = runningAccumulator;
            pastFixings_ = pastFixings;
            fixingDates_ = fixingDates;

            fixingDates_.Sort();
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
            base.setupArguments(args);

            var moreArgs = args as Arguments;
            QLNet.Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument ExerciseType");

            moreArgs.averageType = averageType_;
            moreArgs.runningAccumulator = runningAccumulator_;
            moreArgs.pastFixings = pastFixings_;
            moreArgs.fixingDates = fixingDates_;
        }
    }
}
