using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class EURLibor7M : EURLibor
    {
        public EURLibor7M()
            : base(new Period(7, TimeUnit.Months), new Handle<YieldTermStructure>())
        {}

        public EURLibor7M(Handle<YieldTermStructure> h)
            : base(new Period(7, TimeUnit.Months), h)
        {}

    }
}