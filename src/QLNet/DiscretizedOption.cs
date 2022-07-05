using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Math;

namespace QLNet
{
    [PublicAPI]
    public class DiscretizedOption : DiscretizedAsset
    {
        protected List<double> exerciseTimes_;
        protected Exercise.Type exerciseType_;
        protected DiscretizedAsset underlying_;

        public DiscretizedOption(DiscretizedAsset underlying, Exercise.Type exerciseType, List<double> exerciseTimes)
        {
            underlying_ = underlying;
            exerciseType_ = exerciseType;
            exerciseTimes_ = exerciseTimes;
        }

        public override List<double> mandatoryTimes()
        {
            var times = underlying_.mandatoryTimes();

            // add the positive ones
            times.AddRange(exerciseTimes_.FindAll(x => x > 0));
            return times;
        }

        public override void reset(int size)
        {
            QLNet.Utils.QL_REQUIRE(method() == underlying_.method(),
                () => "option and underlying were initialized on different methods");
            values_ = new Vector(size, 0.0);
            adjustValues();
        }

        protected void applyExerciseCondition()
        {
            for (var i = 0; i < values_.size(); i++)
            {
                values_[i] = System.Math.Max(underlying_.values()[i], values_[i]);
            }
        }

        protected override void postAdjustValuesImpl()
        {
            /* In the real world, with time flowing forward, first
            any payment is settled and only after options can be
            exercised. Here, with time flowing backward, options
            must be exercised before performing the adjustment.
         */
            underlying_.partialRollback(time());
            underlying_.preAdjustValues();
            switch (exerciseType_)
            {
                case Exercise.Type.American:
                    if (time_ >= exerciseTimes_[0] && time_ <= exerciseTimes_[1])
                    {
                        applyExerciseCondition();
                    }

                    break;
                case Exercise.Type.Bermudan:
                case Exercise.Type.European:
                    for (var i = 0; i < exerciseTimes_.Count; i++)
                    {
                        var t = exerciseTimes_[i];
                        if (t >= 0.0 && isOnTime(t))
                        {
                            applyExerciseCondition();
                        }
                    }

                    break;
                default:
                    QLNet.Utils.QL_FAIL("invalid exercise ExerciseType");
                    break;
            }

            underlying_.postAdjustValues();
        }
    }
}
