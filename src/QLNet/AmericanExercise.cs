using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class AmericanExercise : EarlyExercise
    {
        public AmericanExercise(Date earliestDate, Date latestDate, bool payoffAtExpiry = false)
            : base(Type.American, payoffAtExpiry)
        {
            Utils.QL_REQUIRE(earliestDate <= latestDate, () => "earliest > latest exercise date");
            dates_ = new InitializedList<Date>(2);
            dates_[0] = earliestDate;
            dates_[1] = latestDate;
        }

        public AmericanExercise(Date latest, bool payoffAtExpiry = false) : base(Type.American, payoffAtExpiry)
        {
            dates_ = new InitializedList<Date>(2);
            dates_[0] = Date.minDate();
            dates_[1] = latest;
        }
    }
}
