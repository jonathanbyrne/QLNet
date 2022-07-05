using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EURLibor9M : EURLibor
    {
        public EURLibor9M()
            : base(new Period(9, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor9M(Handle<YieldTermStructure> h)
            : base(new Period(9, TimeUnit.Months), h)
        {
        }
    }
}
