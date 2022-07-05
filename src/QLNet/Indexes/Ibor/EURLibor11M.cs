using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EURLibor11M : EURLibor
    {
        public EURLibor11M()
            : base(new Period(11, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor11M(Handle<YieldTermStructure> h)
            : base(new Period(11, TimeUnit.Months), h)
        {
        }
    }
}
