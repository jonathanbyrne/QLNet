using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class EURLibor1M : EURLibor
    {
        public EURLibor1M()
            : base(new Period(1, TimeUnit.Months), new Handle<YieldTermStructure>())
        {}

        public EURLibor1M(Handle<YieldTermStructure> h)
            : base(new Period(1, TimeUnit.Months), h)
        {}

    }
}