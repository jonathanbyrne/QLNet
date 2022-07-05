using JetBrains.Annotations;
using QLNet.Termstructures;
using QLNet.Time;

namespace QLNet
{
    [PublicAPI]
    public class EURLiborSW : EURLibor
    {
        public EURLiborSW()
            : base(new Period(1, TimeUnit.Weeks), new Handle<YieldTermStructure>())
        {
        }

        public EURLiborSW(Handle<YieldTermStructure> h)
            : base(new Period(1, TimeUnit.Weeks), h)
        {
        }
    }
}
