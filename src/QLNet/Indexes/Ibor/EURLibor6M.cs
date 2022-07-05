using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class EURLibor6M : EURLibor
    {
        public EURLibor6M()
            : base(new Period(6, TimeUnit.Months), new Handle<YieldTermStructure>())
        {
        }

        public EURLibor6M(Handle<YieldTermStructure> h)
            : base(new Period(6, TimeUnit.Months), h)
        {
        }
    }
}
