using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class EURLibor10M : EURLibor
    {
        public EURLibor10M()
            : base(new Period(10, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor10M(Handle<YieldTermStructure> h)
            : base(new Period(10, TimeUnit.Months), h)
        {
        }
    }
}
