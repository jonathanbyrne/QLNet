using System.Collections.Generic;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class BermudanExercise : EarlyExercise
    {
        public BermudanExercise(List<Date> dates) : this(dates, false)
        {}

        public BermudanExercise(List<Date> dates, bool payoffAtExpiry)
            : base(Type.Bermudan, payoffAtExpiry)
        {
            Utils.QL_REQUIRE(!dates.empty(), () => "no exercise date given");

            dates_ = dates;
            dates_.Sort();
        }
    }
}