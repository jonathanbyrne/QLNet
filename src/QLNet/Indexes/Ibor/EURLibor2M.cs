using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet.Indexes.Ibor
{
    [PublicAPI]
    public class EURLibor2M : EURLibor
    {
        public EURLibor2M()
            : base(new Period(2, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor2M(Handle<YieldTermStructure> h)
            : base(new Period(2, TimeUnit.Months), h)
        {
        }
    }
}
