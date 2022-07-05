using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class EURLibor8M : EURLibor
    {
        public EURLibor8M()
            : base(new Period(8, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor8M(Handle<YieldTermStructure> h)
            : base(new Period(8, TimeUnit.Months), h)
        {
        }
    }
}
