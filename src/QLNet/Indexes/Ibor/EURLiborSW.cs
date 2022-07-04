using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [JetBrains.Annotations.PublicAPI] public class EURLiborSW : EURLibor
    {
        public EURLiborSW()
            : base(new Period(1, TimeUnit.Weeks), new Handle<YieldTermStructure>())
        {}
        public EURLiborSW(Handle<YieldTermStructure> h)
            : base(new Period(1, TimeUnit.Weeks), h)
        {}
    }
}