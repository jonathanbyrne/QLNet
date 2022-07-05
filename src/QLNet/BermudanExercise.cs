using System.Collections.Generic;
using JetBrains.Annotations;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class BermudanExercise : EarlyExercise
    {
        public BermudanExercise(List<Date> dates, bool payoffAtExpiry = false)
            : base(Type.Bermudan, payoffAtExpiry)
        {
            Utils.QL_REQUIRE(!dates.empty(), () => "no exercise date given");

            dates_ = dates;
            dates_.Sort();
        }
    }
}
